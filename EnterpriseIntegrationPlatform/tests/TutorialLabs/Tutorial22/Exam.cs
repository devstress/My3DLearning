// ============================================================================
// Tutorial 22 – Scatter-Gather (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Mixed success/failure responses are both collected
//   🟡 Intermediate  — Duration is tracked and greater than zero
//   🔴 Advanced      — Concurrent scatter-gather operations isolate by CorrelationId
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Processing.ScatterGather;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial22;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Mixed success and failure responses ────────────────
    //
    // SCENARIO: Two recipients are scattered to: "ok-svc" responds successfully
    //           and "fail-svc" responds with an error. The gather phase must
    //           collect both and report one success, one failure, no timeout.
    //
    // WHAT YOU PROVE: The scatter-gather correctly collects mixed success/failure
    //                 responses without timing out.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_MixedResponses_SuccessAndFailure()
    {
        await using var output = new MockEndpoint("exam-sg");
        var sg = CreateScatterGatherer(output, timeoutMs: 500);
        var correlationId = Guid.NewGuid();
        // TODO: Create a ScatterRequest with appropriate configuration
        dynamic request = null!;

        var task = sg.ScatterGatherAsync(request);
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("ok-svc", "result", DateTimeOffset.UtcNow, true, null));
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("fail-svc", default!, DateTimeOffset.UtcNow, false, "Timeout"));
        var result = await task;

        Assert.That(result.Responses.Count, Is.EqualTo(2));
        Assert.That(result.Responses.Count(r => r.IsSuccess), Is.EqualTo(1));
        Assert.That(result.Responses.Count(r => !r.IsSuccess), Is.EqualTo(1));
        Assert.That(result.TimedOut, Is.False);
        output.AssertReceivedCount(2);
    }

    // ── 🟡 INTERMEDIATE — Duration tracking ────────────────────────────
    //
    // SCENARIO: A single recipient responds quickly. The result's Duration
    //           must be greater than zero and TimedOut must be false.
    //
    // WHAT YOU PROVE: The scatter-gather accurately tracks how long the
    //                 operation takes from scatter to final gather.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_Duration_IsTracked()
    {
        await using var output = new MockEndpoint("exam-dur");
        var sg = CreateScatterGatherer(output, timeoutMs: 500);
        var correlationId = Guid.NewGuid();
        // TODO: Create a ScatterRequest with appropriate configuration
        dynamic request = null!;

        var task = sg.ScatterGatherAsync(request);
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("topic-1", "done", DateTimeOffset.UtcNow, true, null));
        var result = await task;

        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(result.TimedOut, Is.False);
    }

    // ── 🔴 ADVANCED — Concurrent operations isolate by CorrelationId ───
    //
    // SCENARIO: Two scatter-gather operations run concurrently with different
    //           CorrelationIds. Responses are submitted out of order (corr2
    //           first, then corr1). Each result must contain only its own
    //           responses with the correct payload.
    //
    // WHAT YOU PROVE: The scatter-gather correctly isolates concurrent
    //                 operations so responses never cross-contaminate.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_ConcurrentOperations_IsolateByCorrelation()
    {
        await using var output = new MockEndpoint("exam-conc");
        var sg = CreateScatterGatherer(output, timeoutMs: 1000);

        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        // TODO: Create a ScatterRequest with appropriate configuration
        dynamic req1 = null!;
        // TODO: Create a ScatterRequest with appropriate configuration
        dynamic req2 = null!;

        var task1 = sg.ScatterGatherAsync(req1);
        var task2 = sg.ScatterGatherAsync(req2);

        await sg.SubmitResponseAsync(corr2,
            new GatherResponse<string>("svc-b", "r2", DateTimeOffset.UtcNow, true, null));
        await sg.SubmitResponseAsync(corr1,
            new GatherResponse<string>("svc-a", "r1", DateTimeOffset.UtcNow, true, null));

        var result1 = await task1;
        var result2 = await task2;

        Assert.That(result1.CorrelationId, Is.EqualTo(corr1));
        Assert.That(result2.CorrelationId, Is.EqualTo(corr2));
        Assert.That(result1.Responses[0].Payload, Is.EqualTo("r1"));
        Assert.That(result2.Responses[0].Payload, Is.EqualTo("r2"));
        output.AssertReceivedCount(2);
    }

    private static ScatterGatherer<string, string> CreateScatterGatherer(
        MockEndpoint output, int timeoutMs)
    {
        var options = Options.Create(new ScatterGatherOptions
        {
            TimeoutMs = timeoutMs,
            MaxRecipients = 50,
        });

        return new ScatterGatherer<string, string>(
            output, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);
    }
}
#endif
