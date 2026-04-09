// ============================================================================
// Tutorial 01 – Introduction (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial01;

[TestFixture]
public sealed class ExamAnswers
{
    private NatsBrokerEndpoint _broker = null!;
    private string _natsUrl = null!;

    [SetUp]
    public async Task SetUp()
    {
        var natsUrl = await SharedTestAppHost.GetNatsUrlAsync();
        if (natsUrl is null)
        {
            Assert.Fail("Docker not available — skipping real broker test");
            return;
        }

        _natsUrl = natsUrl;
        _broker = new NatsBrokerEndpoint("broker", _natsUrl);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_broker is not null) await _broker.DisposeAsync();
    }

    // ── 🟢 STARTER — Single Channel Hop with Causation ──────────────────

    [Test]
    public async Task Starter_CommandToEvent_SingleChannelHop()
    {
        var commandBroker = new NatsBrokerEndpoint("commands", _natsUrl);
        var eventBroker = new NatsBrokerEndpoint("events", _natsUrl);

        var commandChannel = new PointToPointChannel(
            commandBroker, commandBroker, NullLogger<PointToPointChannel>.Instance);
        var eventChannel = new PublishSubscribeChannel(
            eventBroker, eventBroker, NullLogger<PublishSubscribeChannel>.Instance);

        var commandTopic = $"order-commands-{Guid.NewGuid():N}";
        var eventTopic = $"order-events-{Guid.NewGuid():N}";

        // Domain command: e-commerce order
        var order = new OrderPayload("ORD-777", "Server Rack", 1, 4999.99m);
        var command = IntegrationEnvelope<OrderPayload>.Create(
            order, "WebStore", "order.place") with
        {
            Intent = MessageIntent.Command,
            Priority = MessagePriority.High,
        };

        // Pipeline: P2P command in → handler transforms → PubSub event out
        await commandChannel.ReceiveAsync<OrderPayload>(commandTopic, "order-processor",
            async msg =>
            {
                var orderEvent = IntegrationEnvelope<string>.Create(
                    $"Processed:{msg.Payload.OrderId}",
                    "OrderProcessor",
                    "order.processed",
                    correlationId: msg.CorrelationId,
                    causationId: msg.MessageId) with
                {
                    Intent = MessageIntent.Event,
                };
                await eventChannel.PublishAsync(orderEvent, eventTopic, CancellationToken.None);
            }, CancellationToken.None);

        await Task.Delay(500);
        await commandChannel.SendAsync(command, commandTopic, CancellationToken.None);
        await eventBroker.WaitForMessagesOnTopicAsync(eventTopic, 1, TimeSpan.FromSeconds(10));

        // ── Assertions: verify the full pipeline ──
        commandBroker.AssertReceivedOnTopic(commandTopic, 1);
        eventBroker.AssertReceivedOnTopic(eventTopic, 1);

        var processedEvent = eventBroker.GetReceived<string>();
        Assert.That(processedEvent.Payload, Is.EqualTo("Processed:ORD-777"));
        Assert.That(processedEvent.CausationId, Is.EqualTo(command.MessageId));
        Assert.That(processedEvent.CorrelationId, Is.EqualTo(command.CorrelationId));
        Assert.That(processedEvent.Intent, Is.EqualTo(MessageIntent.Event));

        await commandBroker.DisposeAsync();
        await eventBroker.DisposeAsync();
    }

    // ── 🟡 INTERMEDIATE — Fan-Out Pipeline with Enrichment ──────────────

    [Test]
    public async Task Intermediate_FanOutPipeline_MultipleDownstreamChannels()
    {
        var pubsubBroker = new NatsBrokerEndpoint("pubsub", _natsUrl);
        var auditBroker = new NatsBrokerEndpoint("audit", _natsUrl);
        var notifyBroker = new NatsBrokerEndpoint("notify", _natsUrl);

        var eventChannel = new PublishSubscribeChannel(
            pubsubBroker, pubsubBroker, NullLogger<PublishSubscribeChannel>.Instance);
        var auditChannel = new PointToPointChannel(
            auditBroker, auditBroker, NullLogger<PointToPointChannel>.Instance);
        var notifyChannel = new PointToPointChannel(
            notifyBroker, notifyBroker, NullLogger<PointToPointChannel>.Instance);

        var businessTopic = $"business-events-{Guid.NewGuid():N}";
        var auditTopic = $"audit-log-{Guid.NewGuid():N}";
        var notifyTopic = $"notifications-{Guid.NewGuid():N}";

        // Subscriber 1: Audit writer — enriches with timestamp, routes to audit queue
        await eventChannel.SubscribeAsync<string>(businessTopic, "audit-writer",
            async msg =>
            {
                var auditMsg = msg with
                {
                    Metadata = new Dictionary<string, string> { ["audit-timestamp"] = DateTimeOffset.UtcNow.ToString("O") },
                };
                await auditChannel.SendAsync(auditMsg, auditTopic, CancellationToken.None);
            }, CancellationToken.None);

        // Subscriber 2: Notification sender — forwards to notification queue
        await eventChannel.SubscribeAsync<string>(businessTopic, "notification-sender",
            async msg =>
            {
                await notifyChannel.SendAsync(msg, notifyTopic, CancellationToken.None);
            }, CancellationToken.None);

        await Task.Delay(500);

        // Business event: invoice paid
        var evt = IntegrationEnvelope<string>.Create(
            "InvoicePaid:INV-300", "BillingService", "invoice.paid") with
        {
            Intent = MessageIntent.Event,
        };
        await eventChannel.PublishAsync(evt, businessTopic, CancellationToken.None);

        await auditBroker.WaitForMessagesOnTopicAsync(auditTopic, 1, TimeSpan.FromSeconds(10));
        await notifyBroker.WaitForMessagesOnTopicAsync(notifyTopic, 1, TimeSpan.FromSeconds(10));

        // ── Assertions: verify fan-out reached both downstream channels ──
        pubsubBroker.AssertReceivedOnTopic(businessTopic, 1);
        auditBroker.AssertReceivedOnTopic(auditTopic, 1);
        notifyBroker.AssertReceivedOnTopic(notifyTopic, 1);

        var auditRecord = auditBroker.GetReceived<string>();
        Assert.That(auditRecord.Metadata.ContainsKey("audit-timestamp"), Is.True);
        Assert.That(auditRecord.Payload, Is.EqualTo("InvoicePaid:INV-300"));

        await pubsubBroker.DisposeAsync();
        await auditBroker.DisposeAsync();
        await notifyBroker.DisposeAsync();
    }

    // ── 🔴 ADVANCED — Record Immutability Across Channels ───────────────

    [Test]
    public async Task Advanced_ImmutableEnrichment_OriginalAndEnriched_SeparateChannels()
    {
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        var rawTopic = $"raw-readings-{Guid.NewGuid():N}";
        var alertTopic = $"alerts-{Guid.NewGuid():N}";

        // Original sensor reading
        var original = IntegrationEnvelope<string>.Create(
            "sensor-reading:42.5", "IoTGateway", "sensor.temperature");

        // Enriched alert — created via `with` (original is NOT mutated)
        var enriched = original with
        {
            Priority = MessagePriority.Critical,
            Metadata = new Dictionary<string, string>
            {
                ["threshold-exceeded"] = "true",
                ["alert-level"] = "critical",
            },
        };

        // Send original → raw readings channel, enriched → alerts channel
        await channel.SendAsync(original, rawTopic, CancellationToken.None);
        await channel.SendAsync(enriched, alertTopic, CancellationToken.None);

        await _broker.WaitForMessagesAsync(2, TimeSpan.FromSeconds(10));

        // ── Assertions: verify immutability and separate delivery ──
        _broker.AssertReceivedCount(2);
        _broker.AssertReceivedOnTopic(rawTopic, 1);
        _broker.AssertReceivedOnTopic(alertTopic, 1);

        var rawMsg = _broker.GetReceived<string>(0);
        var alertMsg = _broker.GetReceived<string>(1);

        // Original retains Normal priority — NOT mutated by enrichment
        Assert.That(rawMsg.Priority, Is.EqualTo(MessagePriority.Normal));
        Assert.That(alertMsg.Priority, Is.EqualTo(MessagePriority.Critical));
        Assert.That(alertMsg.Metadata["alert-level"], Is.EqualTo("critical"));

        // Same MessageId — record `with` copies identity fields
        Assert.That(rawMsg.MessageId, Is.EqualTo(alertMsg.MessageId));
    }
}
