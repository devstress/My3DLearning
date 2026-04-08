// ============================================================================
// Tutorial 21 – Aggregator (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Aggregator pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Interleaved groups with different CorrelationIds complete independently
//   🟡 Intermediate — Metadata key conflict where later envelope overrides earlier
//   🔴 Advanced     — Duplicate message by MessageId is idempotently rejected
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint
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
public sealed class Exam
{
    // ── 🟢 STARTER — Interleaved groups complete independently ──────────
    //
    // SCENARIO: Two order groups (corrA and corrB) with expectedCount=2 are
    //           interleaved: a1, b1, a2, b2. Each group must complete
    //           independently and publish its own aggregate.
    //
    // WHAT YOU PROVE: The aggregator correctly isolates groups by CorrelationId
    //                 even when messages arrive interleaved.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_InterleavedGroups_CompleteIndependently()
    {
        await using var output = new MockEndpoint("exam-agg");
        var aggregator = CreateAggregator(output, expectedCount: 2);

        var corrA = Guid.NewGuid();
        var corrB = Guid.NewGuid();

        var a1 = IntegrationEnvelope<string>.Create("a1", "svc", "t", corrA);
        var b1 = IntegrationEnvelope<string>.Create("b1", "svc", "t", corrB);
        var a2 = IntegrationEnvelope<string>.Create("a2", "svc", "t", corrA);
        var b2 = IntegrationEnvelope<string>.Create("b2", "svc", "t", corrB);

        Assert.That((await aggregator.AggregateAsync(a1)).IsComplete, Is.False);
        Assert.That((await aggregator.AggregateAsync(b1)).IsComplete, Is.False);
        Assert.That((await aggregator.AggregateAsync(a2)).IsComplete, Is.True);
        Assert.That((await aggregator.AggregateAsync(b2)).IsComplete, Is.True);

        output.AssertReceivedOnTopic("aggregated-topic", 2);
    }

    // ── 🟡 INTERMEDIATE — Metadata conflict: later overrides earlier ────
    //
    // SCENARIO: Two envelopes share the same metadata key "key" but with
    //           different values ("first" and "second"). The aggregate must
    //           retain the value from the later envelope.
    //
    // WHAT YOU PROVE: When metadata keys conflict during aggregation, the
    //                 later envelope's value wins.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_MetadataConflict_LaterOverridesEarlier()
    {
        await using var output = new MockEndpoint("exam-meta");
        var aggregator = CreateAggregator(output, expectedCount: 2);
        var correlationId = Guid.NewGuid();

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "t", correlationId) with
        {
            Metadata = new Dictionary<string, string> { ["key"] = "first" },
        };
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "t", correlationId) with
        {
            Metadata = new Dictionary<string, string> { ["key"] = "second" },
        };

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        Assert.That(result.AggregateEnvelope!.Metadata["key"], Is.EqualTo("second"));
        output.AssertReceivedOnTopic("aggregated-topic", 1);
    }

    // ── 🔴 ADVANCED — Duplicate message is idempotently rejected ────────
    //
    // SCENARIO: Envelope e1 is sent twice before e2 arrives. The duplicate
    //           must be ignored (idempotent), so the group still needs e2
    //           to complete. Only one aggregate is published.
    //
    // WHAT YOU PROVE: The aggregator deduplicates by MessageId and does not
    //                 double-count retransmitted messages.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_DuplicateMessage_IsIdempotent()
    {
        await using var output = new MockEndpoint("exam-dup");
        var aggregator = CreateAggregator(output, expectedCount: 2);
        var correlationId = Guid.NewGuid();

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "t", correlationId);
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "t", correlationId);

        await aggregator.AggregateAsync(e1);
        // Resend e1 — duplicate by MessageId should be ignored
        var dupResult = await aggregator.AggregateAsync(e1);
        Assert.That(dupResult.IsComplete, Is.False);
        Assert.That(dupResult.ReceivedCount, Is.EqualTo(1));

        var final = await aggregator.AggregateAsync(e2);
        Assert.That(final.IsComplete, Is.True);
        Assert.That(final.ReceivedCount, Is.EqualTo(2));

        output.AssertReceivedOnTopic("aggregated-topic", 1);
    }

    private static MessageAggregator<string, string> CreateAggregator(
        MockEndpoint output, int expectedCount)
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
            store, completion, strategy, output, options,
            NullLogger<MessageAggregator<string, string>>.Instance);
    }
}
