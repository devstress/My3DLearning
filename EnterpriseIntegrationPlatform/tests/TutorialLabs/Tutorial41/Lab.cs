// ============================================================================
// Tutorial 41 – OpenClaw Web / Blazor UI Concepts (Lab)
// ============================================================================
// EIP Pattern: Message State Tracking (backing the "Where is my message?" UI).
// E2E: InMemoryMessageStateStore — record lifecycle events, query by
//      correlation/business-key, publish results to MockEndpoint.
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial41;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("openclaw-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

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
    public async Task RecordEvent_QueryByCorrelation_PublishToMockEndpoint()
    {
        var store = CreateStore();
        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();

        await store.RecordAsync(MakeEvent(msgId, corrId, "Gateway", "Ingestion", DeliveryStatus.Pending));

        var events = await store.GetByCorrelationIdAsync(corrId);
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Stage, Is.EqualTo("Ingestion"));

        var envelope = IntegrationEnvelope<string>.Create(
            events[0].Stage, "state-store", "state.query");
        await _output.PublishAsync(envelope, "state-results", default);
        _output.AssertReceivedOnTopic("state-results", 1);
    }

    [Test]
    public async Task RecordMultipleStages_TrackLifecycle_PublishLatest()
    {
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
        await _output.PublishAsync(envelope, "lifecycle-events", default);
        _output.AssertReceivedOnTopic("lifecycle-events", 1);
    }


    // ── 2. Lifecycle Tracking ────────────────────────────────────────

    [Test]
    public async Task QueryByBusinessKey_PublishMatchingEvents()
    {
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
            await _output.PublishAsync(envelope, "bizkey-results", default);
        }

        _output.AssertReceivedOnTopic("bizkey-results", 2);
    }

    [Test]
    public async Task QueryByMessageId_PublishEventHistory()
    {
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
        await _output.PublishAsync(envelope, "message-history", default);
        _output.AssertReceivedOnTopic("message-history", 1);
    }


    // ── 3. Business-Key Queries ──────────────────────────────────────

    [Test]
    public async Task GetLatestByCorrelation_NoneRecorded_ReturnsNull()
    {
        var store = CreateStore();

        var latest = await store.GetLatestByCorrelationIdAsync(Guid.NewGuid());
        Assert.That(latest, Is.Null);

        var events = await store.GetByBusinessKeyAsync("MISSING-KEY");
        Assert.That(events, Is.Empty);

        var envelope = IntegrationEnvelope<string>.Create(
            "not-found", "state-store", "state.empty");
        await _output.PublishAsync(envelope, "empty-results", default);
        _output.AssertReceivedOnTopic("empty-results", 1);
    }

    [Test]
    public async Task PublishAllLifecycleEventsToMockEndpoint()
    {
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
            await _output.PublishAsync(envelope, "lifecycle-stream", default);
        }

        _output.AssertReceivedOnTopic("lifecycle-stream", 3);
        var all = _output.GetAllReceived<string>("lifecycle-stream");
        Assert.That(all[0].Payload, Is.EqualTo("Ingestion:Pending"));
        Assert.That(all[2].Payload, Is.EqualTo("Delivery:Delivered"));
    }
}
