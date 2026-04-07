// ============================================================================
// Broker-Agnostic EIP Tests — Splitter + Aggregator
// ============================================================================
// These tests prove that Splitter (decompose) and Aggregator (recompose)
// work identically with any broker. Both publish via IMessageBrokerProducer.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace BrokerAgnosticTests;

[TestFixture]
public sealed class SplitterAggregatorTests
{
    // ── 1. Splitter ─────────────────────────────────────────────────────

    [Test]
    public async Task Splitter_SplitsComposite_PublishesEachPart()
    {
        // The Splitter decomposes a composite payload into individual messages
        // and publishes each to the configured target topic via IMessageBrokerProducer.
        var broker = new MockEndpoint("splitter");
        var strategy = new FuncSplitStrategy<string>(
            composite => composite.Split(',').ToList());

        var splitter = new MessageSplitter<string>(
            strategy, broker,
            Options.Create(new SplitterOptions { TargetTopic = "items.individual" }),
            NullLogger<MessageSplitter<string>>.Instance);

        var composite = IntegrationEnvelope<string>.Create(
            "apple,banana,cherry", "BatchService", "BatchItems");

        var result = await splitter.SplitAsync(composite);

        // 3 individual messages published
        Assert.That(result.SplitEnvelopes, Has.Count.EqualTo(3));
        broker.AssertReceivedCount(3);
        broker.AssertReceivedOnTopic("items.individual", 3);
    }

    [Test]
    public async Task Splitter_PreservesCausationChain()
    {
        // Each split envelope must have CausationId = source.MessageId
        // and the same CorrelationId for tracing.
        var broker = new MockEndpoint("splitter-causation");
        var strategy = new FuncSplitStrategy<int>(n => [n, n + 1]);

        var splitter = new MessageSplitter<int>(
            strategy, broker,
            Options.Create(new SplitterOptions { TargetTopic = "split.out" }),
            NullLogger<MessageSplitter<int>>.Instance);

        var source = IntegrationEnvelope<int>.Create(10, "S", "Batch");
        var result = await splitter.SplitAsync(source);

        foreach (var item in result.SplitEnvelopes)
        {
            Assert.That(item.CausationId, Is.EqualTo(source.MessageId));
            Assert.That(item.CorrelationId, Is.EqualTo(source.CorrelationId));
        }
    }

    [Test]
    public async Task Splitter_EmptyResult_PublishesNothing()
    {
        var broker = new MockEndpoint("splitter-empty");
        var strategy = new FuncSplitStrategy<string>(_ => []);

        var splitter = new MessageSplitter<string>(
            strategy, broker,
            Options.Create(new SplitterOptions { TargetTopic = "out" }),
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create("", "S", "T");
        var result = await splitter.SplitAsync(source);

        Assert.That(result.SplitEnvelopes, Is.Empty);
        broker.AssertNoneReceived();
    }

    // ── 2. Aggregator ───────────────────────────────────────────────────

    [Test]
    public async Task Aggregator_CollectsAndPublishes_WhenGroupComplete()
    {
        // The Aggregator collects individual messages by CorrelationId,
        // combines them when complete, and publishes the aggregate.
        var broker = new MockEndpoint("aggregator");
        var store = new InMemoryMessageAggregateStore<int>();
        var completionStrategy = new CountCompletionStrategy<int>(3);
        var aggregationStrategy = new FuncAggregationStrategy<int, int>(
            items => items.Sum());

        var aggregator = new MessageAggregator<int, int>(
            store, completionStrategy, aggregationStrategy, broker,
            Options.Create(new AggregatorOptions
            {
                TargetTopic = "aggregated.totals",
                ExpectedCount = 3
            }),
            NullLogger<MessageAggregator<int, int>>.Instance);

        var correlationId = Guid.NewGuid();
        var e1 = CreateCorrelated(10, correlationId);
        var e2 = CreateCorrelated(20, correlationId);
        var e3 = CreateCorrelated(30, correlationId);

        var r1 = await aggregator.AggregateAsync(e1);
        Assert.That(r1.IsComplete, Is.False);

        var r2 = await aggregator.AggregateAsync(e2);
        Assert.That(r2.IsComplete, Is.False);

        var r3 = await aggregator.AggregateAsync(e3);
        Assert.That(r3.IsComplete, Is.True);

        // Published the aggregate (sum = 60)
        broker.AssertReceivedCount(1);
        broker.AssertReceivedOnTopic("aggregated.totals", 1);
        var received = broker.GetReceived<int>(0);
        Assert.That(received.Payload, Is.EqualTo(60));
    }

    [Test]
    public async Task Aggregator_DifferentCorrelations_SeparateGroups()
    {
        var broker = new MockEndpoint("agg-groups");
        var store = new InMemoryMessageAggregateStore<string>();
        var completionStrategy = new CountCompletionStrategy<string>(2);
        var aggregationStrategy = new FuncAggregationStrategy<string, string>(
            items => string.Join("+", items));

        var aggregator = new MessageAggregator<string, string>(
            store, completionStrategy, aggregationStrategy, broker,
            Options.Create(new AggregatorOptions
            {
                TargetTopic = "agg.out",
                ExpectedCount = 2
            }),
            NullLogger<MessageAggregator<string, string>>.Instance);

        var group1 = Guid.NewGuid();
        var group2 = Guid.NewGuid();

        await aggregator.AggregateAsync(CreateCorrelated("A", group1));
        await aggregator.AggregateAsync(CreateCorrelated("X", group2));
        await aggregator.AggregateAsync(CreateCorrelated("B", group1)); // group1 complete

        // Only group1 published (A+B)
        broker.AssertReceivedCount(1);

        await aggregator.AggregateAsync(CreateCorrelated("Y", group2)); // group2 complete
        broker.AssertReceivedCount(2);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static IntegrationEnvelope<T> CreateCorrelated<T>(T payload, Guid correlationId) =>
        new()
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            CausationId = Guid.Empty,
            Source = "Test",
            MessageType = "Item",
            Timestamp = DateTimeOffset.UtcNow,
            Payload = payload,
        };
}
