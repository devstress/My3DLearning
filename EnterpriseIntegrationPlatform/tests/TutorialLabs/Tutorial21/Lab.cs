// ============================================================================
// Tutorial 21 – Aggregator (Lab)
// ============================================================================
// This lab exercises the MessageAggregator with InMemoryMessageAggregateStore,
// CountCompletionStrategy, and mock IAggregationStrategy.  You will verify
// accumulation behaviour, completion conditions, and aggregate publishing.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial21;

[TestFixture]
public sealed class Lab
{
    // ── InMemoryMessageAggregateStore Basics ─────────────────────────────────

    [Test]
    public async Task Store_AddAsync_ReturnsSingleItemGroup()
    {
        var store = new InMemoryMessageAggregateStore<string>();

        var envelope = IntegrationEnvelope<string>.Create(
            "item-1", "TestService", "order.line");

        var group = await store.AddAsync(envelope);

        Assert.That(group.Count, Is.EqualTo(1));
        Assert.That(group[0].Payload, Is.EqualTo("item-1"));
    }

    [Test]
    public async Task Store_AddAsync_GroupsBySameCorrelationId()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var correlationId = Guid.NewGuid();

        var e1 = IntegrationEnvelope<string>.Create(
            "item-1", "Svc", "line", correlationId: correlationId);
        var e2 = IntegrationEnvelope<string>.Create(
            "item-2", "Svc", "line", correlationId: correlationId);

        await store.AddAsync(e1);
        var group = await store.AddAsync(e2);

        Assert.That(group.Count, Is.EqualTo(2));
        Assert.That(group[0].Payload, Is.EqualTo("item-1"));
        Assert.That(group[1].Payload, Is.EqualTo("item-2"));
    }

    // ── CountCompletionStrategy ─────────────────────────────────────────────

    [Test]
    public void CountCompletion_NotComplete_WhenBelowExpected()
    {
        var strategy = new CountCompletionStrategy<string>(3);
        var envelopes = new[]
        {
            IntegrationEnvelope<string>.Create("a", "Svc", "t"),
            IntegrationEnvelope<string>.Create("b", "Svc", "t"),
        };

        Assert.That(strategy.IsComplete(envelopes), Is.False);
    }

    [Test]
    public void CountCompletion_Complete_WhenCountReached()
    {
        var strategy = new CountCompletionStrategy<string>(2);
        var envelopes = new[]
        {
            IntegrationEnvelope<string>.Create("a", "Svc", "t"),
            IntegrationEnvelope<string>.Create("b", "Svc", "t"),
        };

        Assert.That(strategy.IsComplete(envelopes), Is.True);
    }

    // ── MessageAggregator – Incomplete Group ────────────────────────────────

    [Test]
    public async Task Aggregator_ReturnsIncomplete_WhenGroupNotReady()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(3);
        var aggregation = Substitute.For<IAggregationStrategy<string, string>>();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new AggregatorOptions
        {
            TargetTopic = "aggregated-topic",
            ExpectedCount = 3,
        });

        var aggregator = new MessageAggregator<string, string>(
            store, completion, aggregation, producer, options,
            NullLogger<MessageAggregator<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var envelope = IntegrationEnvelope<string>.Create(
            "item-1", "Svc", "line", correlationId: correlationId);

        var result = await aggregator.AggregateAsync(envelope);

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.AggregateEnvelope, Is.Null);
        Assert.That(result.ReceivedCount, Is.EqualTo(1));
        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
    }

    // ── MessageAggregator – Complete Group & Publish ─────────────────────────

    [Test]
    public async Task Aggregator_CompletesAndPublishes_WhenCountReached()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(2);
        var aggregation = Substitute.For<IAggregationStrategy<string, string>>();
        aggregation
            .Aggregate(Arg.Any<IReadOnlyList<string>>())
            .Returns(ci =>
            {
                var items = ci.Arg<IReadOnlyList<string>>();
                return string.Join(",", items);
            });

        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new AggregatorOptions
        {
            TargetTopic = "agg-out",
            TargetMessageType = "order.batch",
            ExpectedCount = 2,
        });

        var aggregator = new MessageAggregator<string, string>(
            store, completion, aggregation, producer, options,
            NullLogger<MessageAggregator<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var e1 = IntegrationEnvelope<string>.Create(
            "A", "Svc", "line", correlationId: correlationId);
        var e2 = IntegrationEnvelope<string>.Create(
            "B", "Svc", "line", correlationId: correlationId);

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.ReceivedCount, Is.EqualTo(2));
        Assert.That(result.AggregateEnvelope, Is.Not.Null);
        Assert.That(result.AggregateEnvelope!.Payload, Is.EqualTo("A,B"));
        Assert.That(result.AggregateEnvelope.MessageType, Is.EqualTo("order.batch"));
        Assert.That(result.AggregateEnvelope.CorrelationId, Is.EqualTo(correlationId));

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            "agg-out",
            Arg.Any<CancellationToken>());
    }

    // ── MessageAggregator – Metadata Merging ────────────────────────────────

    [Test]
    public async Task Aggregator_MergesMetadata_FromAllEnvelopes()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(2);
        var aggregation = Substitute.For<IAggregationStrategy<string, string>>();
        aggregation
            .Aggregate(Arg.Any<IReadOnlyList<string>>())
            .Returns("merged");

        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new AggregatorOptions
        {
            TargetTopic = "merged-topic",
            ExpectedCount = 2,
        });

        var aggregator = new MessageAggregator<string, string>(
            store, completion, aggregation, producer, options,
            NullLogger<MessageAggregator<string, string>>.Instance);

        var correlationId = Guid.NewGuid();

        var e1 = IntegrationEnvelope<string>.Create(
            "A", "Svc", "line", correlationId: correlationId) with
        {
            Metadata = new Dictionary<string, string> { ["key1"] = "val1" },
        };
        var e2 = IntegrationEnvelope<string>.Create(
            "B", "Svc", "line", correlationId: correlationId) with
        {
            Metadata = new Dictionary<string, string> { ["key2"] = "val2" },
        };

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        Assert.That(result.AggregateEnvelope!.Metadata, Contains.Key("key1"));
        Assert.That(result.AggregateEnvelope.Metadata, Contains.Key("key2"));
    }
}
