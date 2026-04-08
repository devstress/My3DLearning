// ============================================================================
// Tutorial 44 – Disaster Recovery (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — full failover failback lifecycle_ with nats broker endpoint
//   🟡 Intermediate  — multi region topology_ failover chain
//   🔴 Advanced      — failover result details_ publish audit trail
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial44;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_FullFailoverFailbackLifecycle_WithNatsBrokerEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-exam-lifecycle");
        var topic = AspireFixture.UniqueTopic("t44-exam-dr-audit");

        // TODO: Create a InMemoryFailoverManager with appropriate configuration
        dynamic mgr = null!;

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
        // TODO: var initial = await mgr.GetPrimaryAsync(...)
        dynamic initial = null!;
        Assert.That(initial!.RegionId, Is.EqualTo("primary-region"));

        // Failover
        // TODO: var failover = await mgr.FailoverAsync(...)
        dynamic failover = null!;
        Assert.That(failover.Success, Is.True);
        Assert.That(failover.PromotedRegionId, Is.EqualTo("standby-region"));
        Assert.That(failover.DemotedRegionId, Is.EqualTo("primary-region"));

        // TODO: var afterFailover = await mgr.GetPrimaryAsync(...)
        dynamic afterFailover = null!;
        Assert.That(afterFailover!.RegionId, Is.EqualTo("standby-region"));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope1 = null!;
        // TODO: await nats.PublishAsync(...)

        // Failback
        // TODO: var failback = await mgr.FailbackAsync(...)
        dynamic failback = null!;
        Assert.That(failback.Success, Is.True);

        // TODO: var afterFailback = await mgr.GetPrimaryAsync(...)
        dynamic afterFailback = null!;
        Assert.That(afterFailback!.RegionId, Is.EqualTo("primary-region"));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope2 = null!;
        // TODO: await nats.PublishAsync(...)

        nats.AssertReceivedOnTopic(topic, 2);
    }

    [Test]
    public async Task Intermediate_MultiRegionTopology_FailoverChain()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t44-exam-chain");
        var topic = AspireFixture.UniqueTopic("t44-exam-chain-events");

        // TODO: Create a InMemoryFailoverManager with appropriate configuration
        dynamic mgr = null!;

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
        // TODO: var r1 = await mgr.FailoverAsync(...)
        dynamic r1 = null!;
        Assert.That(r1.Success, Is.True);
        Assert.That((await mgr.GetPrimaryAsync())!.RegionId, Is.EqualTo("eu-west-1"));

        // Second failover: eu-west-1 → ap-south-1
        // TODO: var r2 = await mgr.FailoverAsync(...)
        dynamic r2 = null!;
        Assert.That(r2.Success, Is.True);
        Assert.That((await mgr.GetPrimaryAsync())!.RegionId, Is.EqualTo("ap-south-1"));

        var results = new[] { r1, r2 };
        foreach (var result in results)
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
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

        // TODO: Create a InMemoryFailoverManager with appropriate configuration
        dynamic mgr = null!;

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

        // TODO: var result = await mgr.FailoverAsync(...)
        dynamic result = null!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.PromotedRegionId, Is.EqualTo("region-b"));
        Assert.That(result.DemotedRegionId, Is.EqualTo("region-a"));
        Assert.That(result.Duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        Assert.That(result.CompletedAt, Is.GreaterThan(DateTimeOffset.MinValue));
        Assert.That(result.ErrorMessage, Is.Null);

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await nats.PublishAsync(...)
        nats.AssertReceivedOnTopic(topic, 1);

        // Verify regions after failover
        // TODO: var regions = await mgr.GetAllRegionsAsync(...)
        dynamic regions = null!;
        Assert.That(regions.Single(r => r.RegionId == "region-b").IsPrimary, Is.True);
        Assert.That(regions.Single(r => r.RegionId == "region-a").State, Is.EqualTo(FailoverState.Standby));
    }
}
#endif
