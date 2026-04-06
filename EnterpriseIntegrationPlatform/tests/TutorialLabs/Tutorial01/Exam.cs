// ============================================================================
// Tutorial 01 – Introduction (Exam)
// ============================================================================
// EIP Patterns: Point-to-Point Channel, Publish-Subscribe Channel, Pipeline
// End-to-End: Multi-stage pipelines through real channels — domain objects,
// causation chains, message transformation, and channel orchestration.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial01;

[TestFixture]
public sealed class Exam
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
    public async Task Pipeline_OrderCommand_TransformedToEvent_ThroughRealChannels()
    {
        // Stage 1: P2P channel receives command
        var commandBroker = new MockEndpoint("commands");
        var eventBroker = new MockEndpoint("events");
        var commandChannel = new PointToPointChannel(
            commandBroker, commandBroker, NullLogger<PointToPointChannel>.Instance);
        var eventChannel = new PublishSubscribeChannel(
            eventBroker, eventBroker, NullLogger<PublishSubscribeChannel>.Instance);

        // Wire handler: command in → event out (simulates order processing)
        await commandChannel.ReceiveAsync<OrderPayload>("order-commands", "order-processor",
            async msg =>
            {
                // Transform command to event with causation chain
                var orderEvent = IntegrationEnvelope<string>.Create(
                    $"Processed:{msg.Payload.OrderId}",
                    "OrderProcessor",
                    "order.processed",
                    correlationId: msg.CorrelationId,
                    causationId: msg.MessageId) with
                {
                    Intent = MessageIntent.Event,
                };
                await eventChannel.PublishAsync(orderEvent, "order-events", CancellationToken.None);
            }, CancellationToken.None);

        // Send a domain command through the real pipeline
        var order = new OrderPayload("ORD-777", "Server Rack", 1, 4999.99m);
        var command = IntegrationEnvelope<OrderPayload>.Create(
            order, "WebStore", "order.place") with
        {
            Intent = MessageIntent.Command,
            Priority = MessagePriority.High,
        };
        await commandChannel.SendAsync(command, "order-commands", CancellationToken.None);

        // Command arrived at command broker
        commandBroker.AssertReceivedOnTopic("order-commands", 1);

        // Trigger the processing handler
        await commandBroker.SendAsync(command);

        // Event was published through the event channel
        eventBroker.AssertReceivedOnTopic("order-events", 1);
        var processedEvent = eventBroker.GetReceived<string>();
        Assert.That(processedEvent.Payload, Is.EqualTo("Processed:ORD-777"));
        Assert.That(processedEvent.CausationId, Is.EqualTo(command.MessageId));
        Assert.That(processedEvent.CorrelationId, Is.EqualTo(command.CorrelationId));
        Assert.That(processedEvent.Intent, Is.EqualTo(MessageIntent.Event));

        await commandBroker.DisposeAsync();
        await eventBroker.DisposeAsync();
    }

    [Test]
    public async Task FanOut_EventBroadcast_MultipleDownstreamChannelsReceive()
    {
        var pubsubBroker = new MockEndpoint("pubsub");
        var auditBroker = new MockEndpoint("audit");
        var notifyBroker = new MockEndpoint("notify");

        var eventChannel = new PublishSubscribeChannel(
            pubsubBroker, pubsubBroker, NullLogger<PublishSubscribeChannel>.Instance);
        var auditChannel = new PointToPointChannel(
            auditBroker, auditBroker, NullLogger<PointToPointChannel>.Instance);
        var notifyChannel = new PointToPointChannel(
            notifyBroker, notifyBroker, NullLogger<PointToPointChannel>.Instance);

        // Two subscribers fan out to different downstream channels
        await eventChannel.SubscribeAsync<string>("business-events", "audit-writer",
            async msg =>
            {
                var auditMsg = msg with
                {
                    Metadata = new Dictionary<string, string> { ["audit-timestamp"] = DateTimeOffset.UtcNow.ToString("O") },
                };
                await auditChannel.SendAsync(auditMsg, "audit-log", CancellationToken.None);
            }, CancellationToken.None);

        await eventChannel.SubscribeAsync<string>("business-events", "notification-sender",
            async msg =>
            {
                await notifyChannel.SendAsync(msg, "notifications", CancellationToken.None);
            }, CancellationToken.None);

        // Publish a business event
        var evt = IntegrationEnvelope<string>.Create(
            "InvoicePaid:INV-300", "BillingService", "invoice.paid") with
        {
            Intent = MessageIntent.Event,
        };
        await eventChannel.PublishAsync(evt, "business-events", CancellationToken.None);

        // PubSub published the event
        pubsubBroker.AssertReceivedOnTopic("business-events", 1);

        // Trigger fan-out to both subscribers
        await pubsubBroker.SendAsync(evt);

        // Both downstream channels received their copies
        auditBroker.AssertReceivedOnTopic("audit-log", 1);
        notifyBroker.AssertReceivedOnTopic("notifications", 1);

        var auditRecord = auditBroker.GetReceived<string>();
        Assert.That(auditRecord.Metadata.ContainsKey("audit-timestamp"), Is.True);
        Assert.That(auditRecord.Payload, Is.EqualTo("InvoicePaid:INV-300"));

        await pubsubBroker.DisposeAsync();
        await auditBroker.DisposeAsync();
        await notifyBroker.DisposeAsync();
    }

    [Test]
    public async Task ImmutableModification_OriginalAndEnriched_BothFlowThroughChannels()
    {
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        var original = IntegrationEnvelope<string>.Create(
            "sensor-reading:42.5", "IoTGateway", "sensor.temperature");
        var enriched = original with
        {
            Priority = MessagePriority.Critical,
            Metadata = new Dictionary<string, string>
            {
                ["threshold-exceeded"] = "true",
                ["alert-level"] = "critical",
            },
        };

        // Both versions flow through the same real channel
        await channel.SendAsync(original, "raw-readings", CancellationToken.None);
        await channel.SendAsync(enriched, "alerts", CancellationToken.None);

        _broker.AssertReceivedCount(2);
        _broker.AssertReceivedOnTopic("raw-readings", 1);
        _broker.AssertReceivedOnTopic("alerts", 1);

        // Original retains Normal priority, enriched has Critical
        var rawMsg = _broker.GetReceived<string>(0);
        var alertMsg = _broker.GetReceived<string>(1);
        Assert.That(rawMsg.Priority, Is.EqualTo(MessagePriority.Normal));
        Assert.That(alertMsg.Priority, Is.EqualTo(MessagePriority.Critical));
        Assert.That(alertMsg.Metadata["alert-level"], Is.EqualTo("critical"));
        // Same message identity — record immutability preserved
        Assert.That(rawMsg.MessageId, Is.EqualTo(alertMsg.MessageId));
    }
}
