// ============================================================================
// Tutorial 21 – Aggregator (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Aggregator pattern collects
//          related messages by CorrelationId and combines them into a single
//          aggregate when the group is complete.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Aggregate_SingleMessage_GroupNotComplete            — single message does not complete the group
//   2. Aggregate_ReachesCount_CompletesAndPublishes        — group completes and publishes when count reached
//   3. Aggregate_PreservesCorrelationId                    — aggregate preserves the original CorrelationId
//   4. Aggregate_DifferentCorrelationIds_FormSeparateGroups — different CorrelationIds form isolated groups
//   5. Aggregate_CountCompletion_ExactThreshold            — exact threshold triggers completion on final message
//   6. Aggregate_MergesMetadata_FromAllEnvelopes           — metadata from all envelopes is merged
//   7. Aggregate_UsesHighestPriority                       — aggregate uses the highest priority from the group
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial21;

[TestFixture]
public sealed class Lab
{
    // ── 1. Group Completion ──────────────────────────────────────────

    [Test]
    public async Task Aggregate_SingleMessage_GroupNotComplete()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t21-single");
        var topic = AspireFixture.UniqueTopic("t21-agg");
        var aggregator = CreateAggregator(nats, topic, expectedCount: 3);
        var envelope = IntegrationEnvelope<string>.Create("item1", "svc", "order.line");

        var result = await aggregator.AggregateAsync(envelope);

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.ReceivedCount, Is.EqualTo(1));
        Assert.That(result.AggregateEnvelope, Is.Null);
        nats.AssertNoneReceived();
    }

    [Test]
    public async Task Aggregate_ReachesCount_CompletesAndPublishes()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t21-complete");
        var topic = AspireFixture.UniqueTopic("t21-agg");
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(nats, topic, expectedCount: 2);

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "line", correlationId);
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "line", correlationId);

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.ReceivedCount, Is.EqualTo(2));
        Assert.That(result.AggregateEnvelope, Is.Not.Null);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Correlation & Isolation ───────────────────────────────────

    [Test]
    public async Task Aggregate_PreservesCorrelationId()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t21-corr");
        var topic = AspireFixture.UniqueTopic("t21-agg");
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(nats, topic, expectedCount: 2);

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "line", correlationId);
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "line", correlationId);

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(result.AggregateEnvelope!.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task Aggregate_DifferentCorrelationIds_FormSeparateGroups()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t21-groups");
        var topic = AspireFixture.UniqueTopic("t21-agg");
        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        var aggregator = CreateAggregator(nats, topic, expectedCount: 2);

        var e1a = IntegrationEnvelope<string>.Create("a1", "svc", "line", corr1);
        var e2a = IntegrationEnvelope<string>.Create("a2", "svc", "line", corr2);

        var r1 = await aggregator.AggregateAsync(e1a);
        var r2 = await aggregator.AggregateAsync(e2a);

        Assert.That(r1.IsComplete, Is.False);
        Assert.That(r2.IsComplete, Is.False);
        Assert.That(r1.ReceivedCount, Is.EqualTo(1));
        Assert.That(r2.ReceivedCount, Is.EqualTo(1));
        nats.AssertNoneReceived();
    }

    [Test]
    public async Task Aggregate_CountCompletion_ExactThreshold()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t21-threshold");
        var topic = AspireFixture.UniqueTopic("t21-agg");
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(nats, topic, expectedCount: 3);

        var e1 = IntegrationEnvelope<string>.Create("x", "svc", "t", correlationId);
        var e2 = IntegrationEnvelope<string>.Create("y", "svc", "t", correlationId);
        var e3 = IntegrationEnvelope<string>.Create("z", "svc", "t", correlationId);

        var r1 = await aggregator.AggregateAsync(e1);
        var r2 = await aggregator.AggregateAsync(e2);
        var r3 = await aggregator.AggregateAsync(e3);

        Assert.That(r1.IsComplete, Is.False);
        Assert.That(r2.IsComplete, Is.False);
        Assert.That(r3.IsComplete, Is.True);
        Assert.That(r3.ReceivedCount, Is.EqualTo(3));
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 3. Merge Strategies ──────────────────────────────────────────

    [Test]
    public async Task Aggregate_MergesMetadata_FromAllEnvelopes()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t21-meta");
        var topic = AspireFixture.UniqueTopic("t21-agg");
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(nats, topic, expectedCount: 2);

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "t", correlationId) with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "us-east" },
        };
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "t", correlationId) with
        {
            Metadata = new Dictionary<string, string> { ["tier"] = "premium" },
        };

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        var meta = result.AggregateEnvelope!.Metadata;
        Assert.That(meta["region"], Is.EqualTo("us-east"));
        Assert.That(meta["tier"], Is.EqualTo("premium"));
    }

    [Test]
    public async Task Aggregate_UsesHighestPriority()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t21-prio");
        var topic = AspireFixture.UniqueTopic("t21-agg");
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(nats, topic, expectedCount: 2);

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "t", correlationId) with
        {
            Priority = MessagePriority.Low,
        };
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "t", correlationId) with
        {
            Priority = MessagePriority.High,
        };

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        Assert.That(result.AggregateEnvelope!.Priority, Is.EqualTo(MessagePriority.High));
    }

    private static MessageAggregator<string, string> CreateAggregator(
        NatsBrokerEndpoint nats, string topic, int expectedCount)
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(expectedCount);
        var strategy = new MockAggregationStrategy<string, string>(items => string.Join(",", items));

        var options = Options.Create(new AggregatorOptions
        {
            TargetTopic = topic,
            ExpectedCount = expectedCount,
        });

        return new MessageAggregator<string, string>(
            store, completion, strategy, nats, options,
            NullLogger<MessageAggregator<string, string>>.Instance);
    }
}
