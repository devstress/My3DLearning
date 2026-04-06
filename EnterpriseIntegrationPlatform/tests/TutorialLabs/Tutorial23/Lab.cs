// ============================================================================
// Tutorial 23 – Request-Reply (Lab)
// ============================================================================
// This lab exercises the RequestReplyCorrelator using mocked broker
// interfaces.  You will verify request publishing with ReplyTo, reply
// correlation, timeout behaviour, and option defaults.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.RequestReply;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial23;

[TestFixture]
public sealed class Lab
{
    // ── Options Default Values ──────────────────────────────────────────────

    [Test]
    public void Options_DefaultValues_AreCorrect()
    {
        var opts = new RequestReplyOptions();

        Assert.That(opts.TimeoutMs, Is.EqualTo(30_000));
        Assert.That(opts.ConsumerGroup, Is.EqualTo("request-reply"));
    }

    // ── RequestReplyMessage Construction ─────────────────────────────────────

    [Test]
    public void Message_RecordProperties_AreCorrect()
    {
        var correlationId = Guid.NewGuid();
        var msg = new RequestReplyMessage<string>(
            "payload", "req-topic", "reply-topic", "TestSvc", "cmd.ping", correlationId);

        Assert.That(msg.Payload, Is.EqualTo("payload"));
        Assert.That(msg.RequestTopic, Is.EqualTo("req-topic"));
        Assert.That(msg.ReplyTopic, Is.EqualTo("reply-topic"));
        Assert.That(msg.Source, Is.EqualTo("TestSvc"));
        Assert.That(msg.MessageType, Is.EqualTo("cmd.ping"));
        Assert.That(msg.CorrelationId, Is.EqualTo(correlationId));
    }

    // ── Correlator Publishes Request With ReplyTo ────────────────────────────

    [Test]
    public async Task Correlator_PublishesRequest_WithReplyToSet()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        var options = Options.Create(new RequestReplyOptions { TimeoutMs = 500 });

        var correlator = new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);

        var msg = new RequestReplyMessage<string>(
            "ping", "commands", "replies", "TestSvc", "cmd.ping");

        // Will time out since no reply is submitted, but request should be published.
        await correlator.SendAndReceiveAsync(msg);

        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.ReplyTo == "replies"),
            "commands",
            Arg.Any<CancellationToken>());
    }

    // ── Correlator Sets Intent To Command ────────────────────────────────────

    [Test]
    public async Task Correlator_SetsIntentToCommand()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        var options = Options.Create(new RequestReplyOptions { TimeoutMs = 500 });

        var correlator = new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);

        var msg = new RequestReplyMessage<string>(
            "data", "req", "rep", "Svc", "cmd.do");

        await correlator.SendAndReceiveAsync(msg);

        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Intent == MessageIntent.Command),
            "req",
            Arg.Any<CancellationToken>());
    }

    // ── Timeout Returns TimedOut Result ──────────────────────────────────────

    [Test]
    public async Task Correlator_Timeout_ReturnsTimedOutResult()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        var options = Options.Create(new RequestReplyOptions { TimeoutMs = 300 });

        var correlator = new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);

        var msg = new RequestReplyMessage<string>(
            "request-data", "cmd-topic", "reply-topic", "Svc", "cmd.type");

        var result = await correlator.SendAndReceiveAsync(msg);

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Reply, Is.Null);
        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
    }

    // ── RequestReplyResult Record ────────────────────────────────────────────

    [Test]
    public void ResultRecord_Properties_AreCorrectlySet()
    {
        var correlationId = Guid.NewGuid();
        var reply = IntegrationEnvelope<string>.Create(
            "pong", "ReplySvc", "reply.type", correlationId: correlationId);

        var result = new RequestReplyResult<string>(
            correlationId, reply, false, TimeSpan.FromMilliseconds(42));

        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(result.Reply, Is.Not.Null);
        Assert.That(result.Reply!.Payload, Is.EqualTo("pong"));
        Assert.That(result.TimedOut, Is.False);
        Assert.That(result.Duration.TotalMilliseconds, Is.EqualTo(42));
    }

    // ── Empty RequestTopic Throws ────────────────────────────────────────────

    [Test]
    public void Correlator_EmptyRequestTopic_ThrowsArgumentException()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        var options = Options.Create(new RequestReplyOptions { TimeoutMs = 500 });

        var correlator = new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);

        var msg = new RequestReplyMessage<string>(
            "data", "", "reply-topic", "Svc", "type");

        Assert.ThrowsAsync<ArgumentException>(
            () => correlator.SendAndReceiveAsync(msg));
    }
}
