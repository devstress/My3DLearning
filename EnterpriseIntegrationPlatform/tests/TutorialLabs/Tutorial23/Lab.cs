// ============================================================================
// Tutorial 23 – Request-Reply (Lab)
// ============================================================================
// EIP Pattern: Request-Reply.
// E2E: Wire real RequestReplyCorrelator with MockEndpoints for both producer
// and consumer. Simulate reply delivery via MockEndpoint.SendAsync.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.RequestReply;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial23;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _producer = null!;
    private MockEndpoint _consumer = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = new MockEndpoint("req-producer");
        _consumer = new MockEndpoint("req-consumer");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _producer.DisposeAsync();
        await _consumer.DisposeAsync();
    }


    // ── 1. Request-Reply Correlation ─────────────────────────────────

    [Test]
    public async Task SendAndReceive_PublishesRequestToTopic()
    {
        var correlator = CreateCorrelator(timeoutMs: 500);
        var correlationId = Guid.NewGuid();
        var request = new RequestReplyMessage<string>(
            "get-price", "requests-topic", "replies-topic", "PriceSvc", "PriceRequest", correlationId);

        // Start request — will timeout since no reply, but request must be published
        var result = await correlator.SendAndReceiveAsync(request);

        _producer.AssertReceivedOnTopic("requests-topic", 1);
        var sent = _producer.GetReceived<string>(0);
        Assert.That(sent.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(sent.ReplyTo, Is.EqualTo("replies-topic"));
    }

    [Test]
    public async Task SendAndReceive_ReceivesCorrelatedReply()
    {
        var correlator = CreateCorrelator(timeoutMs: 2000);
        var correlationId = Guid.NewGuid();
        var request = new RequestReplyMessage<string>(
            "get-price", "requests-topic", "replies-topic", "PriceSvc", "PriceReq", correlationId);

        // Simulate reply arrival after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            var reply = IntegrationEnvelope<string>.Create(
                "$42.00", "PriceBackend", "PriceReply", correlationId);
            await _consumer.SendAsync(reply);
        });

        var result = await correlator.SendAndReceiveAsync(request);

        Assert.That(result.TimedOut, Is.False);
        Assert.That(result.Reply, Is.Not.Null);
        Assert.That(result.Reply!.Payload, Is.EqualTo("$42.00"));
        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
    }


    // ── 2. Timeout & Duration ────────────────────────────────────────

    [Test]
    public async Task SendAndReceive_TimesOut_ReturnsNullReply()
    {
        var correlator = CreateCorrelator(timeoutMs: 200);
        var request = new RequestReplyMessage<string>(
            "req", "requests-topic", "replies-topic", "svc", "type");

        var result = await correlator.SendAndReceiveAsync(request);

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Reply, Is.Null);
    }

    [Test]
    public async Task SendAndReceive_DurationIsTracked()
    {
        var correlator = CreateCorrelator(timeoutMs: 2000);
        var correlationId = Guid.NewGuid();
        var request = new RequestReplyMessage<string>(
            "req", "req-topic", "reply-topic", "svc", "type", correlationId);

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            var reply = IntegrationEnvelope<string>.Create(
                "ok", "backend", "reply", correlationId);
            await _consumer.SendAsync(reply);
        });

        var result = await correlator.SendAndReceiveAsync(request);

        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(result.TimedOut, Is.False);
    }


    // ── 3. Input Validation ──────────────────────────────────────────

    [Test]
    public async Task SendAndReceive_EmptyRequestTopic_Throws()
    {
        var correlator = CreateCorrelator(timeoutMs: 500);
        var request = new RequestReplyMessage<string>(
            "data", "", "replies", "svc", "type");

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await correlator.SendAndReceiveAsync(request));
    }

    [Test]
    public async Task SendAndReceive_EmptyReplyTopic_Throws()
    {
        var correlator = CreateCorrelator(timeoutMs: 500);
        var request = new RequestReplyMessage<string>(
            "data", "requests", "", "svc", "type");

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await correlator.SendAndReceiveAsync(request));
    }

    private RequestReplyCorrelator<string, string> CreateCorrelator(int timeoutMs)
    {
        var options = Options.Create(new RequestReplyOptions
        {
            TimeoutMs = timeoutMs,
            ConsumerGroup = "test-group",
        });

        return new RequestReplyCorrelator<string, string>(
            _producer, _consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);
    }
}
