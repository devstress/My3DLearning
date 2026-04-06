// ============================================================================
// Tutorial 38 – OpenTelemetry / Observability (Exam)
// ============================================================================
// Coding challenges: full message lifecycle tracking, WhereIs inspection,
// and creating a MessageStateSnapshot from an envelope.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial38;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full Message Lifecycle Tracking ─────────────────────────

    [Test]
    public async Task Challenge1_FullMessageLifecycleTracking()
    {
        var store = new InMemoryMessageStateStore();
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var stages = new[]
        {
            (Stage: "Ingestion", Status: DeliveryStatus.Pending),
            (Stage: "Routing",   Status: DeliveryStatus.InFlight),
            (Stage: "Transform", Status: DeliveryStatus.InFlight),
            (Stage: "Delivery",  Status: DeliveryStatus.Delivered),
        };

        foreach (var (stage, status) in stages)
        {
            await store.RecordAsync(new MessageEvent
            {
                EventId = Guid.NewGuid(),
                MessageId = messageId,
                CorrelationId = correlationId,
                MessageType = "order.placed",
                Source = "OrderSvc",
                Stage = stage,
                Status = status,
                RecordedAt = DateTimeOffset.UtcNow,
                BusinessKey = "ORD-999",
            });
        }

        var trail = await store.GetByCorrelationIdAsync(correlationId);

        Assert.That(trail, Has.Count.EqualTo(4));
        Assert.That(trail[0].Stage, Is.EqualTo("Ingestion"));
        Assert.That(trail[^1].Stage, Is.EqualTo("Delivery"));
        Assert.That(trail[^1].Status, Is.EqualTo(DeliveryStatus.Delivered));

        var latest = await store.GetLatestByCorrelationIdAsync(correlationId);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Stage, Is.EqualTo("Delivery"));
    }

    // ── Challenge 2: WhereIs Inspection with Mocked Services ────────────────

    [Test]
    public async Task Challenge2_WhereIsInspection_WithMockedServices()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                EventId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "order.placed",
                Source = "OrderSvc",
                Stage = "Routing",
                Status = DeliveryStatus.InFlight,
                RecordedAt = DateTimeOffset.UtcNow,
                BusinessKey = "ORD-555",
            },
        };

        var eventLog = Substitute.For<IObservabilityEventLog>();
        eventLog.GetByBusinessKeyAsync("ORD-555", Arg.Any<CancellationToken>())
            .Returns(events);

        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        traceAnalyzer.WhereIsMessageAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message is currently being routed");

        var inspector = new MessageStateInspector(
            eventLog, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var result = await inspector.WhereIsAsync("ORD-555");

        Assert.That(result.Query, Is.EqualTo("ORD-555"));
        Assert.That(result.Found, Is.True);
        Assert.That(result.Events, Has.Count.EqualTo(1));
    }

    // ── Challenge 3: Create MessageStateSnapshot from Envelope ──────────────

    [Test]
    public void Challenge3_CreateMessageStateSnapshot_FromEnvelope()
    {
        var eventLog = Substitute.For<IObservabilityEventLog>();
        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        var inspector = new MessageStateInspector(
            eventLog, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "Order data", "OrderSvc", "order.placed");

        var snapshot = inspector.CreateSnapshot(
            envelope, "Ingestion", DeliveryStatus.Pending);

        Assert.That(snapshot.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(snapshot.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(snapshot.MessageType, Is.EqualTo("order.placed"));
        Assert.That(snapshot.Source, Is.EqualTo("OrderSvc"));
        Assert.That(snapshot.CurrentStage, Is.EqualTo("Ingestion"));
        Assert.That(snapshot.DeliveryStatus, Is.EqualTo(DeliveryStatus.Pending));
    }
}
