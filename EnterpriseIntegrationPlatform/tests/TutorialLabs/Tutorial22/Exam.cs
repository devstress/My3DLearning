// ============================================================================
// Tutorial 22 – Scatter-Gather (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Scatter-Gather pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Mixed success/failure responses are both collected
//   🟡 Intermediate — Duration is tracked and greater than zero
//   🔴 Advanced     — Concurrent scatter-gather operations isolate by CorrelationId
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.ScatterGather;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

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
        var request = new ScatterRequest<string>(correlationId, "req",
            new[] { "ok-svc", "fail-svc" });

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
        var request = new ScatterRequest<string>(correlationId, "req",
            new[] { "topic-1" });

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
        var req1 = new ScatterRequest<string>(corr1, "req1", new[] { "svc-a" });
        var req2 = new ScatterRequest<string>(corr2, "req2", new[] { "svc-b" });

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
