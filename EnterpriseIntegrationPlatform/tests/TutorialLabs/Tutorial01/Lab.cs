// ============================================================================
// Tutorial 01 – Introduction (Lab)
// ============================================================================
// EIP Patterns: Point-to-Point Channel, Publish-Subscribe Channel
// End-to-End: Wire real channels with MockEndpoint, send and receive
// messages through actual PointToPointChannel and PublishSubscribeChannel
// components — real integration, no stubs.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial01;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _broker = null!;

    [SetUp]
    public void SetUp()
    {
        _broker = new MockEndpoint("broker");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _broker.DisposeAsync();
    }

    [Test]
    public async Task PointToPoint_SendAndReceive_MessageFlowsThroughChannel()
    {
        // Wire a real PointToPointChannel with MockEndpoint as broker
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        // Subscribe a handler that captures messages coming out of the channel
        IntegrationEnvelope<string>? received = null;
        await channel.ReceiveAsync<string>("orders-queue", "order-processor",
            msg => { received = msg; return Task.CompletedTask; },
            CancellationToken.None);

        // Send a command through the real channel
        var order = IntegrationEnvelope<string>.Create(
            "PlaceOrder:ORD-001", "WebApp", "order.place") with
        {
            Intent = MessageIntent.Command,
        };
        await channel.SendAsync(order, "orders-queue", CancellationToken.None);

        // The channel published to MockEndpoint — verify it arrived
        _broker.AssertReceivedOnTopic("orders-queue", 1);

        // The handler was invoked via the subscribe path
        await _broker.SendAsync(order);
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Payload, Is.EqualTo("PlaceOrder:ORD-001"));
    }

    [Test]
    public async Task PubSub_MultipleSubscribers_AllReceiveFanOut()
    {
        // Wire a real PublishSubscribeChannel
        var channel = new PublishSubscribeChannel(
            _broker, _broker, NullLogger<PublishSubscribeChannel>.Instance);

        // Two independent subscribers on the same topic
        IntegrationEnvelope<string>? subscriber1Msg = null;
        IntegrationEnvelope<string>? subscriber2Msg = null;

        await channel.SubscribeAsync<string>("events-topic", "audit-service",
            msg => { subscriber1Msg = msg; return Task.CompletedTask; },
            CancellationToken.None);
        await channel.SubscribeAsync<string>("events-topic", "notification-service",
            msg => { subscriber2Msg = msg; return Task.CompletedTask; },
            CancellationToken.None);

        // Publish an event through the real channel
        var evt = IntegrationEnvelope<string>.Create(
            "OrderShipped", "ShippingService", "order.shipped") with
        {
            Intent = MessageIntent.Event,
        };
        await channel.PublishAsync(evt, "events-topic", CancellationToken.None);

        // MockEndpoint captured the published message
        _broker.AssertReceivedOnTopic("events-topic", 1);

        // Deliver to all subscribers via the broker
        await _broker.SendAsync(evt);
        Assert.That(subscriber1Msg, Is.Not.Null);
        Assert.That(subscriber2Msg, Is.Not.Null);
        Assert.That(subscriber1Msg!.MessageId, Is.EqualTo(subscriber2Msg!.MessageId));
    }

    [Test]
    public async Task PointToPoint_MultipleMessages_AllDeliveredInSequence()
    {
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        // Send a batch of 5 messages through the real channel
        for (var i = 0; i < 5; i++)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"item-{i}", "BatchProducer", "batch.item");
            await channel.SendAsync(envelope, "batch-queue", CancellationToken.None);
        }

        // All 5 messages flowed through the real channel and arrived at the broker
        _broker.AssertReceivedCount(5);
        Assert.That(_broker.GetReceived<string>(0).Payload, Is.EqualTo("item-0"));
        Assert.That(_broker.GetReceived<string>(4).Payload, Is.EqualTo("item-4"));
    }

    [Test]
    public async Task PointToPoint_DomainObject_FlowsThroughChannel()
    {
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        var order = new OrderPayload("ORD-500", "Laptop", 2, 1299.99m);
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            order, "CatalogService", "order.created") with
        {
            Intent = MessageIntent.Document,
            Priority = MessagePriority.High,
        };

        await channel.SendAsync(envelope, "high-priority-orders", CancellationToken.None);

        _broker.AssertReceivedOnTopic("high-priority-orders", 1);
        var received = _broker.GetReceived<OrderPayload>();
        Assert.That(received.Payload.OrderId, Is.EqualTo("ORD-500"));
        Assert.That(received.Payload.Price, Is.EqualTo(1299.99m));
        Assert.That(received.Priority, Is.EqualTo(MessagePriority.High));
    }

    [Test]
    public async Task ChannelHop_P2PToHandler_ThenPubSubFanOut()
    {
        // Two real channels: P2P for input, PubSub for fanout
        var inputBroker = new MockEndpoint("input-broker");
        var fanoutBroker = new MockEndpoint("fanout-broker");

        var inputChannel = new PointToPointChannel(
            inputBroker, inputBroker, NullLogger<PointToPointChannel>.Instance);
        var fanoutChannel = new PublishSubscribeChannel(
            fanoutBroker, fanoutBroker, NullLogger<PublishSubscribeChannel>.Instance);

        // Handler: receives from P2P, enriches, and publishes to PubSub
        await inputChannel.ReceiveAsync<string>("ingest-queue", "enricher",
            async msg =>
            {
                var enriched = msg with
                {
                    Metadata = new Dictionary<string, string> { ["enriched"] = "true" },
                };
                await fanoutChannel.PublishAsync(enriched, "enriched-events", CancellationToken.None);
            }, CancellationToken.None);

        // Send a raw message into the P2P channel
        var raw = IntegrationEnvelope<string>.Create(
            "raw-event-data", "SensorService", "sensor.reading");
        await inputChannel.SendAsync(raw, "ingest-queue", CancellationToken.None);

        // P2P channel delivered to input broker
        inputBroker.AssertReceivedOnTopic("ingest-queue", 1);

        // Trigger the handler which forwards to PubSub
        await inputBroker.SendAsync(raw);

        // Fanout channel received the enriched message
        fanoutBroker.AssertReceivedOnTopic("enriched-events", 1);
        var enrichedMsg = fanoutBroker.GetReceived<string>();
        Assert.That(enrichedMsg.Metadata["enriched"], Is.EqualTo("true"));
        Assert.That(enrichedMsg.Payload, Is.EqualTo("raw-event-data"));

        await inputBroker.DisposeAsync();
        await fanoutBroker.DisposeAsync();
    }

    [Test]
    public async Task PubSub_CausationChain_PreservedThroughChannelHops()
    {
        var channel = new PublishSubscribeChannel(
            _broker, _broker, NullLogger<PublishSubscribeChannel>.Instance);

        // Command → Event causation chain, both published through real channel
        var command = IntegrationEnvelope<string>.Create(
            "CreateUser", "WebApp", "user.create") with
        {
            Intent = MessageIntent.Command,
        };

        var evt = IntegrationEnvelope<string>.Create(
            "UserCreated", "UserService", "user.created",
            correlationId: command.CorrelationId,
            causationId: command.MessageId) with
        {
            Intent = MessageIntent.Event,
        };

        await channel.PublishAsync(command, "commands", CancellationToken.None);
        await channel.PublishAsync(evt, "events", CancellationToken.None);

        // Both messages flowed through the real channel
        _broker.AssertReceivedCount(2);
        var receivedEvt = _broker.GetReceived<string>(1);
        Assert.That(receivedEvt.CausationId, Is.EqualTo(command.MessageId));
        Assert.That(receivedEvt.CorrelationId, Is.EqualTo(command.CorrelationId));
    }
}

public sealed record OrderPayload(string OrderId, string Product, int Quantity, decimal Price);
