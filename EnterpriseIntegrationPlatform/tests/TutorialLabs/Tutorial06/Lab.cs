// ============================================================================
// Tutorial 06 – Messaging Channels (Lab)
// ============================================================================
// EIP Patterns: Point-to-Point Channel, Publish-Subscribe Channel,
//               Datatype Channel, Invalid Message Channel
// End-to-End: Wire real channel classes with MockEndpoints — send through
// each channel type and verify delivery semantics: queue (P2P), fan-out
// (Pub/Sub), type-based routing (Datatype), and error routing (Invalid).
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial06;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _endpoint = null!;

    [SetUp]
    public void SetUp() => _endpoint = new MockEndpoint("lab06");

    [TearDown]
    public async Task TearDown() => await _endpoint.DisposeAsync();

    // ── 1. Point-to-Point Channel ───────────────────────────────────────

    [Test]
    public async Task PointToPoint_Send_DeliversToQueueChannel()
    {
        // Point-to-Point: each message delivered to exactly one consumer
        // in the group — queue semantics for command processing.
        var channel = new PointToPointChannel(
            _endpoint, _endpoint, NullLogger<PointToPointChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-123", "OrderService", "order.created");

        await channel.SendAsync(envelope, "orders-queue", CancellationToken.None);

        _endpoint.AssertReceivedCount(1);
        Assert.That(_endpoint.GetReceived<string>().Payload, Is.EqualTo("order-123"));
        _endpoint.AssertReceivedOnTopic("orders-queue", 1);
    }

    [Test]
    public async Task PointToPoint_Receive_HandlerTriggeredOnSend()
    {
        // ReceiveAsync registers a consumer handler on the channel.
        // When a message arrives via SendAsync, the handler is invoked.
        var channel = new PointToPointChannel(
            _endpoint, _endpoint, NullLogger<PointToPointChannel>.Instance);

        IntegrationEnvelope<string>? captured = null;
        await channel.ReceiveAsync<string>("orders-queue", "worker-group",
            msg => { captured = msg; return Task.CompletedTask; }, CancellationToken.None);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-456", "OrderService", "order.created");
        await _endpoint.SendAsync(envelope);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload, Is.EqualTo("order-456"));
        Assert.That(captured.MessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task PointToPoint_MultipleSends_AllDelivered()
    {
        // Multiple messages sent through the same channel accumulate
        // in order — FIFO delivery within a single channel.
        var channel = new PointToPointChannel(
            _endpoint, _endpoint, NullLogger<PointToPointChannel>.Instance);

        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create(
                $"order-{i}", "OrderService", "order.created");
            await channel.SendAsync(env, "orders", CancellationToken.None);
        }

        _endpoint.AssertReceivedCount(3);
        _endpoint.AssertReceivedOnTopic("orders", 3);
        Assert.That(_endpoint.GetReceived<string>(0).Payload, Is.EqualTo("order-0"));
        Assert.That(_endpoint.GetReceived<string>(2).Payload, Is.EqualTo("order-2"));
    }

    // ── 2. Publish-Subscribe Channel ────────────────────────────────────

    [Test]
    public async Task PubSub_Publish_DeliversToChannel()
    {
        // Publish-Subscribe: every subscriber receives every message.
        // The channel creates unique consumer groups per subscriber ID.
        var channel = new PublishSubscribeChannel(
            _endpoint, _endpoint, NullLogger<PublishSubscribeChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "event-data", "EventService", "event.fired");

        await channel.PublishAsync(envelope, "events-topic", CancellationToken.None);

        _endpoint.AssertReceivedCount(1);
        _endpoint.AssertReceivedOnTopic("events-topic", 1);
    }

    [Test]
    public async Task PubSub_Subscribe_MultipleSubscribersGetFanOut()
    {
        // Each subscriberId gets a unique consumer group, ensuring
        // fan-out: all subscribers receive the same message independently.
        var channel = new PublishSubscribeChannel(
            _endpoint, _endpoint, NullLogger<PublishSubscribeChannel>.Instance);

        var payloads = new List<string>();
        await channel.SubscribeAsync<string>("events", "sub-A",
            msg => { payloads.Add(msg.Payload + "-A"); return Task.CompletedTask; },
            CancellationToken.None);
        await channel.SubscribeAsync<string>("events", "sub-B",
            msg => { payloads.Add(msg.Payload + "-B"); return Task.CompletedTask; },
            CancellationToken.None);

        var envelope = IntegrationEnvelope<string>.Create("fan-out", "svc", "type");
        await _endpoint.SendAsync(envelope);

        Assert.That(payloads, Has.Count.EqualTo(2));
        Assert.That(payloads, Does.Contain("fan-out-A"));
        Assert.That(payloads, Does.Contain("fan-out-B"));
    }

    // ── 3. Datatype Channel ─────────────────────────────────────────────

    [Test]
    public async Task DatatypeChannel_RoutesMessageByType()
    {
        // Datatype Channel routes each message to a topic derived from its
        // MessageType: {prefix}{separator}{messageType.toLower()}.
        // This separates different message types onto dedicated channels.
        var options = Options.Create(new DatatypeChannelOptions
            { TopicPrefix = "datatype", Separator = "." });
        var channel = new DatatypeChannel(
            _endpoint, options, NullLogger<DatatypeChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");
        await channel.PublishAsync(envelope, CancellationToken.None);

        _endpoint.AssertReceivedCount(1);
        _endpoint.AssertReceivedOnTopic("datatype.order.created", 1);
    }

    [Test]
    public void DatatypeChannel_ResolveChannel_ComputesTopicName()
    {
        // ResolveChannel returns the computed topic for a given MessageType
        // without publishing — useful for route planning and diagnostics.
        var options = Options.Create(new DatatypeChannelOptions
            { TopicPrefix = "dt", Separator = "-" });
        var channel = new DatatypeChannel(
            _endpoint, options, NullLogger<DatatypeChannel>.Instance);

        Assert.That(channel.ResolveChannel("order.created"), Is.EqualTo("dt-order.created"));
        Assert.That(channel.ResolveChannel("payment.processed"), Is.EqualTo("dt-payment.processed"));
    }

    // ── 4. Invalid Message Channel ──────────────────────────────────────

    [Test]
    public async Task InvalidMessageChannel_RouteInvalid_PublishesToInvalidTopic()
    {
        // Invalid Message Channel routes malformed or schema-violating
        // messages to a dedicated topic for investigation and replay.
        var options = Options.Create(new InvalidMessageChannelOptions
            { InvalidMessageTopic = "invalid-msgs", Source = "TestChannel" });
        var channel = new InvalidMessageChannel(
            _endpoint, options, NullLogger<InvalidMessageChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "bad-data", "LegacySystem", "legacy.event");
        await channel.RouteInvalidAsync(envelope, "Schema mismatch", CancellationToken.None);

        _endpoint.AssertReceivedCount(1);
        _endpoint.AssertReceivedOnTopic("invalid-msgs", 1);
        var received = _endpoint.GetReceived<InvalidMessageEnvelope>();
        Assert.That(received.Payload.Reason, Is.EqualTo("Schema mismatch"));
    }

    [Test]
    public async Task InvalidMessageChannel_RouteRawInvalid_CapturesRawData()
    {
        // RouteRawInvalidAsync handles messages that couldn't even be
        // deserialized — the raw string is preserved for debugging.
        var options = Options.Create(new InvalidMessageChannelOptions
            { InvalidMessageTopic = "invalid-raw", Source = "Gateway" });
        var channel = new InvalidMessageChannel(
            _endpoint, options, NullLogger<InvalidMessageChannel>.Instance);

        await channel.RouteRawInvalidAsync(
            "not-json-at-all", "inbound-topic", "Parse failure", CancellationToken.None);

        _endpoint.AssertReceivedCount(1);
        _endpoint.AssertReceivedOnTopic("invalid-raw", 1);
        var received = _endpoint.GetReceived<InvalidMessageEnvelope>();
        Assert.That(received.Payload.RawData, Is.EqualTo("not-json-at-all"));
        Assert.That(received.Payload.Reason, Is.EqualTo("Parse failure"));
        Assert.That(received.Payload.SourceTopic, Is.EqualTo("inbound-topic"));
    }
}
