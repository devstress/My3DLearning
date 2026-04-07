// ============================================================================
// Tutorial 01 – Introduction (Lab)
// ============================================================================
// EIP Patterns: Point-to-Point Channel, Publish-Subscribe Channel
// End-to-End: Wire real channels with NatsBrokerEndpoint backed by real
// NATS JetStream via Aspire — real broker connections, no mocks.
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
    private NatsBrokerEndpoint _broker = null!;

    [SetUp]
    public async Task SetUp()
    {
        var natsUrl = await SharedTestAppHost.GetNatsUrlAsync();
        if (natsUrl is null)
            Assert.Ignore("Docker not available — skipping real broker test");

        _broker = new NatsBrokerEndpoint("broker", natsUrl);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_broker is not null) await _broker.DisposeAsync();
    }

    [Test]
    public async Task PointToPoint_SendAndReceive_MessageFlowsThroughChannel()
    {
        // Wire a real PointToPointChannel with NatsBrokerEndpoint
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        // Subscribe a handler that captures messages coming out of the channel
        IntegrationEnvelope<string>? received = null;
        var topic = $"orders-queue-{Guid.NewGuid():N}";
        await channel.ReceiveAsync<string>(topic, "order-processor",
            msg => { received = msg; return Task.CompletedTask; },
            CancellationToken.None);

        // Small delay to let subscription establish on real NATS
        await Task.Delay(500);

        // Send a command through the real channel
        var order = IntegrationEnvelope<string>.Create(
            "PlaceOrder:ORD-001", "WebApp", "order.place") with
        {
            Intent = MessageIntent.Command,
        };
        await channel.SendAsync(order, topic, CancellationToken.None);

        // Wait for real NATS delivery
        await _broker.WaitForConsumedAsync(1, TimeSpan.FromSeconds(10));

        // The channel published to NatsBrokerEndpoint — verify it arrived
        _broker.AssertReceivedOnTopic(topic, 1);

        // The handler was invoked via real NATS subscription
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
        var topic = $"events-topic-{Guid.NewGuid():N}";

        await channel.SubscribeAsync<string>(topic, "audit-service",
            msg => { subscriber1Msg = msg; return Task.CompletedTask; },
            CancellationToken.None);
        await channel.SubscribeAsync<string>(topic, "notification-service",
            msg => { subscriber2Msg = msg; return Task.CompletedTask; },
            CancellationToken.None);

        await Task.Delay(500);

        // Publish an event through the real channel
        var evt = IntegrationEnvelope<string>.Create(
            "OrderShipped", "ShippingService", "order.shipped") with
        {
            Intent = MessageIntent.Event,
        };
        await channel.PublishAsync(evt, topic, CancellationToken.None);

        // Wait for real NATS delivery to both subscribers
        await _broker.WaitForConsumedAsync(2, TimeSpan.FromSeconds(10));

        // NatsBrokerEndpoint captured the published message
        _broker.AssertReceivedOnTopic(topic, 1);

        // Both subscribers received via real NATS
        Assert.That(subscriber1Msg, Is.Not.Null);
        Assert.That(subscriber2Msg, Is.Not.Null);
    }

    [Test]
    public async Task PointToPoint_MultipleMessages_AllDeliveredInSequence()
    {
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        var topic = $"batch-queue-{Guid.NewGuid():N}";

        // Send a batch of 5 messages through the real channel
        for (var i = 0; i < 5; i++)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"item-{i}", "BatchProducer", "batch.item");
            await channel.SendAsync(envelope, topic, CancellationToken.None);
        }

        // Wait for real NATS delivery
        await _broker.WaitForMessagesOnTopicAsync(topic, 5, TimeSpan.FromSeconds(10));

        // All 5 messages flowed through the real channel and arrived at NATS
        _broker.AssertReceivedOnTopic(topic, 5);
        Assert.That(_broker.GetReceived<string>(0).Payload, Is.EqualTo("item-0"));
        Assert.That(_broker.GetReceived<string>(4).Payload, Is.EqualTo("item-4"));
    }

    [Test]
    public async Task PointToPoint_DomainObject_FlowsThroughChannel()
    {
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        var topic = $"high-priority-orders-{Guid.NewGuid():N}";
        var order = new OrderPayload("ORD-500", "Laptop", 2, 1299.99m);
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            order, "CatalogService", "order.created") with
        {
            Intent = MessageIntent.Document,
            Priority = MessagePriority.High,
        };

        await channel.SendAsync(envelope, topic, CancellationToken.None);

        await _broker.WaitForMessagesOnTopicAsync(topic, 1, TimeSpan.FromSeconds(10));

        _broker.AssertReceivedOnTopic(topic, 1);
        var received = _broker.GetReceived<OrderPayload>();
        Assert.That(received.Payload.OrderId, Is.EqualTo("ORD-500"));
        Assert.That(received.Payload.Price, Is.EqualTo(1299.99m));
        Assert.That(received.Priority, Is.EqualTo(MessagePriority.High));
    }

    [Test]
    public async Task ChannelHop_P2PToHandler_ThenPubSubFanOut()
    {
        // Two real channels: P2P for input, PubSub for fanout
        var natsUrl = (await SharedTestAppHost.GetNatsUrlAsync())!;
        var inputBroker = new NatsBrokerEndpoint("input-broker", natsUrl);
        var fanoutBroker = new NatsBrokerEndpoint("fanout-broker", natsUrl);

        var inputChannel = new PointToPointChannel(
            inputBroker, inputBroker, NullLogger<PointToPointChannel>.Instance);
        var fanoutChannel = new PublishSubscribeChannel(
            fanoutBroker, fanoutBroker, NullLogger<PublishSubscribeChannel>.Instance);

        var ingestTopic = $"ingest-queue-{Guid.NewGuid():N}";
        var enrichedTopic = $"enriched-events-{Guid.NewGuid():N}";

        // Handler: receives from P2P, enriches, and publishes to PubSub
        await inputChannel.ReceiveAsync<string>(ingestTopic, "enricher",
            async msg =>
            {
                var enriched = msg with
                {
                    Metadata = new Dictionary<string, string> { ["enriched"] = "true" },
                };
                await fanoutChannel.PublishAsync(enriched, enrichedTopic, CancellationToken.None);
            }, CancellationToken.None);

        await Task.Delay(500);

        // Send a raw message into the P2P channel
        var raw = IntegrationEnvelope<string>.Create(
            "raw-event-data", "SensorService", "sensor.reading");
        await inputChannel.SendAsync(raw, ingestTopic, CancellationToken.None);

        // Wait for real NATS delivery through the hop
        await fanoutBroker.WaitForMessagesOnTopicAsync(enrichedTopic, 1, TimeSpan.FromSeconds(10));

        // Fanout channel received the enriched message
        fanoutBroker.AssertReceivedOnTopic(enrichedTopic, 1);
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

        var commandTopic = $"commands-{Guid.NewGuid():N}";
        var eventTopic = $"events-{Guid.NewGuid():N}";

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

        await channel.PublishAsync(command, commandTopic, CancellationToken.None);
        await channel.PublishAsync(evt, eventTopic, CancellationToken.None);

        // Wait for real NATS delivery
        await _broker.WaitForMessagesAsync(2, TimeSpan.FromSeconds(10));

        // Both messages flowed through the real channel
        _broker.AssertReceivedCount(2);
        var receivedEvt = _broker.GetReceived<string>(1);
        Assert.That(receivedEvt.CausationId, Is.EqualTo(command.MessageId));
        Assert.That(receivedEvt.CorrelationId, Is.EqualTo(command.CorrelationId));
    }
}

public sealed record OrderPayload(string OrderId, string Product, int Quantity, decimal Price);
