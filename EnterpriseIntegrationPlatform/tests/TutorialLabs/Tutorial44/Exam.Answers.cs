// ============================================================================
// Tutorial 44 – Disaster Recovery (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — full failover failback lifecycle_ with nats broker endpoint
//   🟡 Intermediate — multi region topology_ failover chain
//   🔴 Advanced     — failover result details_ publish audit trail
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial44;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_FullFailoverFailbackLifecycle_WithNatsBrokerEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-exam-lifecycle");
        var topic = AspireFixture.UniqueTopic("t44-exam-dr-audit");

        var mgr = new InMemoryFailoverManager(
            NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

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

        // Initial state
        var initial = await mgr.GetPrimaryAsync();
        Assert.That(initial!.RegionId, Is.EqualTo("primary-region"));

        // Failover
        var failover = await mgr.FailoverAsync("standby-region");
        Assert.That(failover.Success, Is.True);
        Assert.That(failover.PromotedRegionId, Is.EqualTo("standby-region"));
        Assert.That(failover.DemotedRegionId, Is.EqualTo("primary-region"));

        var afterFailover = await mgr.GetPrimaryAsync();
        Assert.That(afterFailover!.RegionId, Is.EqualTo("standby-region"));

        var envelope1 = IntegrationEnvelope<string>.Create(
            $"failover:{failover.PromotedRegionId}", "dr-manager", "failover.event");
        await nats.PublishAsync(envelope1, topic, default);

        // Failback
        var failback = await mgr.FailbackAsync("primary-region");
        Assert.That(failback.Success, Is.True);

        var afterFailback = await mgr.GetPrimaryAsync();
        Assert.That(afterFailback!.RegionId, Is.EqualTo("primary-region"));

        var envelope2 = IntegrationEnvelope<string>.Create(
            $"failback:{failback.PromotedRegionId}", "dr-manager", "failback.event");
        await nats.PublishAsync(envelope2, topic, default);

        nats.AssertReceivedOnTopic(topic, 2);
    }

    [Test]
    public async Task Intermediate_MultiRegionTopology_FailoverChain()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-exam-chain");
        var topic = AspireFixture.UniqueTopic("t44-exam-chain-events");

        var mgr = new InMemoryFailoverManager(
            NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

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

        // First failover: us-east-1 → eu-west-1
        var r1 = await mgr.FailoverAsync("eu-west-1");
        Assert.That(r1.Success, Is.True);
        Assert.That((await mgr.GetPrimaryAsync())!.RegionId, Is.EqualTo("eu-west-1"));

        // Second failover: eu-west-1 → ap-south-1
        var r2 = await mgr.FailoverAsync("ap-south-1");
        Assert.That(r2.Success, Is.True);
        Assert.That((await mgr.GetPrimaryAsync())!.RegionId, Is.EqualTo("ap-south-1"));

        var results = new[] { r1, r2 };
        foreach (var result in results)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{result.DemotedRegionId}→{result.PromotedRegionId}",
                "dr-manager", "failover.chain");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 2);

        var all = nats.GetAllReceived<string>(topic);
        Assert.That(all[0].Payload, Is.EqualTo("us-east-1→eu-west-1"));
        Assert.That(all[1].Payload, Is.EqualTo("eu-west-1→ap-south-1"));
    }

    [Test]
    public async Task Advanced_FailoverResultDetails_PublishAuditTrail()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-exam-audit");
        var topic = AspireFixture.UniqueTopic("t44-exam-audit-trail");

        var mgr = new InMemoryFailoverManager(
            NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "region-a", DisplayName = "Region A",
            State = FailoverState.Primary,
        });
        await mgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "region-b", DisplayName = "Region B",
            State = FailoverState.Standby,
        });

        var result = await mgr.FailoverAsync("region-b");

        Assert.That(result.Success, Is.True);
        Assert.That(result.PromotedRegionId, Is.EqualTo("region-b"));
        Assert.That(result.DemotedRegionId, Is.EqualTo("region-a"));
        Assert.That(result.Duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        Assert.That(result.CompletedAt, Is.GreaterThan(DateTimeOffset.MinValue));
        Assert.That(result.ErrorMessage, Is.Null);

        var envelope = IntegrationEnvelope<string>.Create(
            $"success:{result.PromotedRegionId}|demoted:{result.DemotedRegionId}|duration:{result.Duration.TotalMilliseconds}ms",
            "dr-manager", "failover.audit");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);

        // Verify regions after failover
        var regions = await mgr.GetAllRegionsAsync();
        Assert.That(regions.Single(r => r.RegionId == "region-b").IsPrimary, Is.True);
        Assert.That(regions.Single(r => r.RegionId == "region-a").State, Is.EqualTo(FailoverState.Standby));
    }
}
