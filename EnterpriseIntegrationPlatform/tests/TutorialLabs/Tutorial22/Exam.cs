// ============================================================================
// Tutorial 22 – Scatter-Gather (Exam)
// ============================================================================
// Coding challenges: multi-recipient gather with mixed success/error
// responses, timeout behaviour with partial results, and duplicate
// correlation ID rejection.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.ScatterGather;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial22;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Mixed Success and Error Responses ───────────────────────

    [Test]
    public async Task Challenge1_GatherMixedResponses_AllIncludedInResult()
    {
        // Scatter to 2 recipients. One succeeds, one fails.
        // Both responses should appear in the result.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 10_000 });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(
            correlationId, "compute",
            new List<string> { "svc-fast", "svc-flaky" });

        var scatterTask = sg.ScatterGatherAsync(request);

        await Task.Delay(100);

        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("svc-fast", "ok", DateTimeOffset.UtcNow, true, null));

        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("svc-flaky", "", DateTimeOffset.UtcNow, false, "Internal error"));

        var result = await scatterTask;

        Assert.That(result.Responses.Count, Is.EqualTo(2));
        Assert.That(result.TimedOut, Is.False);

        var successResp = result.Responses.First(r => r.Recipient == "svc-fast");
        Assert.That(successResp.IsSuccess, Is.True);

        var errorResp = result.Responses.First(r => r.Recipient == "svc-flaky");
        Assert.That(errorResp.IsSuccess, Is.False);
        Assert.That(errorResp.ErrorMessage, Is.EqualTo("Internal error"));
    }

    // ── Challenge 2: Timeout Returns Partial Responses ──────────────────────

    [Test]
    public async Task Challenge2_Timeout_ReturnsPartialResponses()
    {
        // Scatter to 2 recipients with a short timeout. Only 1 responds in time.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 500 });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(
            correlationId, "urgent",
            new List<string> { "svc-quick", "svc-slow" });

        var scatterTask = sg.ScatterGatherAsync(request);

        await Task.Delay(50);
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("svc-quick", "done", DateTimeOffset.UtcNow, true, null));

        // svc-slow never responds — the timeout expires.
        var result = await scatterTask;

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Responses.Count, Is.EqualTo(1));
        Assert.That(result.Responses[0].Recipient, Is.EqualTo("svc-quick"));
        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
    }

    // ── Challenge 3: Duplicate CorrelationId Throws ─────────────────────────

    [Test]
    public async Task Challenge3_DuplicateCorrelationId_ThrowsInvalidOperation()
    {
        // Starting two scatter-gather operations with the same CorrelationId
        // should throw InvalidOperationException on the second call.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 5000 });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(
            correlationId, "first",
            new List<string> { "svc-a" });

        // First call starts gathering (will block waiting for response).
        var firstTask = sg.ScatterGatherAsync(request);
        await Task.Delay(100);

        // Second call with the same correlationId should throw.
        var secondRequest = new ScatterRequest<string>(
            correlationId, "second",
            new List<string> { "svc-b" });

        Assert.ThrowsAsync<InvalidOperationException>(
            () => sg.ScatterGatherAsync(secondRequest));

        // Complete the first task by submitting a response.
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("svc-a", "done", DateTimeOffset.UtcNow, true, null));
        await firstTask;
    }
}
