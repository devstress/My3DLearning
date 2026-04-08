// ============================================================================
// Tutorial 27 – Resequencer (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Large out-of-order batch released in correct sequence
//   🟡 Intermediate  — Interleaved sequences each release independently
//   🔴 Advanced      — Timeout partial release then complete a new sequence
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Resequencer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial27;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Large out-of-order batch ──────────────────────────
    //
    // SCENARIO: Ten messages arrive in reverse order. The resequencer must
    //           buffer them all and release them in correct sequence when
    //           the final message completes the set.
    //
    // WHAT YOU PROVE: The resequencer handles a large batch and releases
    //                 all messages in ascending sequence number order.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_LargeOutOfOrderBatch_ReleasedInSequence()
    {
        await using var output = new MockEndpoint("reseq-batch");
        // TODO: Create a MessageResequencer with appropriate configuration
        dynamic resequencer = null!;
        var correlationId = Guid.NewGuid();
        const int total = 10;

        var indices = Enumerable.Range(0, total).Reverse().ToList();
        IReadOnlyList<IntegrationEnvelope<string>> released = [];
        foreach (var i in indices)
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic env = null!;
            released = resequencer.Accept(env);
        }

        Assert.That(released, Has.Count.EqualTo(total));
        for (var i = 0; i < total; i++)
            Assert.That(released[i].SequenceNumber, Is.EqualTo(i));

        foreach (var env in released)
            // TODO: await output.PublishAsync(...)

        output.AssertReceivedOnTopic("ordered", total);
    }

    // ── 🟡 INTERMEDIATE — Interleaved sequences ───────────────────────
    //
    // SCENARIO: Messages from two independent sequences (corrA and corrB)
    //           arrive interleaved. Each sequence must be tracked and
    //           released independently when complete.
    //
    // WHAT YOU PROVE: The resequencer maintains separate buffers per
    //                 CorrelationId and releases each sequence correctly.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_InterleavedSequences_EachReleasedIndependently()
    {
        await using var output = new MockEndpoint("reseq-interleave");
        // TODO: Create a MessageResequencer with appropriate configuration
        dynamic resequencer = null!;

        var corrA = Guid.NewGuid();
        var corrB = Guid.NewGuid();

        resequencer.Accept(CreateEnvelope("A1", corrA, 1, 2));
        resequencer.Accept(CreateEnvelope("B0", corrB, 0, 2));

        var releasedA = resequencer.Accept(CreateEnvelope("A0", corrA, 0, 2));
        Assert.That(releasedA, Has.Count.EqualTo(2));
        Assert.That(releasedA[0].Payload, Is.EqualTo("A0"));

        var releasedB = resequencer.Accept(CreateEnvelope("B1", corrB, 1, 2));
        Assert.That(releasedB, Has.Count.EqualTo(2));
        Assert.That(releasedB[0].Payload, Is.EqualTo("B0"));

        foreach (var env in releasedA.Concat(releasedB))
            // TODO: await output.PublishAsync(...)

        output.AssertReceivedOnTopic("interleaved", 4);
    }

    // ── 🔴 ADVANCED — Timeout partial release then new sequence ────────
    //
    // SCENARIO: An incomplete sequence is force-released via timeout. Then
    //           a new, separate sequence arrives and completes normally.
    //           The resequencer must handle both flows cleanly.
    //
    // WHAT YOU PROVE: Timeout correctly flushes partial state, and the
    //                 resequencer can accept and complete new sequences
    //                 afterwards with ActiveSequenceCount returning to 0.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_TimeoutPartialRelease_ThenCompleteNewSequence()
    {
        await using var output = new MockEndpoint("reseq-timeout");
        // TODO: Create a MessageResequencer with appropriate configuration
        dynamic resequencer = null!;

        var corrOld = Guid.NewGuid();
        resequencer.Accept(CreateEnvelope("old-0", corrOld, 0, 5));
        resequencer.Accept(CreateEnvelope("old-3", corrOld, 3, 5));

        var partial = resequencer.ReleaseOnTimeout<string>(corrOld);
        Assert.That(partial, Has.Count.EqualTo(2));

        foreach (var env in partial)
            // TODO: await output.PublishAsync(...)

        var corrNew = Guid.NewGuid();
        resequencer.Accept(CreateEnvelope("new-1", corrNew, 1, 2));
        var complete = resequencer.Accept(CreateEnvelope("new-0", corrNew, 0, 2));
        Assert.That(complete, Has.Count.EqualTo(2));

        foreach (var env in complete)
            // TODO: await output.PublishAsync(...)

        output.AssertReceivedOnTopic("partial", 2);
        output.AssertReceivedOnTopic("complete", 2);
        Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(0));
    }

    private static IntegrationEnvelope<string> CreateEnvelope(
        string payload, Guid correlationId, int sequenceNumber, int totalCount) =>
        IntegrationEnvelope<string>.Create(payload, "Svc", "evt") with
        {
            CorrelationId = correlationId,
            SequenceNumber = sequenceNumber,
            TotalCount = totalCount,
        };
}
#endif
