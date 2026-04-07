// ============================================================================
// Tutorial 44 – Disaster Recovery (Lab)
// ============================================================================
// EIP Pattern: Failover / Failback.
// E2E: InMemoryFailoverManager — register regions, failover, failback,
//      health-check updates, publish results to NatsBrokerEndpoint
//      (real NATS JetStream via Aspire).
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial44;

[TestFixture]
public sealed class Lab
{
    private static InMemoryFailoverManager CreateManager() =>
        new(NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));


    // ── 1. Region Registration ───────────────────────────────────────

    [Test]
    public async Task RegisterRegions_PublishTopologyToNatsBrokerEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-register");
        var topic = AspireFixture.UniqueTopic("t44-topology");

        var mgr = CreateManager();

        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1", DisplayName = "US East",
            State = FailoverState.Primary,
        });
        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "eu-west-1", DisplayName = "EU West",
            State = FailoverState.Standby,
        });

        var regions = await mgr.GetAllRegionsAsync();
        Assert.That(regions, Has.Count.EqualTo(2));

        foreach (var region in regions)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{region.RegionId}:{region.State}", "dr-manager", "topology.registered");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 2);
    }

    [Test]
    public async Task Failover_PromotesTarget_PublishResult()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-failover");
        var topic = AspireFixture.UniqueTopic("t44-failover-events");

        var mgr = CreateManager();

        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1", DisplayName = "US East",
            State = FailoverState.Primary,
        });
        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-west-2", DisplayName = "US West",
            State = FailoverState.Standby,
        });

        var result = await mgr.FailoverAsync("us-west-2");
        Assert.That(result.Success, Is.True);
        Assert.That(result.PromotedRegionId, Is.EqualTo("us-west-2"));
        Assert.That(result.DemotedRegionId, Is.EqualTo("us-east-1"));

        var primary = await mgr.GetPrimaryAsync();
        Assert.That(primary!.RegionId, Is.EqualTo("us-west-2"));

        var envelope = IntegrationEnvelope<string>.Create(
            $"promoted:{result.PromotedRegionId}", "dr-manager", "failover.complete");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Failover Operations ───────────────────────────────────────

    [Test]
    public async Task FailoverToUnknownRegion_PublishError()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-unknown");
        var topic = AspireFixture.UniqueTopic("t44-failover-errors-unknown");

        var mgr = CreateManager();

        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1", DisplayName = "US East",
            State = FailoverState.Primary,
        });

        var result = await mgr.FailoverAsync("nonexistent-region");
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not registered"));

        var envelope = IntegrationEnvelope<string>.Create(
            result.ErrorMessage!, "dr-manager", "failover.error");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task FailoverToSameRegion_PublishError()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-same-region");
        var topic = AspireFixture.UniqueTopic("t44-failover-errors-same");

        var mgr = CreateManager();

        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1", DisplayName = "US East",
            State = FailoverState.Primary,
        });

        var result = await mgr.FailoverAsync("us-east-1");
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("already the primary"));

        var envelope = IntegrationEnvelope<string>.Create(
            result.ErrorMessage!, "dr-manager", "failover.noop");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task FailbackRestoresOriginalPrimary_PublishResult()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-failback");
        var topic = AspireFixture.UniqueTopic("t44-failback-events");

        var mgr = CreateManager();

        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "primary-region", DisplayName = "Primary",
            State = FailoverState.Primary,
        });
        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "standby-region", DisplayName = "Standby",
            State = FailoverState.Standby,
        });

        await mgr.FailoverAsync("standby-region");
        var failback = await mgr.FailbackAsync("primary-region");
        Assert.That(failback.Success, Is.True);

        var primary = await mgr.GetPrimaryAsync();
        Assert.That(primary!.RegionId, Is.EqualTo("primary-region"));

        var envelope = IntegrationEnvelope<string>.Create(
            $"restored:{primary.RegionId}", "dr-manager", "failback.complete");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 3. Health Monitoring ─────────────────────────────────────────

    [Test]
    public async Task UpdateHealthCheck_PublishTimestampChange()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-health");
        var topic = AspireFixture.UniqueTopic("t44-health-events");

        var mgr = CreateManager();

        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1", DisplayName = "US East",
            State = FailoverState.Primary,
        });

        await mgr.UpdateHealthCheckAsync("us-east-1");

        var regions = await mgr.GetAllRegionsAsync();
        var region = regions.Single(r => r.RegionId == "us-east-1");
        Assert.That(region.LastHealthCheck, Is.GreaterThan(DateTimeOffset.MinValue));

        var envelope = IntegrationEnvelope<string>.Create(
            $"healthcheck:{region.RegionId}", "dr-manager", "health.updated");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task GetAllRegions_PublishRegionStates()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-all-regions");
        var topic = AspireFixture.UniqueTopic("t44-region-inventory");

        var mgr = CreateManager();

        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1", DisplayName = "US East",
            State = FailoverState.Primary,
        });
        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "eu-west-1", DisplayName = "EU West",
            State = FailoverState.Standby,
        });
        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "ap-south-1", DisplayName = "AP South",
            State = FailoverState.Standby,
        });

        var regions = await mgr.GetAllRegionsAsync();
        Assert.That(regions, Has.Count.EqualTo(3));

        foreach (var region in regions)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{region.RegionId}:{region.State}", "dr-manager", "region.state");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);

        var primary = await mgr.GetPrimaryAsync();
        Assert.That(primary!.RegionId, Is.EqualTo("us-east-1"));
    }
}
