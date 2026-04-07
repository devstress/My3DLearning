// ============================================================================
// Tutorial 41 – OpenClaw Web / Blazor UI Concepts (Lab)
// ============================================================================
// EIP Pattern: Message State Tracking (backing the "Where is my message?" UI).
// E2E: InMemoryMessageStateStore — record lifecycle events, query by
//      correlation/business-key, publish results to NatsBrokerEndpoint
//      (real NATS JetStream via Aspire).
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial41;

[TestFixture]
public sealed class Lab
{
    private static InMemoryMessageStateStore CreateStore() => new();

    private static MessageEvent MakeEvent(
        Guid messageId, Guid correlationId, string source, string stage,
        DeliveryStatus status, string? businessKey = null) =>
        new()
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            MessageType = "Order",
            Source = source,
            Stage = stage,
            Status = status,
            BusinessKey = businessKey,
        };


    // ── 1. State Tracking ────────────────────────────────────────────

    [Test]
    public async Task RecordEvent_QueryByCorrelation_PublishToNatsBrokerEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t41-record-query");
        var topic = AspireFixture.UniqueTopic("t41-state-results");

        var store = CreateStore();
        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();

        await store.RecordAsync(MakeEvent(msgId, corrId, "Gateway", "Ingestion", DeliveryStatus.Pending));

        var events = await store.GetByCorrelationIdAsync(corrId);
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Stage, Is.EqualTo("Ingestion"));

        var envelope = IntegrationEnvelope<string>.Create(
            events[0].Stage, "state-store", "state.query");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task RecordMultipleStages_TrackLifecycle_PublishLatest()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t41-lifecycle-track");
        var topic = AspireFixture.UniqueTopic("t41-lifecycle-events");

        var store = CreateStore();
        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();

        await store.RecordAsync(MakeEvent(msgId, corrId, "Gateway", "Ingestion", DeliveryStatus.Pending));
        await store.RecordAsync(MakeEvent(msgId, corrId, "Router", "Routing", DeliveryStatus.InFlight));
        await store.RecordAsync(MakeEvent(msgId, corrId, "Connector", "Delivery", DeliveryStatus.Delivered));

        var events = await store.GetByCorrelationIdAsync(corrId);
        Assert.That(events, Has.Count.EqualTo(3));

        var latest = await store.GetLatestByCorrelationIdAsync(corrId);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Stage, Is.EqualTo("Delivery"));
        Assert.That(latest.Status, Is.EqualTo(DeliveryStatus.Delivered));

        var envelope = IntegrationEnvelope<string>.Create(
            latest.Stage, "state-store", "lifecycle.complete");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Lifecycle Tracking ────────────────────────────────────────

    [Test]
    public async Task QueryByBusinessKey_PublishMatchingEvents()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t41-bizkey-query");
        var topic = AspireFixture.UniqueTopic("t41-bizkey-results");

        var store = CreateStore();
        var corrId = Guid.NewGuid();

        await store.RecordAsync(MakeEvent(Guid.NewGuid(), corrId, "Gateway", "Ingestion",
            DeliveryStatus.Pending, "ORD-123"));
        await store.RecordAsync(MakeEvent(Guid.NewGuid(), corrId, "Router", "Routing",
            DeliveryStatus.InFlight, "ORD-123"));

        var events = await store.GetByBusinessKeyAsync("ORD-123");
        Assert.That(events, Has.Count.EqualTo(2));
        Assert.That(events[0].BusinessKey, Is.EqualTo("ORD-123"));

        foreach (var evt in events)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                evt.Stage, "state-store", "bizkey.query");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 2);
    }

    [Test]
    public async Task QueryByMessageId_PublishEventHistory()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t41-msgid-query");
        var topic = AspireFixture.UniqueTopic("t41-message-history");

        var store = CreateStore();
        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();

        await store.RecordAsync(MakeEvent(msgId, corrId, "Gateway", "Ingestion", DeliveryStatus.Pending));
        await store.RecordAsync(MakeEvent(msgId, corrId, "Validator", "Validation", DeliveryStatus.InFlight));

        var events = await store.GetByMessageIdAsync(msgId);
        Assert.That(events, Has.Count.EqualTo(2));
        Assert.That(events[0].MessageId, Is.EqualTo(msgId));

        var envelope = IntegrationEnvelope<string>.Create(
            $"{events.Count} events", "state-store", "msgid.query");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 3. Business-Key Queries ──────────────────────────────────────

    [Test]
    public async Task GetLatestByCorrelation_NoneRecorded_ReturnsNull()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t41-empty-query");
        var topic = AspireFixture.UniqueTopic("t41-empty-results");

        var store = CreateStore();

        var latest = await store.GetLatestByCorrelationIdAsync(Guid.NewGuid());
        Assert.That(latest, Is.Null);

        var events = await store.GetByBusinessKeyAsync("MISSING-KEY");
        Assert.That(events, Is.Empty);

        var envelope = IntegrationEnvelope<string>.Create(
            "not-found", "state-store", "state.empty");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task PublishAllLifecycleEventsToNatsBrokerEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t41-lifecycle-all");
        var topic = AspireFixture.UniqueTopic("t41-lifecycle-stream");

        var store = CreateStore();
        var corrId = Guid.NewGuid();

        await store.RecordAsync(MakeEvent(Guid.NewGuid(), corrId, "Gateway", "Ingestion",
            DeliveryStatus.Pending, "SHIP-456"));
        await store.RecordAsync(MakeEvent(Guid.NewGuid(), corrId, "Transform", "Transform",
            DeliveryStatus.InFlight, "SHIP-456"));
        await store.RecordAsync(MakeEvent(Guid.NewGuid(), corrId, "Connector", "Delivery",
            DeliveryStatus.Delivered, "SHIP-456"));

        var events = await store.GetByCorrelationIdAsync(corrId);
        foreach (var evt in events)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{evt.Stage}:{evt.Status}", "state-store", evt.MessageType);
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);
        var all = nats.GetAllReceived<string>(topic);
        Assert.That(all[0].Payload, Is.EqualTo("Ingestion:Pending"));
        Assert.That(all[2].Payload, Is.EqualTo("Delivery:Delivered"));
    }
}
