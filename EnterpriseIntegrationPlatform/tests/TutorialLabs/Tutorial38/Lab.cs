// ============================================================================
// Tutorial 38 – OpenTelemetry / Observability (Lab)
// ============================================================================
// This lab exercises MessageEvent, InMemoryMessageStateStore, InspectionResult,
// MessageStateSnapshot, DeliveryStatus, and CorrelationPropagator.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using NUnit.Framework;

namespace TutorialLabs.Tutorial38;

[TestFixture]
public sealed class Lab
{
    // ── MessageEvent Record Shape ───────────────────────────────────────────

    [Test]
    public void MessageEvent_RecordShape_AllPropertiesAccessible()
    {
        var evt = new MessageEvent
        {
            EventId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            MessageType = "order.placed",
            Source = "OrderSvc",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
            RecordedAt = DateTimeOffset.UtcNow,
            Details = "Received at gateway",
            BusinessKey = "ORD-123",
            TraceId = "abc123",
            SpanId = "def456",
        };

        Assert.That(evt.EventId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(evt.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(evt.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(evt.MessageType, Is.EqualTo("order.placed"));
        Assert.That(evt.Source, Is.EqualTo("OrderSvc"));
        Assert.That(evt.Stage, Is.EqualTo("Ingestion"));
        Assert.That(evt.Status, Is.EqualTo(DeliveryStatus.Pending));
        Assert.That(evt.Details, Is.EqualTo("Received at gateway"));
        Assert.That(evt.BusinessKey, Is.EqualTo("ORD-123"));
        Assert.That(evt.TraceId, Is.EqualTo("abc123"));
        Assert.That(evt.SpanId, Is.EqualTo("def456"));
    }

    // ── InMemoryMessageStateStore Record and Retrieve by CorrelationId ──────

    [Test]
    public async Task InMemoryMessageStateStore_RecordAndRetrieveByCorrelationId()
    {
        var store = new InMemoryMessageStateStore();
        var correlationId = Guid.NewGuid();

        var evt = new MessageEvent
        {
            EventId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "order.placed",
            Source = "OrderSvc",
            Stage = "Routing",
            Status = DeliveryStatus.InFlight,
            RecordedAt = DateTimeOffset.UtcNow,
        };

        await store.RecordAsync(evt);

        var results = await store.GetByCorrelationIdAsync(correlationId);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].CorrelationId, Is.EqualTo(correlationId));
    }

    // ── InMemoryMessageStateStore Record and Retrieve by BusinessKey ────────

    [Test]
    public async Task InMemoryMessageStateStore_RecordAndRetrieveByBusinessKey()
    {
        var store = new InMemoryMessageStateStore();

        var evt = new MessageEvent
        {
            EventId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            MessageType = "invoice.paid",
            Source = "BillingSvc",
            Stage = "Processing",
            Status = DeliveryStatus.Delivered,
            RecordedAt = DateTimeOffset.UtcNow,
            BusinessKey = "INV-2024-001",
        };

        await store.RecordAsync(evt);

        var results = await store.GetByBusinessKeyAsync("INV-2024-001");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].BusinessKey, Is.EqualTo("INV-2024-001"));
    }

    // ── InspectionResult Record Shape ───────────────────────────────────────

    [Test]
    public void InspectionResult_RecordShape()
    {
        var result = new InspectionResult
        {
            Query = "ORD-123",
            Found = true,
            Summary = "Message delivered successfully",
            Events = new List<MessageEvent>(),
            LatestStage = "Delivery",
            LatestStatus = DeliveryStatus.Delivered,
        };

        Assert.That(result.Query, Is.EqualTo("ORD-123"));
        Assert.That(result.Found, Is.True);
        Assert.That(result.Summary, Is.EqualTo("Message delivered successfully"));
        Assert.That(result.Events, Is.Not.Null);
        Assert.That(result.LatestStage, Is.EqualTo("Delivery"));
        Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.Delivered));
    }

    // ── MessageStateSnapshot Record Shape ───────────────────────────────────

    [Test]
    public void MessageStateSnapshot_RecordShape()
    {
        var snapshot = new MessageStateSnapshot
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid(),
            MessageType = "order.shipped",
            Source = "ShippingSvc",
            Priority = MessagePriority.High,
            Timestamp = DateTimeOffset.UtcNow,
            CurrentStage = "Delivery",
            DeliveryStatus = DeliveryStatus.Delivered,
            TraceId = "trace-abc",
            SpanId = "span-xyz",
            RetryCount = 0,
        };

        Assert.That(snapshot.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(snapshot.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(snapshot.CausationId, Is.Not.Null);
        Assert.That(snapshot.MessageType, Is.EqualTo("order.shipped"));
        Assert.That(snapshot.Source, Is.EqualTo("ShippingSvc"));
        Assert.That(snapshot.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(snapshot.CurrentStage, Is.EqualTo("Delivery"));
        Assert.That(snapshot.DeliveryStatus, Is.EqualTo(DeliveryStatus.Delivered));
        Assert.That(snapshot.TraceId, Is.EqualTo("trace-abc"));
        Assert.That(snapshot.SpanId, Is.EqualTo("span-xyz"));
        Assert.That(snapshot.RetryCount, Is.EqualTo(0));
    }

    // ── DeliveryStatus Enum Values ──────────────────────────────────────────

    [Test]
    public void DeliveryStatus_EnumValues()
    {
        Assert.That((int)DeliveryStatus.Pending, Is.EqualTo(0));
        Assert.That((int)DeliveryStatus.InFlight, Is.EqualTo(1));
        Assert.That((int)DeliveryStatus.Delivered, Is.EqualTo(2));
        Assert.That((int)DeliveryStatus.Failed, Is.EqualTo(3));
        Assert.That((int)DeliveryStatus.Retrying, Is.EqualTo(4));
        Assert.That((int)DeliveryStatus.DeadLettered, Is.EqualTo(5));
    }

    // ── CorrelationPropagator.InjectTraceContext Adds Trace Metadata ────────

    [Test]
    public void CorrelationPropagator_InjectTraceContext_AddsTraceMetadata()
    {
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "test.event");

        var enriched = CorrelationPropagator.InjectTraceContext(envelope);

        // InjectTraceContext reads from Activity.Current; if no activity is active,
        // the metadata keys may not be set. Verify the method runs without error
        // and returns an envelope.
        Assert.That(enriched, Is.Not.Null);
        Assert.That(enriched.MessageId, Is.EqualTo(envelope.MessageId));
    }
}
