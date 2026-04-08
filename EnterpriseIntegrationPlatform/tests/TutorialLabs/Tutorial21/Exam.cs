// ============================================================================
// Tutorial 21 – Aggregator (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Interleaved groups with different CorrelationIds complete independently
//   🟡 Intermediate  — Metadata key conflict where later envelope overrides earlier
//   🔴 Advanced      — Duplicate message by MessageId is idempotently rejected
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic a1 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic b1 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic a2 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic b2 = null!;

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

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e1 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e2 = null!;

        await aggregator.AggregateAsync(e1);
        // TODO: var result = await aggregator.AggregateAsync(...)
        dynamic result = null!;

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

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e1 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e2 = null!;

        await aggregator.AggregateAsync(e1);
        // Resend e1 — duplicate by MessageId should be ignored
        // TODO: var dupResult = await aggregator.AggregateAsync(...)
        dynamic dupResult = null!;
        Assert.That(dupResult.IsComplete, Is.False);
        Assert.That(dupResult.ReceivedCount, Is.EqualTo(1));

        // TODO: var final = await aggregator.AggregateAsync(...)
        dynamic final = null!;
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
#endif
