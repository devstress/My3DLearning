// ============================================================================
// Tutorial 21 – Aggregator (Exam)
// ============================================================================
// Coding challenges: accumulate order line items into a batch, verify
// idempotent deduplication, and confirm that TargetSource overrides the
// first envelope's source in the aggregate output.
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
public sealed class Exam
{
    // ── Challenge 1: Order Line Aggregation ──────────────────────────────────

    [Test]
    public async Task Challenge1_AggregateThreeLineItems_IntoSingleBatch()
    {
        // Aggregate 3 order line items into a single comma-separated batch.
        var store = new InMemoryMessageAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(3);
        var aggregation = Substitute.For<IAggregationStrategy<string, string>>();
        aggregation
            .Aggregate(Arg.Any<IReadOnlyList<string>>())
            .Returns(ci =>
            {
                var items = ci.Arg<IReadOnlyList<string>>();
                return string.Join(";", items);
            });

        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new AggregatorOptions
        {
            TargetTopic = "order-batches",
            TargetMessageType = "order.batch",
            ExpectedCount = 3,
        });

        var aggregator = new MessageAggregator<string, string>(
            store, completion, aggregation, producer, options,
            NullLogger<MessageAggregator<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var e1 = IntegrationEnvelope<string>.Create("SKU-A", "OrderSvc", "line", correlationId: correlationId);
        var e2 = IntegrationEnvelope<string>.Create("SKU-B", "OrderSvc", "line", correlationId: correlationId);
        var e3 = IntegrationEnvelope<string>.Create("SKU-C", "OrderSvc", "line", correlationId: correlationId);

        var r1 = await aggregator.AggregateAsync(e1);
        var r2 = await aggregator.AggregateAsync(e2);
        var r3 = await aggregator.AggregateAsync(e3);

        Assert.That(r1.IsComplete, Is.False);
        Assert.That(r1.ReceivedCount, Is.EqualTo(1));
        Assert.That(r2.IsComplete, Is.False);
        Assert.That(r2.ReceivedCount, Is.EqualTo(2));
        Assert.That(r3.IsComplete, Is.True);
        Assert.That(r3.ReceivedCount, Is.EqualTo(3));
        Assert.That(r3.AggregateEnvelope!.Payload, Is.EqualTo("SKU-A;SKU-B;SKU-C"));

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            "order-batches",
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 2: Deduplication Via InMemoryStore ─────────────────────────

    [Test]
    public async Task Challenge2_DuplicateMessageId_IsIgnoredByStore()
    {
        // The InMemoryMessageAggregateStore should ignore a duplicate MessageId
        // so the group size stays at 1 despite adding the same envelope twice.
        var store = new InMemoryMessageAggregateStore<string>();
        var correlationId = Guid.NewGuid();

        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "Svc", "type", correlationId: correlationId);

        var group1 = await store.AddAsync(envelope);
        var group2 = await store.AddAsync(envelope);

        Assert.That(group1.Count, Is.EqualTo(1));
        Assert.That(group2.Count, Is.EqualTo(1));
    }

    // ── Challenge 3: TargetSource Overrides First Envelope Source ────────────

    [Test]
    public async Task Challenge3_TargetSource_OverridesEnvelopeSource()
    {
        // When AggregatorOptions.TargetSource is set, the aggregate envelope
        // should use that source instead of the first envelope's source.
        var store = new InMemoryMessageAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(1);
        var aggregation = Substitute.For<IAggregationStrategy<string, string>>();
        aggregation.Aggregate(Arg.Any<IReadOnlyList<string>>()).Returns("agg");

        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new AggregatorOptions
        {
            TargetTopic = "out",
            TargetSource = "AggregatorService",
            ExpectedCount = 1,
        });

        var aggregator = new MessageAggregator<string, string>(
            store, completion, aggregation, producer, options,
            NullLogger<MessageAggregator<string, string>>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "OriginalService", "msg.type");

        var result = await aggregator.AggregateAsync(envelope);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.AggregateEnvelope!.Source, Is.EqualTo("AggregatorService"));
    }
}
