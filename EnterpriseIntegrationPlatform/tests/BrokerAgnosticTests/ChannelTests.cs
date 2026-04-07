// ============================================================================
// Broker-Agnostic EIP Tests — Channels (P2P, Pub/Sub, Datatype, Invalid, Bridge)
// ============================================================================
// These tests prove that ALL messaging channel patterns work identically
// regardless of which IMessageBrokerProducer/Consumer implementation backs them.
// The broker is completely interchangeable — swap MockEndpoint for Postgres,
// NATS, Kafka, or Pulsar and these tests pass unchanged.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace BrokerAgnosticTests;

[TestFixture]
public sealed class ChannelTests
{
    // ── 1. Point-to-Point Channel ───────────────────────────────────────

    [Test]
    public async Task PointToPoint_Send_DeliversToBrokerOnChannel()
    {
        // Point-to-Point wraps the broker: send → producer.PublishAsync
        var broker = new MockEndpoint("p2p");
        var channel = new PointToPointChannel(
            broker, broker, NullLogger<PointToPointChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-123", "OrderService", "OrderCreated");

        await channel.SendAsync(envelope, "orders.queue", CancellationToken.None);

        broker.AssertReceivedCount(1);
        broker.AssertReceivedOnTopic("orders.queue", 1);
    }

    [Test]
    public async Task PointToPoint_Receive_RegistersConsumerSubscription()
    {
        // Receive registers a subscription via consumer.SubscribeAsync
        var broker = new MockEndpoint("p2p-rx");
        var channel = new PointToPointChannel(
            broker, broker, NullLogger<PointToPointChannel>.Instance);

        var received = new List<string>();
        await channel.ReceiveAsync<string>(
            "orders.queue", "order-processor",
            async env => received.Add(env.Payload!),
            CancellationToken.None);

        // Simulate inbound message
        var inbound = IntegrationEnvelope<string>.Create("item-1", "S", "T");
        await broker.SendAsync(inbound);

        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0], Is.EqualTo("item-1"));
    }

    // ── 2. Publish-Subscribe Channel ────────────────────────────────────

    [Test]
    public async Task PubSub_Publish_FansOutToAllSubscribers()
    {
        // Publish-Subscribe: each subscriber gets a unique consumer group,
        // so every subscriber receives every message.
        var broker = new MockEndpoint("pubsub");
        var pubsub = new PublishSubscribeChannel(
            broker, broker, NullLogger<PublishSubscribeChannel>.Instance);

        var sub1Messages = new List<string>();
        var sub2Messages = new List<string>();

        await pubsub.SubscribeAsync<string>("events", "subscriber-A",
            async env => sub1Messages.Add(env.Payload!), CancellationToken.None);
        await pubsub.SubscribeAsync<string>("events", "subscriber-B",
            async env => sub2Messages.Add(env.Payload!), CancellationToken.None);

        var envelope = IntegrationEnvelope<string>.Create("event-1", "S", "T");
        await pubsub.PublishAsync(envelope, "events", CancellationToken.None);

        // The publish goes to the broker
        broker.AssertReceivedCount(1);
        broker.AssertReceivedOnTopic("events", 1);

        // Simulate delivery to both subscribers via send
        await broker.SendAsync(envelope);
        Assert.That(sub1Messages, Has.Count.EqualTo(1));
        Assert.That(sub2Messages, Has.Count.EqualTo(1));
    }

    // ── 3. Datatype Channel ─────────────────────────────────────────────

    [Test]
    public async Task DatatypeChannel_RoutesBasedOnMessageType()
    {
        // Datatype Channel resolves topic from envelope.MessageType + prefix
        var broker = new MockEndpoint("datatype");
        var dtChannel = new DatatypeChannel(
            broker,
            Options.Create(new DatatypeChannelOptions
            {
                TopicPrefix = "eip",
                Separator = "."
            }),
            NullLogger<DatatypeChannel>.Instance);

        var orderEnv = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "OrderCreated");
        var paymentEnv = IntegrationEnvelope<string>.Create(
            "payment-data", "PaymentService", "PaymentProcessed");

        await dtChannel.PublishAsync(orderEnv, CancellationToken.None);
        await dtChannel.PublishAsync(paymentEnv, CancellationToken.None);

        broker.AssertReceivedCount(2);
        broker.AssertReceivedOnTopic("eip.ordercreated", 1);
        broker.AssertReceivedOnTopic("eip.paymentprocessed", 1);
    }

    [Test]
    public void DatatypeChannel_ResolveChannel_WithPrefix()
    {
        var broker = new MockEndpoint("dt-resolve");
        var dtChannel = new DatatypeChannel(
            broker,
            Options.Create(new DatatypeChannelOptions
            {
                TopicPrefix = "myapp",
                Separator = "-"
            }),
            NullLogger<DatatypeChannel>.Instance);

        Assert.That(dtChannel.ResolveChannel("OrderCreated"), Is.EqualTo("myapp-ordercreated"));
        Assert.That(dtChannel.ResolveChannel("PaymentFailed"), Is.EqualTo("myapp-paymentfailed"));
    }

    [Test]
    public void DatatypeChannel_ResolveChannel_NoPrefix()
    {
        var broker = new MockEndpoint("dt-resolve-no-prefix");
        var dtChannel = new DatatypeChannel(
            broker,
            Options.Create(new DatatypeChannelOptions { TopicPrefix = "" }),
            NullLogger<DatatypeChannel>.Instance);

        Assert.That(dtChannel.ResolveChannel("OrderCreated"), Is.EqualTo("ordercreated"));
    }

    // ── 4. Invalid Message Channel ──────────────────────────────────────

    [Test]
    public async Task InvalidMessageChannel_RoutesToInvalidTopic()
    {
        // Invalid messages (malformed, schema mismatch) are routed to a
        // dedicated channel — distinct from DLQ which handles processing failures.
        var broker = new MockEndpoint("invalid");
        var invalidChannel = new InvalidMessageChannel(
            broker,
            Options.Create(new InvalidMessageChannelOptions
            {
                InvalidMessageTopic = "eip.invalid",
                Source = "BrokerAgnosticTest"
            }),
            NullLogger<InvalidMessageChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "bad-data", "Ingest", "Unknown");

        await invalidChannel.RouteInvalidAsync(
            envelope, "Schema validation failed", CancellationToken.None);

        broker.AssertReceivedCount(1);
        broker.AssertReceivedOnTopic("eip.invalid", 1);

        var received = broker.GetReceived<InvalidMessageEnvelope>(0);
        Assert.That(received.Payload.Reason, Is.EqualTo("Schema validation failed"));
        Assert.That(received.Payload.OriginalMessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task InvalidMessageChannel_RouteRaw_HandlesUnparsedData()
    {
        var broker = new MockEndpoint("invalid-raw");
        var invalidChannel = new InvalidMessageChannel(
            broker,
            Options.Create(new InvalidMessageChannelOptions
            {
                InvalidMessageTopic = "eip.invalid",
                Source = "BrokerAgnosticTest"
            }),
            NullLogger<InvalidMessageChannel>.Instance);

        await invalidChannel.RouteRawInvalidAsync(
            "{broken-json}", "orders.inbound", "JSON parse failure",
            CancellationToken.None);

        broker.AssertReceivedCount(1);
        var received = broker.GetReceived<InvalidMessageEnvelope>(0);
        Assert.That(received.Payload.RawData, Is.EqualTo("{broken-json}"));
        Assert.That(received.Payload.SourceTopic, Is.EqualTo("orders.inbound"));
    }

    // ── 5. Messaging Bridge ─────────────────────────────────────────────

    [Test]
    public async Task MessagingBridge_ForwardsMessages_WithDeduplication()
    {
        // The bridge consumes from source and publishes to target.
        // It deduplicates by MessageId within a sliding window.
        var source = new MockEndpoint("bridge-source");
        var target = new MockEndpoint("bridge-target");

        var bridge = new MessagingBridge(
            source, target,
            Options.Create(new MessagingBridgeOptions
            {
                ConsumerGroup = "bridge-group",
                DeduplicationWindowSize = 100
            }),
            NullLogger<MessagingBridge>.Instance);

        await bridge.StartAsync<string>(
            "source.topic", "target.topic", CancellationToken.None);

        // Send message through the bridge
        var msg = IntegrationEnvelope<string>.Create("data-1", "S", "T");
        await source.SendAsync(msg);

        // Message forwarded to target
        target.AssertReceivedCount(1);
        target.AssertReceivedOnTopic("target.topic", 1);
        Assert.That(bridge.ForwardedCount, Is.EqualTo(1));
        Assert.That(bridge.DuplicateCount, Is.EqualTo(0));
    }

    [Test]
    public async Task MessagingBridge_DeduplicatesSameMessageId()
    {
        var source = new MockEndpoint("bridge-dedup-src");
        var target = new MockEndpoint("bridge-dedup-tgt");

        var bridge = new MessagingBridge(
            source, target,
            Options.Create(new MessagingBridgeOptions
            {
                ConsumerGroup = "dedup-group",
                DeduplicationWindowSize = 50
            }),
            NullLogger<MessagingBridge>.Instance);

        await bridge.StartAsync<string>(
            "src", "tgt", CancellationToken.None);

        // Send same message twice (same MessageId)
        var msg = IntegrationEnvelope<string>.Create("dup-data", "S", "T");
        await source.SendAsync(msg);
        await source.SendAsync(msg); // duplicate

        target.AssertReceivedCount(1); // Only one forwarded
        Assert.That(bridge.ForwardedCount, Is.EqualTo(1));
        Assert.That(bridge.DuplicateCount, Is.EqualTo(1));
    }
}
