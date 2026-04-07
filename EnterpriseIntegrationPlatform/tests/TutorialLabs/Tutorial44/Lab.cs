// ============================================================================
// Tutorial 44 – Disaster Recovery (Lab)
// ============================================================================
// EIP Pattern: Failover / Failback.
// E2E: InMemoryFailoverManager — register regions, failover, failback,
//      health-check updates, publish results to MockEndpoint.
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
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("dr-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    private static InMemoryFailoverManager CreateManager() =>
        new(NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));


    // ── 1. Region Registration ───────────────────────────────────────

    [Test]
    public async Task RegisterRegions_PublishTopologyToMockEndpoint()
    {
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
            await _output.PublishAsync(envelope, "topology", default);
        }

        _output.AssertReceivedOnTopic("topology", 2);
    }

    [Test]
    public async Task Failover_PromotesTarget_PublishResult()
    {
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
        await _output.PublishAsync(envelope, "failover-events", default);
        _output.AssertReceivedOnTopic("failover-events", 1);
    }


    // ── 2. Failover Operations ───────────────────────────────────────

    [Test]
    public async Task FailoverToUnknownRegion_PublishError()
    {
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
        await _output.PublishAsync(envelope, "failover-errors", default);
        _output.AssertReceivedOnTopic("failover-errors", 1);
    }

    [Test]
    public async Task FailoverToSameRegion_PublishError()
    {
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
        await _output.PublishAsync(envelope, "failover-errors", default);
        _output.AssertReceivedOnTopic("failover-errors", 1);
    }

    [Test]
    public async Task FailbackRestoresOriginalPrimary_PublishResult()
    {
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
        await _output.PublishAsync(envelope, "failback-events", default);
        _output.AssertReceivedOnTopic("failback-events", 1);
    }


    // ── 3. Health Monitoring ─────────────────────────────────────────

    [Test]
    public async Task UpdateHealthCheck_PublishTimestampChange()
    {
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
        await _output.PublishAsync(envelope, "health-events", default);
        _output.AssertReceivedOnTopic("health-events", 1);
    }

    [Test]
    public async Task GetAllRegions_PublishRegionStates()
    {
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
            await _output.PublishAsync(envelope, "region-inventory", default);
        }

        _output.AssertReceivedOnTopic("region-inventory", 3);

        var primary = await mgr.GetPrimaryAsync();
        Assert.That(primary!.RegionId, Is.EqualTo("us-east-1"));
    }
}
