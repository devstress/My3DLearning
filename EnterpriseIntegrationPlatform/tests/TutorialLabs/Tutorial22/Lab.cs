// ============================================================================
// Tutorial 22 – Scatter-Gather (Lab)
// ============================================================================
// EIP Pattern: Scatter-Gather.
// Real Integrations: ScatterGatherer with NatsBrokerEndpoint
// (real NATS JetStream via Aspire) as producer.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.ScatterGather;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial22;

[TestFixture]
public sealed class Lab
{
    // ── 1. Scatter Phase ─────────────────────────────────────────────

    [Test]
    public async Task Scatter_PublishesToAllRecipients()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t22-scatter");
        var sg = CreateScatterGatherer(nats, timeoutMs: 500);
        var correlationId = Guid.NewGuid();
        var supplierA = AspireFixture.UniqueTopic("t22-supplier-a");
        var supplierB = AspireFixture.UniqueTopic("t22-supplier-b");
        var supplierC = AspireFixture.UniqueTopic("t22-supplier-c");
        var request = new ScatterRequest<string>(correlationId, "quote-request",
            new[] { supplierA, supplierB, supplierC });

        // Start scatter-gather in background; submit responses immediately
        var task = sg.ScatterGatherAsync(request);

        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>(supplierA, "price-a", DateTimeOffset.UtcNow, true, null));
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>(supplierB, "price-b", DateTimeOffset.UtcNow, true, null));
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>(supplierC, "price-c", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.Responses.Count, Is.EqualTo(3));
        Assert.That(result.TimedOut, Is.False);
        nats.AssertReceivedOnTopic(supplierA, 1);
        nats.AssertReceivedOnTopic(supplierB, 1);
        nats.AssertReceivedOnTopic(supplierC, 1);
    }

    [Test]
    public async Task Scatter_EmptyRecipients_ReturnsImmediately()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t22-empty");
        var sg = CreateScatterGatherer(nats, timeoutMs: 500);
        var request = new ScatterRequest<string>(Guid.NewGuid(), "data", Array.Empty<string>());

        var result = await sg.ScatterGatherAsync(request);

        Assert.That(result.Responses.Count, Is.EqualTo(0));
        Assert.That(result.TimedOut, Is.False);
        nats.AssertNoneReceived();
    }


    // ── 2. Gather & Timeout ──────────────────────────────────────────

    [Test]
    public async Task Gather_TimesOut_ReturnsPartialResponses()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t22-timeout");
        var sg = CreateScatterGatherer(nats, timeoutMs: 200);
        var correlationId = Guid.NewGuid();
        var topicFast = AspireFixture.UniqueTopic("t22-fast");
        var topicSlow = AspireFixture.UniqueTopic("t22-slow");
        var request = new ScatterRequest<string>(correlationId, "req",
            new[] { topicFast, topicSlow });

        var task = sg.ScatterGatherAsync(request);

        // Only fast responds
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>(topicFast, "done", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Responses.Count, Is.EqualTo(1));
        Assert.That(result.Responses[0].Recipient, Is.EqualTo(topicFast));
    }

    [Test]
    public async Task Gather_PreservesCorrelationId()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t22-corr");
        var sg = CreateScatterGatherer(nats, timeoutMs: 500);
        var correlationId = Guid.NewGuid();
        var topic = AspireFixture.UniqueTopic("t22-topic");
        var request = new ScatterRequest<string>(correlationId, "data",
            new[] { topic });

        var task = sg.ScatterGatherAsync(request);
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>(topic, "resp", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
    }


    // ── 3. Edge Cases ────────────────────────────────────────────────

    [Test]
    public async Task SubmitResponse_UnknownCorrelation_ReturnsFalse()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t22-unknown");
        var sg = CreateScatterGatherer(nats, timeoutMs: 500);

        var accepted = await sg.SubmitResponseAsync(Guid.NewGuid(),
            new GatherResponse<string>("x", "data", DateTimeOffset.UtcNow, true, null));

        Assert.That(accepted, Is.False);
    }

    [Test]
    public async Task Scatter_ExceedsMaxRecipients_Throws()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t22-maxrecip");
        var sg = CreateScatterGatherer(nats, timeoutMs: 500, maxRecipients: 2);
        var request = new ScatterRequest<string>(Guid.NewGuid(), "data",
            new[] { "a", "b", "c" });

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await sg.ScatterGatherAsync(request));
    }

    private static ScatterGatherer<string, string> CreateScatterGatherer(
        NatsBrokerEndpoint nats, int timeoutMs, int maxRecipients = 50)
    {
        var options = Options.Create(new ScatterGatherOptions
        {
            TimeoutMs = timeoutMs,
            MaxRecipients = maxRecipients,
        });

        return new ScatterGatherer<string, string>(
            nats, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);
    }
}
