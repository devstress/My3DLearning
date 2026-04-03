using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.RequestReply;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RequestReplyCorrelatorTests
{
    private IMessageBrokerProducer _producer = null!;
    private IMessageBrokerConsumer _consumer = null!;
    private ILogger<RequestReplyCorrelator<string, string>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _consumer = Substitute.For<IMessageBrokerConsumer>();
        _logger = Substitute.For<ILogger<RequestReplyCorrelator<string, string>>>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _consumer.DisposeAsync();
    }

    private RequestReplyCorrelator<string, string> BuildCorrelator(int timeoutMs = 30_000) =>
        new(
            _producer,
            _consumer,
            Options.Create(new RequestReplyOptions { TimeoutMs = timeoutMs, ConsumerGroup = "test-rr" }),
            _logger);

    private static RequestReplyMessage<string> BuildRequest(
        string payload = "request-payload",
        string requestTopic = "orders.request",
        string replyTopic = "orders.reply",
        Guid? correlationId = null) =>
        new(payload, requestTopic, replyTopic, "test-svc", "OrderRequest", correlationId);

    // ── Request publishing ────────────────────────────────────────────────────

    [Test]
    public async Task SendAndReceiveAsync_PublishesRequestWithReplyToSet()
    {
        var correlationId = Guid.NewGuid();
        var sut = BuildCorrelator(timeoutMs: 200);

        // The consumer subscribe will simulate a reply
        _consumer.When(x => x.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>()))
            .Do(async ci =>
            {
                var handler = ci.ArgAt<Func<IntegrationEnvelope<string>, Task>>(2);
                var reply = IntegrationEnvelope<string>.Create(
                    "reply-payload", "responder", "OrderResponse",
                    correlationId: correlationId);
                await handler(reply);
            });

        var request = BuildRequest(correlationId: correlationId);
        await sut.SendAndReceiveAsync(request);

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e =>
                e.ReplyTo == "orders.reply" &&
                e.CorrelationId == correlationId),
            "orders.request",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendAndReceiveAsync_SetsIntentToCommand()
    {
        var correlationId = Guid.NewGuid();
        var sut = BuildCorrelator(timeoutMs: 200);

        _consumer.When(x => x.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>()))
            .Do(async ci =>
            {
                var handler = ci.ArgAt<Func<IntegrationEnvelope<string>, Task>>(2);
                var reply = IntegrationEnvelope<string>.Create(
                    "reply", "responder", "Response", correlationId: correlationId);
                await handler(reply);
            });

        var request = BuildRequest(correlationId: correlationId);
        await sut.SendAndReceiveAsync(request);

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Intent == MessageIntent.Command),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Successful reply ──────────────────────────────────────────────────────

    [Test]
    public async Task SendAndReceiveAsync_ReturnsReply_WhenCorrelatedResponseReceived()
    {
        var correlationId = Guid.NewGuid();
        var sut = BuildCorrelator(timeoutMs: 5000);

        _consumer.When(x => x.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>()))
            .Do(async ci =>
            {
                var handler = ci.ArgAt<Func<IntegrationEnvelope<string>, Task>>(2);
                var reply = IntegrationEnvelope<string>.Create(
                    "reply-data", "responder", "OrderResponse",
                    correlationId: correlationId);
                await handler(reply);
            });

        var request = BuildRequest(correlationId: correlationId);
        var result = await sut.SendAndReceiveAsync(request);

        Assert.That(result.TimedOut, Is.False);
        Assert.That(result.Reply, Is.Not.Null);
        Assert.That(result.Reply!.Payload, Is.EqualTo("reply-data"));
        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task SendAndReceiveAsync_ReturnsDuration_OnSuccess()
    {
        var correlationId = Guid.NewGuid();
        var sut = BuildCorrelator(timeoutMs: 5000);

        _consumer.When(x => x.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>()))
            .Do(async ci =>
            {
                var handler = ci.ArgAt<Func<IntegrationEnvelope<string>, Task>>(2);
                var reply = IntegrationEnvelope<string>.Create(
                    "reply", "responder", "Response", correlationId: correlationId);
                await handler(reply);
            });

        var request = BuildRequest(correlationId: correlationId);
        var result = await sut.SendAndReceiveAsync(request);

        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
    }

    // ── Timeout ───────────────────────────────────────────────────────────────

    [Test]
    public async Task SendAndReceiveAsync_ReturnsTimedOut_WhenNoReplyWithinTimeout()
    {
        var sut = BuildCorrelator(timeoutMs: 100);

        // Consumer subscribes but never invokes the handler (no reply)
        _consumer.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var request = BuildRequest();
        var result = await sut.SendAndReceiveAsync(request);

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Reply, Is.Null);
    }

    [Test]
    public async Task SendAndReceiveAsync_ReturnsDuration_OnTimeout()
    {
        var sut = BuildCorrelator(timeoutMs: 100);

        _consumer.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var request = BuildRequest();
        var result = await sut.SendAndReceiveAsync(request);

        Assert.That(result.Duration.TotalMilliseconds, Is.GreaterThanOrEqualTo(80));
    }

    // ── Correlation mismatch ──────────────────────────────────────────────────

    [Test]
    public async Task SendAndReceiveAsync_IgnoresMismatchedCorrelationId()
    {
        var requestCorrelationId = Guid.NewGuid();
        var wrongCorrelationId = Guid.NewGuid();
        var sut = BuildCorrelator(timeoutMs: 200);

        _consumer.When(x => x.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>()))
            .Do(async ci =>
            {
                var handler = ci.ArgAt<Func<IntegrationEnvelope<string>, Task>>(2);
                // Send a reply with wrong correlation ID
                var wrongReply = IntegrationEnvelope<string>.Create(
                    "wrong-reply", "responder", "Response",
                    correlationId: wrongCorrelationId);
                await handler(wrongReply);
            });

        var request = BuildRequest(correlationId: requestCorrelationId);
        var result = await sut.SendAndReceiveAsync(request);

        // Should time out because the reply doesn't match
        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Reply, Is.Null);
    }

    // ── Auto-generated CorrelationId ──────────────────────────────────────────

    [Test]
    public async Task SendAndReceiveAsync_GeneratesCorrelationId_WhenNotProvided()
    {
        var sut = BuildCorrelator(timeoutMs: 100);

        _consumer.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var request = BuildRequest(correlationId: null);
        var result = await sut.SendAndReceiveAsync(request);

        Assert.That(result.CorrelationId, Is.Not.EqualTo(Guid.Empty));
    }

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Test]
    public void SendAndReceiveAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        var sut = BuildCorrelator();

        Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.SendAndReceiveAsync(null!));
    }

    [Test]
    public void SendAndReceiveAsync_ThrowsArgumentException_WhenRequestTopicIsEmpty()
    {
        var sut = BuildCorrelator();
        var request = new RequestReplyMessage<string>("payload", "", "reply-topic", "svc", "Type");

        Assert.ThrowsAsync<ArgumentException>(
            () => sut.SendAndReceiveAsync(request));
    }

    [Test]
    public void SendAndReceiveAsync_ThrowsArgumentException_WhenReplyTopicIsEmpty()
    {
        var sut = BuildCorrelator();
        var request = new RequestReplyMessage<string>("payload", "request-topic", "", "svc", "Type");

        Assert.ThrowsAsync<ArgumentException>(
            () => sut.SendAndReceiveAsync(request));
    }

    // ── Subscribes to reply topic ─────────────────────────────────────────────

    [Test]
    public async Task SendAndReceiveAsync_SubscribesToReplyTopic()
    {
        var sut = BuildCorrelator(timeoutMs: 100);

        _consumer.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var request = BuildRequest(replyTopic: "my-replies");
        await sut.SendAndReceiveAsync(request);

        await _consumer.Received(1).SubscribeAsync<string>(
            "my-replies",
            "test-rr",
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());
    }
}
