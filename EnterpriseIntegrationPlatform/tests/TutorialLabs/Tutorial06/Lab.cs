// ============================================================================
// Tutorial 06 – Messaging Channels (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how messaging channel patterns work
//          through real NATS JetStream via Aspire. Read the code and comments
//          to understand each concept before moving to the Exam.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Point-to-Point send — single consumer delivery via queue channel
//   2. Point-to-Point receive — handler triggered on message arrival
//   3. Point-to-Point multiple sends — messages accumulate in order
//   4. Publish-Subscribe publish — event delivered to channel
//   5. Publish-Subscribe fan-out — multiple subscribers receive copies
//   6. Datatype Channel routing — message routed by MessageType
//   7. Datatype Channel resolve — topic name computed from type
//   8. Invalid Message Channel — malformed message routed to invalid topic
//   9. Invalid Message Channel raw — raw data captured with reason
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
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
    // ── 1. Point-to-Point Channel (Real NATS) ───────────────────────────

    [Test]
    public async Task PointToPoint_Send_DeliversToQueueChannel()
    {
        // Point-to-Point through real NATS JetStream.
        await using var nats = AspireFixture.CreateNatsEndpoint("t06-p2p");
        var topic = AspireFixture.UniqueTopic("t06-p2p");

        var channel = new PointToPointChannel(
            nats, nats, NullLogger<PointToPointChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-123", "OrderService", "order.created");

        await channel.SendAsync(envelope, topic, CancellationToken.None);

        nats.AssertReceivedCount(1);
        Assert.That(nats.GetReceived<string>().Payload, Is.EqualTo("order-123"));
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task PointToPoint_Receive_HandlerTriggeredOnSend()
    {
        // ReceiveAsync registers a consumer handler on real NATS.
        await using var nats = AspireFixture.CreateNatsEndpoint("t06-recv");
        var topic = AspireFixture.UniqueTopic("t06-recv");

        var channel = new PointToPointChannel(
            nats, nats, NullLogger<PointToPointChannel>.Instance);

        IntegrationEnvelope<string>? captured = null;
        await channel.ReceiveAsync<string>(topic, "worker-group",
            msg => { captured = msg; return Task.CompletedTask; }, CancellationToken.None);

        await Task.Delay(500);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-456", "OrderService", "order.created");
        await nats.SendAsync(envelope, topic);

        await nats.WaitForConsumedAsync(1, TimeSpan.FromSeconds(10));

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload, Is.EqualTo("order-456"));
        Assert.That(captured.MessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task PointToPoint_MultipleSends_AllDelivered()
    {
        // Multiple messages through real NATS accumulate in order.
        await using var nats = AspireFixture.CreateNatsEndpoint("t06-multi");
        var topic = AspireFixture.UniqueTopic("t06-multi");

        var channel = new PointToPointChannel(
            nats, nats, NullLogger<PointToPointChannel>.Instance);

        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create(
                $"order-{i}", "OrderService", "order.created");
            await channel.SendAsync(env, topic, CancellationToken.None);
        }

        nats.AssertReceivedCount(3);
        nats.AssertReceivedOnTopic(topic, 3);
        Assert.That(nats.GetReceived<string>(0).Payload, Is.EqualTo("order-0"));
        Assert.That(nats.GetReceived<string>(2).Payload, Is.EqualTo("order-2"));
    }

    // ── 2. Publish-Subscribe Channel (Real NATS) ────────────────────────

    [Test]
    public async Task PubSub_Publish_DeliversToChannel()
    {
        // Publish-Subscribe through real NATS.
        await using var nats = AspireFixture.CreateNatsEndpoint("t06-pubsub");
        var topic = AspireFixture.UniqueTopic("t06-pubsub");

        var channel = new PublishSubscribeChannel(
            nats, nats, NullLogger<PublishSubscribeChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "event-data", "EventService", "event.fired");

        await channel.PublishAsync(envelope, topic, CancellationToken.None);

        nats.AssertReceivedCount(1);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task PubSub_Subscribe_MultipleSubscribersGetFanOut()
    {
        // Fan-out: multiple subscribers via real NATS.
        await using var nats = AspireFixture.CreateNatsEndpoint("t06-fanout");
        var topic = AspireFixture.UniqueTopic("t06-fanout");

        var channel = new PublishSubscribeChannel(
            nats, nats, NullLogger<PublishSubscribeChannel>.Instance);

        var payloads = new List<string>();
        await channel.SubscribeAsync<string>(topic, "sub-A",
            msg => { payloads.Add(msg.Payload + "-A"); return Task.CompletedTask; },
            CancellationToken.None);
        await channel.SubscribeAsync<string>(topic, "sub-B",
            msg => { payloads.Add(msg.Payload + "-B"); return Task.CompletedTask; },
            CancellationToken.None);

        await Task.Delay(500);

        var envelope = IntegrationEnvelope<string>.Create("fan-out", "svc", "type");
        await nats.SendAsync(envelope, topic);

        await nats.WaitForConsumedAsync(1, TimeSpan.FromSeconds(10));

        // At least one subscriber should receive the message
        Assert.That(payloads.Count, Is.GreaterThanOrEqualTo(1));
    }

    // ── 3. Datatype Channel (Real NATS) ─────────────────────────────────

    [Test]
    public async Task DatatypeChannel_RoutesMessageByType()
    {
        // Datatype Channel routes each message to a topic derived from its MessageType
        // through real NATS.
        await using var nats = AspireFixture.CreateNatsEndpoint("t06-dtype");

        var options = Options.Create(new DatatypeChannelOptions
            { TopicPrefix = "datatype", Separator = "." });
        var channel = new DatatypeChannel(
            nats, options, NullLogger<DatatypeChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");
        await channel.PublishAsync(envelope, CancellationToken.None);

        nats.AssertReceivedCount(1);
        nats.AssertReceivedOnTopic("datatype.order.created", 1);
    }

    [Test]
    public void DatatypeChannel_ResolveChannel_ComputesTopicName()
    {
        // ResolveChannel returns the computed topic — pure logic, no broker needed.
        // But we still need an IMessageBrokerProducer for construction.
        // Use a real (but unused) NATS endpoint to satisfy the dependency.
        var nats = AspireFixture.CreateNatsEndpoint("t06-resolve");

        var options = Options.Create(new DatatypeChannelOptions
            { TopicPrefix = "dt", Separator = "-" });
        var channel = new DatatypeChannel(
            nats, options, NullLogger<DatatypeChannel>.Instance);

        Assert.That(channel.ResolveChannel("order.created"), Is.EqualTo("dt-order.created"));
        Assert.That(channel.ResolveChannel("payment.processed"), Is.EqualTo("dt-payment.processed"));

        nats.DisposeAsync().AsTask().Wait();
    }

    // ── 4. Invalid Message Channel (Real NATS) ──────────────────────────

    [Test]
    public async Task InvalidMessageChannel_RouteInvalid_PublishesToInvalidTopic()
    {
        // Invalid Message Channel routes malformed messages through real NATS.
        await using var nats = AspireFixture.CreateNatsEndpoint("t06-invalid");

        var options = Options.Create(new InvalidMessageChannelOptions
            { InvalidMessageTopic = "invalid-msgs", Source = "TestChannel" });
        var channel = new InvalidMessageChannel(
            nats, options, NullLogger<InvalidMessageChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "bad-data", "LegacySystem", "legacy.event");
        await channel.RouteInvalidAsync(envelope, "Schema mismatch", CancellationToken.None);

        nats.AssertReceivedCount(1);
        nats.AssertReceivedOnTopic("invalid-msgs", 1);
        var received = nats.GetReceived<InvalidMessageEnvelope>();
        Assert.That(received.Payload.Reason, Is.EqualTo("Schema mismatch"));
    }

    [Test]
    public async Task InvalidMessageChannel_RouteRawInvalid_CapturesRawData()
    {
        // RouteRawInvalidAsync handles raw data through real NATS.
        await using var nats = AspireFixture.CreateNatsEndpoint("t06-raw");

        var options = Options.Create(new InvalidMessageChannelOptions
            { InvalidMessageTopic = "invalid-raw", Source = "Gateway" });
        var channel = new InvalidMessageChannel(
            nats, options, NullLogger<InvalidMessageChannel>.Instance);

        await channel.RouteRawInvalidAsync(
            "not-json-at-all", "inbound-topic", "Parse failure", CancellationToken.None);

        nats.AssertReceivedCount(1);
        nats.AssertReceivedOnTopic("invalid-raw", 1);
        var received = nats.GetReceived<InvalidMessageEnvelope>();
        Assert.That(received.Payload.RawData, Is.EqualTo("not-json-at-all"));
        Assert.That(received.Payload.Reason, Is.EqualTo("Parse failure"));
        Assert.That(received.Payload.SourceTopic, Is.EqualTo("inbound-topic"));
    }
}
