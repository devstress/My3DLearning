// ============================================================================
// Tutorial 22 – Scatter-Gather (Lab)
// ============================================================================
// This lab exercises the ScatterGatherer: empty recipients, max-recipient
// validation, scatter publishing, response submission, and result assembly.
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
public sealed class Lab
{
    // ── Empty Recipients Returns Immediately ─────────────────────────────────

    [Test]
    public async Task Scatter_EmptyRecipients_ReturnsEmptyResult()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 5000 });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var request = new ScatterRequest<string>(
            Guid.NewGuid(), "ping", new List<string>());

        var result = await sg.ScatterGatherAsync(request);

        Assert.That(result.Responses, Is.Empty);
        Assert.That(result.TimedOut, Is.False);
        Assert.That(result.Duration, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(1)));
    }

    // ── Max Recipients Exceeded Throws ───────────────────────────────────────

    [Test]
    public void Scatter_ExceedsMaxRecipients_ThrowsArgumentException()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions
        {
            MaxRecipients = 2,
            TimeoutMs = 5000,
        });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var request = new ScatterRequest<string>(
            Guid.NewGuid(), "payload",
            new List<string> { "t1", "t2", "t3" });

        Assert.ThrowsAsync<ArgumentException>(() => sg.ScatterGatherAsync(request));
    }

    // ── Scatter Publishes To All Recipients ──────────────────────────────────

    [Test]
    public async Task Scatter_PublishesToEachRecipientTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 500 });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var recipients = new List<string> { "svc-a", "svc-b" };
        var request = new ScatterRequest<string>(
            Guid.NewGuid(), "hello", recipients);

        // Scatter will publish to both topics then time out waiting for responses.
        await sg.ScatterGatherAsync(request);

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            "svc-a",
            Arg.Any<CancellationToken>());

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            "svc-b",
            Arg.Any<CancellationToken>());
    }

    // ── SubmitResponse For Unknown CorrelationId Returns False ────────────────

    [Test]
    public async Task SubmitResponse_UnknownCorrelation_ReturnsFalse()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 5000 });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var response = new GatherResponse<string>(
            "svc-a", "pong", DateTimeOffset.UtcNow, true, null);

        var accepted = await sg.SubmitResponseAsync(Guid.NewGuid(), response);

        Assert.That(accepted, Is.False);
    }

    // ── Full Scatter-Gather With Submitted Responses ─────────────────────────

    [Test]
    public async Task Scatter_ReceivesAllResponses_CompletesBeforeTimeout()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 10_000 });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(
            correlationId, "query", new List<string> { "svc-a" });

        // Start scatter-gather on a background task.
        var scatterTask = sg.ScatterGatherAsync(request);

        // Give scatter time to publish, then submit a response.
        await Task.Delay(100);
        var submitted = await sg.SubmitResponseAsync(
            correlationId,
            new GatherResponse<string>("svc-a", "answer", DateTimeOffset.UtcNow, true, null));

        var result = await scatterTask;

        Assert.That(submitted, Is.True);
        Assert.That(result.Responses.Count, Is.EqualTo(1));
        Assert.That(result.Responses[0].Payload, Is.EqualTo("answer"));
        Assert.That(result.TimedOut, Is.False);
    }

    // ── ScatterGatherResult Preserves CorrelationId ──────────────────────────

    [Test]
    public async Task Result_CorrelationId_MatchesRequest()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 500 });

        var sg = new ScatterGatherer<string, string>(
            producer, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(
            correlationId, "payload", new List<string>());

        var result = await sg.ScatterGatherAsync(request);

        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
    }

    // ── Options Default Values ──────────────────────────────────────────────

    [Test]
    public void Options_DefaultValues_AreCorrect()
    {
        var opts = new ScatterGatherOptions();

        Assert.That(opts.TimeoutMs, Is.EqualTo(30_000));
        Assert.That(opts.MaxRecipients, Is.EqualTo(50));
    }
}
