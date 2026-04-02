using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.ScatterGather;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.ScatterGatherTests;

[TestFixture]
public class ScatterGathererTests
{
    private IMessageBrokerProducer _producer = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private ScatterGatherer<string, string> BuildScatterGatherer(
        ScatterGatherOptions? options = null)
    {
        options ??= new ScatterGatherOptions();
        return new ScatterGatherer<string, string>(
            _producer,
            Options.Create(options),
            NullLogger<ScatterGatherer<string, string>>.Instance);
    }

    private static ScatterRequest<string> BuildRequest(
        IReadOnlyList<string>? recipients = null,
        string payload = "request-data",
        Guid? correlationId = null)
    {
        return new ScatterRequest<string>(
            correlationId ?? Guid.NewGuid(),
            payload,
            recipients ?? ["topic-a", "topic-b"]);
    }

    // ------------------------------------------------------------------ //
    // All recipients respond
    // ------------------------------------------------------------------ //

    [Test]
    public async Task ScatterGatherAsync_AllRecipientsRespond_ReturnsAllResponses()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a", "topic-b"]);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));
        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-b", "resp-b", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.Responses, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ScatterGatherAsync_AllRecipientsRespond_TimedOutIsFalse()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a"]);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.TimedOut, Is.False);
    }

    [Test]
    public async Task ScatterGatherAsync_AllRecipientsRespond_CorrelationIdIsPreserved()
    {
        var correlationId = Guid.NewGuid();
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a"], correlationId: correlationId);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task ScatterGatherAsync_AllRecipientsRespond_DurationIsPositive()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a"]);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
    }

    [Test]
    public async Task ScatterGatherAsync_AllRecipientsRespond_PublishesToAllTopics()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a", "topic-b", "topic-c"]);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", "a", DateTimeOffset.UtcNow, true, null));
        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-b", "b", DateTimeOffset.UtcNow, true, null));
        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-c", "c", DateTimeOffset.UtcNow, true, null));

        await task;

        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(), "topic-a", Arg.Any<CancellationToken>());
        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(), "topic-b", Arg.Any<CancellationToken>());
        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(), "topic-c", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ScatterGatherAsync_AllRecipientsRespond_ResponsePayloadsPreserved()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a"]);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", "expected-payload", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.Responses[0].Payload, Is.EqualTo("expected-payload"));
    }

    // ------------------------------------------------------------------ //
    // Partial timeout
    // ------------------------------------------------------------------ //

    [Test]
    public async Task ScatterGatherAsync_PartialTimeout_ReturnsOnlyReceivedResponses()
    {
        var options = new ScatterGatherOptions { TimeoutMs = 200 };
        var sut = BuildScatterGatherer(options);
        var request = BuildRequest(["topic-a", "topic-b"]);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.Responses, Has.Count.EqualTo(1));
        Assert.That(result.Responses[0].Recipient, Is.EqualTo("topic-a"));
    }

    [Test]
    public async Task ScatterGatherAsync_PartialTimeout_TimedOutIsTrue()
    {
        var options = new ScatterGatherOptions { TimeoutMs = 200 };
        var sut = BuildScatterGatherer(options);
        var request = BuildRequest(["topic-a", "topic-b"]);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.TimedOut, Is.True);
    }

    [Test]
    public async Task ScatterGatherAsync_NoResponses_TimedOutIsTrue()
    {
        var options = new ScatterGatherOptions { TimeoutMs = 200 };
        var sut = BuildScatterGatherer(options);
        var request = BuildRequest(["topic-a"]);

        var result = await sut.ScatterGatherAsync(request);

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Responses, Is.Empty);
    }

    // ------------------------------------------------------------------ //
    // Empty recipients
    // ------------------------------------------------------------------ //

    [Test]
    public async Task ScatterGatherAsync_EmptyRecipients_ReturnsEmptyResult()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(recipients: []);

        var result = await sut.ScatterGatherAsync(request);

        Assert.That(result.Responses, Is.Empty);
        Assert.That(result.TimedOut, Is.False);
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public async Task ScatterGatherAsync_EmptyRecipients_DoesNotPublish()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(recipients: []);

        await sut.ScatterGatherAsync(request);

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ //
    // Validation
    // ------------------------------------------------------------------ //

    [Test]
    public void ScatterGatherAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = BuildScatterGatherer();

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.ScatterGatherAsync(null!));
    }

    [Test]
    public void ScatterGatherAsync_ExceedsMaxRecipients_ThrowsArgumentException()
    {
        var options = new ScatterGatherOptions { MaxRecipients = 2 };
        var sut = BuildScatterGatherer(options);
        var request = BuildRequest(["a", "b", "c"]);

        Assert.ThrowsAsync<ArgumentException>(
            async () => await sut.ScatterGatherAsync(request));
    }

    [Test]
    public void ScatterGatherAsync_DuplicateCorrelationId_ThrowsInvalidOperationException()
    {
        var options = new ScatterGatherOptions { TimeoutMs = 5000 };
        var sut = BuildScatterGatherer(options);
        var correlationId = Guid.NewGuid();
        var request1 = BuildRequest(["topic-a"], correlationId: correlationId);
        var request2 = BuildRequest(["topic-b"], correlationId: correlationId);

        // Start first scatter-gather without completing it
        _ = sut.ScatterGatherAsync(request1);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ScatterGatherAsync(request2));
    }

    // ------------------------------------------------------------------ //
    // Response submission
    // ------------------------------------------------------------------ //

    [Test]
    public async Task SubmitResponseAsync_UnknownCorrelationId_ReturnsFalse()
    {
        var sut = BuildScatterGatherer();

        var result = await sut.SubmitResponseAsync(
            Guid.NewGuid(),
            new GatherResponse<string>("topic-a", "resp", DateTimeOffset.UtcNow, true, null));

        Assert.That(result, Is.False);
    }

    [Test]
    public void SubmitResponseAsync_NullResponse_ThrowsArgumentNullException()
    {
        var sut = BuildScatterGatherer();

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.SubmitResponseAsync(Guid.NewGuid(), null!));
    }

    [Test]
    public async Task SubmitResponseAsync_ActiveGather_ReturnsTrue()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a"]);

        var task = sut.ScatterGatherAsync(request);

        var accepted = await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));

        await task;

        Assert.That(accepted, Is.True);
    }

    // ------------------------------------------------------------------ //
    // Error responses
    // ------------------------------------------------------------------ //

    [Test]
    public async Task ScatterGatherAsync_ErrorResponse_IncludedInResult()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a"]);

        var task = sut.ScatterGatherAsync(request);

        await sut.SubmitResponseAsync(request.CorrelationId,
            new GatherResponse<string>("topic-a", default!, DateTimeOffset.UtcNow, false, "Service unavailable"));

        var result = await task;

        Assert.That(result.Responses[0].IsSuccess, Is.False);
        Assert.That(result.Responses[0].ErrorMessage, Is.EqualTo("Service unavailable"));
    }

    // ------------------------------------------------------------------ //
    // Cancellation
    // ------------------------------------------------------------------ //

    [Test]
    public void ScatterGatherAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var sut = BuildScatterGatherer();
        var request = BuildRequest(["topic-a"]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(
            async () => await sut.ScatterGatherAsync(request, cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    // ------------------------------------------------------------------ //
    // Options defaults
    // ------------------------------------------------------------------ //

    [Test]
    public void ScatterGatherOptions_Defaults_TimeoutMs30000()
    {
        var options = new ScatterGatherOptions();

        Assert.That(options.TimeoutMs, Is.EqualTo(30_000));
    }

    [Test]
    public void ScatterGatherOptions_Defaults_MaxRecipients50()
    {
        var options = new ScatterGatherOptions();

        Assert.That(options.MaxRecipients, Is.EqualTo(50));
    }
}
