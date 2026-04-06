// ============================================================================
// Tutorial 21 – Aggregator (Lab)
// ============================================================================
// EIP Pattern: Aggregator.
// E2E: Wire real MessageAggregator with InMemoryMessageAggregateStore,
// CountCompletionStrategy, MockAggregationStrategy, and MockEndpoint.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial21;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("agg-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task Aggregate_SingleMessage_GroupNotComplete()
    {
        var aggregator = CreateAggregator(expectedCount: 3);
        var envelope = IntegrationEnvelope<string>.Create("item1", "svc", "order.line");

        var result = await aggregator.AggregateAsync(envelope);

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.ReceivedCount, Is.EqualTo(1));
        Assert.That(result.AggregateEnvelope, Is.Null);
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task Aggregate_ReachesCount_CompletesAndPublishes()
    {
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(expectedCount: 2);

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "line", correlationId);
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "line", correlationId);

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.ReceivedCount, Is.EqualTo(2));
        Assert.That(result.AggregateEnvelope, Is.Not.Null);
        _output.AssertReceivedOnTopic("aggregated-topic", 1);
    }

    [Test]
    public async Task Aggregate_PreservesCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(expectedCount: 2);

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
        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        var aggregator = CreateAggregator(expectedCount: 2);

        var e1a = IntegrationEnvelope<string>.Create("a1", "svc", "line", corr1);
        var e2a = IntegrationEnvelope<string>.Create("a2", "svc", "line", corr2);

        var r1 = await aggregator.AggregateAsync(e1a);
        var r2 = await aggregator.AggregateAsync(e2a);

        Assert.That(r1.IsComplete, Is.False);
        Assert.That(r2.IsComplete, Is.False);
        Assert.That(r1.ReceivedCount, Is.EqualTo(1));
        Assert.That(r2.ReceivedCount, Is.EqualTo(1));
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task Aggregate_CountCompletion_ExactThreshold()
    {
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(expectedCount: 3);

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
        _output.AssertReceivedOnTopic("aggregated-topic", 1);
    }

    [Test]
    public async Task Aggregate_MergesMetadata_FromAllEnvelopes()
    {
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(expectedCount: 2);

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
        var correlationId = Guid.NewGuid();
        var aggregator = CreateAggregator(expectedCount: 2);

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

    private MessageAggregator<string, string> CreateAggregator(int expectedCount)
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(expectedCount);
        var strategy = new MockAggregationStrategy<string, string>(items => string.Join(",", items));

        var options = Options.Create(new AggregatorOptions
        {
            TargetTopic = "aggregated-topic",
            ExpectedCount = expectedCount,
        });

        return new MessageAggregator<string, string>(
            store, completion, strategy, _output, options,
            NullLogger<MessageAggregator<string, string>>.Instance);
    }
}
