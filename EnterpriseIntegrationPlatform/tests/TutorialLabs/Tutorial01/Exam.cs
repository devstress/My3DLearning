// ============================================================================
// Tutorial 01 – Introduction (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Apply ONE concept (channel hop with causation)
//   🟡 Intermediate — Combine concepts (fan-out pipeline with enrichment)
//   🔴 Advanced     — Design decision (record immutability across channels)
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial01;

[TestFixture]
public sealed class Exam
{
    private NatsBrokerEndpoint _broker = null!;
    private string _natsUrl = null!;

    [SetUp]
    public async Task SetUp()
    {
        var natsUrl = await SharedTestAppHost.GetNatsUrlAsync();
        if (natsUrl is null)
            Assert.Ignore("Docker not available — skipping real broker test");

        _natsUrl = natsUrl;
        _broker = new NatsBrokerEndpoint("broker", _natsUrl);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_broker is not null) await _broker.DisposeAsync();
    }

    // ── 🟢 STARTER — Single Channel Hop with Causation ──────────────────
    //
    // SCENARIO: An e-commerce WebStore submits a high-priority order command.
    // An order processor receives it via a P2P queue, processes it, and emits
    // an "order.processed" event via PubSub. The event must carry the correct
    // causation chain (CorrelationId + CausationId) so downstream services
    // can trace the order back to its origin.
    //
    // WHAT YOU PROVE: You can wire a P2P→PubSub pipeline and maintain
    // message lineage across the hop.
    // ─────────────────────────────────────────────────────────────────────

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

        // TODO: Create an IntegrationEnvelope<OrderPayload> with payload=order, source="WebStore", type="order.place"
        //       Set Intent=Command, Priority=High using `with` expression.
        IntegrationEnvelope<OrderPayload> command = null!; // ← replace with IntegrationEnvelope<OrderPayload>.Create(...) with { ... }

        // Pipeline: P2P command in → handler transforms → PubSub event out
        await commandChannel.ReceiveAsync<OrderPayload>(commandTopic, "order-processor",
            async msg =>
            {
                // TODO: Create an IntegrationEnvelope<string> event with payload $"Processed:{msg.Payload.OrderId}",
                //       source "OrderProcessor", type "order.processed",
                //       correlationId=msg.CorrelationId, causationId=msg.MessageId, Intent=Event.
                //       Then publish it to eventChannel on eventTopic.
                await Task.CompletedTask;
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
    //
    // SCENARIO: A billing service publishes an "invoice.paid" event. Two
    // independent downstream systems must react:
    //   1. Audit Writer — adds an audit-timestamp and sends to an audit queue
    //   2. Notification Sender — forwards the event to a notifications queue
    //
    // This tests PubSub fan-out → two P2P downstream channels — a common
    // enterprise pattern (event broadcasting with per-subscriber processing).
    //
    // WHAT YOU PROVE: You can wire fan-out to multiple downstream channels,
    // each with independent processing logic and metadata enrichment.
    // ─────────────────────────────────────────────────────────────────────

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
                // TODO: Create an enriched copy of msg using `with` that adds
                //       Metadata with key "audit-timestamp" = DateTimeOffset.UtcNow.ToString("O"),
                //       then send it to auditChannel on auditTopic.
                await Task.CompletedTask;
            }, CancellationToken.None);

        // Subscriber 2: Notification sender — forwards to notification queue
        await eventChannel.SubscribeAsync<string>(businessTopic, "notification-sender",
            async msg =>
            {
                // TODO: Forward msg to notifyChannel on notifyTopic.
                await Task.CompletedTask;
            }, CancellationToken.None);

        await Task.Delay(500);

        // TODO: Create an IntegrationEnvelope<string> with payload "InvoicePaid:INV-300",
        //       source "BillingService", type "invoice.paid", Intent=Event.
        IntegrationEnvelope<string> evt = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { ... }
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
    //
    // SCENARIO: An IoT gateway sends a sensor temperature reading. A
    // monitoring service detects the reading exceeds a threshold and creates
    // an enriched "alert" version using the `with` operator. Both the
    // original reading and the enriched alert must flow through separate
    // channels without mutating each other.
    //
    // This tests C# record immutability — `with` creates a new record,
    // leaving the original untouched. Both share the same MessageId because
    // `with` copies all fields (including identity). In production you would
    // give the alert a new MessageId; here we verify the `with` behavior.
    //
    // WHAT YOU PROVE: You understand record immutability and can send
    // original + enriched messages to separate channels without data leaks.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_ImmutableEnrichment_OriginalAndEnriched_SeparateChannels()
    {
        var channel = new PointToPointChannel(
            _broker, _broker, NullLogger<PointToPointChannel>.Instance);

        var rawTopic = $"raw-readings-{Guid.NewGuid():N}";
        var alertTopic = $"alerts-{Guid.NewGuid():N}";

        // TODO: Create the original sensor reading envelope with payload "sensor-reading:42.5",
        //       source "IoTGateway", type "sensor.temperature".
        IntegrationEnvelope<string> original = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        // TODO: Create an enriched alert from original using `with` expression:
        //       Priority=Critical, Metadata with "threshold-exceeded"="true" and "alert-level"="critical".
        IntegrationEnvelope<string> enriched = null!; // ← replace with original with { ... }

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
#endif
