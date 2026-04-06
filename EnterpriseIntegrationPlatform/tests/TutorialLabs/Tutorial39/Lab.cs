// ============================================================================
// Tutorial 39 – Message Lifecycle / System Management (Lab)
// ============================================================================
// This lab exercises SmartProxy, TestMessageGenerator, ControlBusPublisher,
// and their associated options and result records.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.SystemManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial39;

[TestFixture]
public sealed class Lab
{
    // ── SmartProxy Tracks Request and Increments OutstandingCount ────────────

    [Test]
    public void SmartProxy_TrackRequest_IncrementsOutstandingCount()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

        var envelope = CreateEnvelopeWithReplyTo("request", "Svc", "cmd.query", "reply-queue-1");

        var tracked = proxy.TrackRequest(envelope);

        Assert.That(tracked, Is.True);
        Assert.That(proxy.OutstandingCount, Is.EqualTo(1));
    }

    // ── SmartProxy Correlates Reply to Tracked Request ──────────────────────

    [Test]
    public void SmartProxy_CorrelateReply_ReturnsCorrelation()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

        var request = CreateEnvelopeWithReplyTo("request", "Svc", "cmd.query", "reply-queue");
        proxy.TrackRequest(request);

        // Create a reply with the same CorrelationId
        var reply = IntegrationEnvelope<string>.Create(
            "response", "ReplySvc", "cmd.response",
            correlationId: request.CorrelationId);

        var correlation = proxy.CorrelateReply(reply);

        Assert.That(correlation, Is.Not.Null);
        Assert.That(correlation!.CorrelationId, Is.EqualTo(request.CorrelationId));
        Assert.That(correlation.OriginalReplyTo, Is.EqualTo("reply-queue"));
        Assert.That(correlation.RequestMessageId, Is.EqualTo(request.MessageId));
        Assert.That(proxy.OutstandingCount, Is.EqualTo(0));
    }

    // ── SmartProxy Returns Null for Unknown Reply ───────────────────────────

    [Test]
    public void SmartProxy_CorrelateReply_ReturnsNull_ForUnknownReply()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

        var unknownReply = IntegrationEnvelope<string>.Create("data", "Svc", "unknown.reply");

        var correlation = proxy.CorrelateReply(unknownReply);

        Assert.That(correlation, Is.Null);
    }

    // ── TestMessageGenerator Publishes to Target Topic ──────────────────────

    [Test]
    public async Task TestMessageGenerator_PublishesToTargetTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var generator = new TestMessageGenerator(
            producer, NullLogger<TestMessageGenerator>.Instance);

        var result = await generator.GenerateAsync("test-topic", CancellationToken.None);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.TargetTopic, Is.EqualTo("test-topic"));
        Assert.That(result.MessageId, Is.Not.EqualTo(Guid.Empty));

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            "test-topic",
            Arg.Any<CancellationToken>());
    }

    // ── ControlBusOptions Shape ─────────────────────────────────────────────

    [Test]
    public void ControlBusOptions_Shape()
    {
        var opts = new ControlBusOptions();

        Assert.That(opts.ControlTopic, Is.EqualTo("eip.control-bus"));
        Assert.That(opts.ConsumerGroup, Is.EqualTo("control-bus-consumers"));
        Assert.That(opts.Source, Is.EqualTo("ControlBus"));
    }

    // ── ControlBusResult Record Shape ───────────────────────────────────────

    [Test]
    public void ControlBusResult_RecordShape()
    {
        var success = new ControlBusResult(
            Succeeded: true, ControlTopic: "eip.control-bus", FailureReason: null);

        Assert.That(success.Succeeded, Is.True);
        Assert.That(success.ControlTopic, Is.EqualTo("eip.control-bus"));
        Assert.That(success.FailureReason, Is.Null);

        var failure = new ControlBusResult(
            Succeeded: false, ControlTopic: "eip.control-bus",
            FailureReason: "Broker unavailable");

        Assert.That(failure.Succeeded, Is.False);
        Assert.That(failure.FailureReason, Is.EqualTo("Broker unavailable"));
    }

    // ── TestMessageResult Record Shape ──────────────────────────────────────

    [Test]
    public void TestMessageResult_RecordShape()
    {
        var id = Guid.NewGuid();

        var success = new TestMessageResult(
            MessageId: id, TargetTopic: "orders", Succeeded: true, FailureReason: null);

        Assert.That(success.MessageId, Is.EqualTo(id));
        Assert.That(success.TargetTopic, Is.EqualTo("orders"));
        Assert.That(success.Succeeded, Is.True);
        Assert.That(success.FailureReason, Is.Null);

        var failure = new TestMessageResult(
            MessageId: id, TargetTopic: "orders", Succeeded: false,
            FailureReason: "Publish failed");

        Assert.That(failure.Succeeded, Is.False);
        Assert.That(failure.FailureReason, Is.EqualTo("Publish failed"));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static IntegrationEnvelope<string> CreateEnvelopeWithReplyTo(
        string payload, string source, string messageType, string replyTo,
        Guid? correlationId = null) =>
        new()
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = source,
            MessageType = messageType,
            Payload = payload,
            ReplyTo = replyTo,
        };
}
