// ============================================================================
// Tutorial 23 – Request-Reply (Lab)
// ============================================================================
// EIP Pattern: Request-Reply.
// Real Integrations: RequestReplyCorrelator with NatsBrokerEndpoint
// (real NATS JetStream via Aspire) for both producer and consumer.
// Simulate reply delivery via NatsBrokerEndpoint.SendAsync.
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
    // ── 1. Request-Reply Correlation ─────────────────────────────────

    [Test]
    public async Task SendAndReceive_PublishesRequestToTopic()
    {
        await using var producer = AspireFixture.CreateNatsEndpoint("t23-pub-req");
        await using var consumer = AspireFixture.CreateNatsEndpoint("t23-con-req");
        var requestsTopic = AspireFixture.UniqueTopic("t23-requests");
        var repliesTopic = AspireFixture.UniqueTopic("t23-replies");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 500);
        var correlationId = Guid.NewGuid();
        var request = new RequestReplyMessage<string>(
            "get-price", requestsTopic, repliesTopic, "PriceSvc", "PriceRequest", correlationId);

        // Start request — will timeout since no reply, but request must be published
        var result = await correlator.SendAndReceiveAsync(request);

        producer.AssertReceivedOnTopic(requestsTopic, 1);
        var sent = producer.GetReceived<string>(0);
        Assert.That(sent.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(sent.ReplyTo, Is.EqualTo(repliesTopic));
    }

    [Test]
    public async Task SendAndReceive_ReceivesCorrelatedReply()
    {
        await using var producer = AspireFixture.CreateNatsEndpoint("t23-pub-reply");
        await using var consumer = AspireFixture.CreateNatsEndpoint("t23-con-reply");
        var requestsTopic = AspireFixture.UniqueTopic("t23-requests");
        var repliesTopic = AspireFixture.UniqueTopic("t23-replies");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 2000);
        var correlationId = Guid.NewGuid();
        var request = new RequestReplyMessage<string>(
            "get-price", requestsTopic, repliesTopic, "PriceSvc", "PriceReq", correlationId);

        // Simulate reply arrival after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            var reply = IntegrationEnvelope<string>.Create(
                "$42.00", "PriceBackend", "PriceReply", correlationId);
            await consumer.SendAsync(reply, repliesTopic);
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
        await using var producer = AspireFixture.CreateNatsEndpoint("t23-pub-to");
        await using var consumer = AspireFixture.CreateNatsEndpoint("t23-con-to");
        var requestsTopic = AspireFixture.UniqueTopic("t23-requests");
        var repliesTopic = AspireFixture.UniqueTopic("t23-replies");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 200);
        var request = new RequestReplyMessage<string>(
            "req", requestsTopic, repliesTopic, "svc", "type");

        var result = await correlator.SendAndReceiveAsync(request);

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Reply, Is.Null);
    }

    [Test]
    public async Task SendAndReceive_DurationIsTracked()
    {
        await using var producer = AspireFixture.CreateNatsEndpoint("t23-pub-dur");
        await using var consumer = AspireFixture.CreateNatsEndpoint("t23-con-dur");
        var requestsTopic = AspireFixture.UniqueTopic("t23-req");
        var repliesTopic = AspireFixture.UniqueTopic("t23-reply");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 2000);
        var correlationId = Guid.NewGuid();
        var request = new RequestReplyMessage<string>(
            "req", requestsTopic, repliesTopic, "svc", "type", correlationId);

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            var reply = IntegrationEnvelope<string>.Create(
                "ok", "backend", "reply", correlationId);
            await consumer.SendAsync(reply, repliesTopic);
        });

        var result = await correlator.SendAndReceiveAsync(request);

        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(result.TimedOut, Is.False);
    }


    // ── 3. Input Validation ──────────────────────────────────────────

    [Test]
    public async Task SendAndReceive_EmptyRequestTopic_Throws()
    {
        await using var producer = AspireFixture.CreateNatsEndpoint("t23-pub-val1");
        await using var consumer = AspireFixture.CreateNatsEndpoint("t23-con-val1");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 500);
        var request = new RequestReplyMessage<string>(
            "data", "", "replies", "svc", "type");

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await correlator.SendAndReceiveAsync(request));
    }

    [Test]
    public async Task SendAndReceive_EmptyReplyTopic_Throws()
    {
        await using var producer = AspireFixture.CreateNatsEndpoint("t23-pub-val2");
        await using var consumer = AspireFixture.CreateNatsEndpoint("t23-con-val2");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 500);
        var request = new RequestReplyMessage<string>(
            "data", "requests", "", "svc", "type");

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await correlator.SendAndReceiveAsync(request));
    }

    private static RequestReplyCorrelator<string, string> CreateCorrelator(
        NatsBrokerEndpoint producer, NatsBrokerEndpoint consumer, int timeoutMs)
    {
        var options = Options.Create(new RequestReplyOptions
        {
            TimeoutMs = timeoutMs,
            ConsumerGroup = "test-group",
        });

        return new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);
    }
}
