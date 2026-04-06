// ============================================================================
// Tutorial 41 – OpenClaw Web / Blazor UI Concepts (Exam)
// ============================================================================
// Coding challenges: full WhereIs flow, snapshot creation from complex
// envelope, and AI trace analysis integration.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial41;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full WhereIs Flow ──────────────────────────────────────

    [Test]
    public async Task Challenge1_FullWhereIsFlow_StoreEventsInspectByCorrelation()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "OrderShipment",
                Source = "Gateway",
                Stage = "Ingestion",
                Status = DeliveryStatus.Pending,
                BusinessKey = "ORD-42",
            },
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "OrderShipment",
                Source = "Router",
                Stage = "Routing",
                Status = DeliveryStatus.InFlight,
                BusinessKey = "ORD-42",
            },
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "OrderShipment",
                Source = "Connector",
                Stage = "Delivery",
                Status = DeliveryStatus.Delivered,
                BusinessKey = "ORD-42",
            },
        };

        var log = Substitute.For<IObservabilityEventLog>();
        log.GetByCorrelationIdAsync(correlationId, Arg.Any<CancellationToken>())
            .Returns(events);

        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        traceAnalyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message was ingested, routed, and delivered successfully to the target system.");

        var inspector = new MessageStateInspector(
            log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var result = await inspector.WhereIsByCorrelationAsync(correlationId);

        Assert.That(result.Found, Is.True);
        Assert.That(result.Events, Has.Count.EqualTo(3));
        Assert.That(result.LatestStage, Is.EqualTo("Delivery"));
        Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.Delivered));
        Assert.That(result.OllamaAvailable, Is.True);
        Assert.That(result.Summary, Does.Contain("delivered"));
    }

    // ── Challenge 2: Snapshot Creation from Complex Envelope ────────────────

    [Test]
    public void Challenge2_SnapshotCreation_FromComplexEnvelope()
    {
        var log = Substitute.For<IObservabilityEventLog>();
        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        var inspector = new MessageStateInspector(
            log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "complex-payload", "OrderService", "order.shipped");
        envelope.Metadata[MessageHeaders.TraceId] = "trace-abc-123";
        envelope.Metadata[MessageHeaders.SpanId] = "span-xyz-456";
        envelope.Metadata[MessageHeaders.RetryCount] = "2";

        var snapshot = inspector.CreateSnapshot(envelope, "Transform", DeliveryStatus.InFlight);

        Assert.That(snapshot.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(snapshot.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(snapshot.MessageType, Is.EqualTo("order.shipped"));
        Assert.That(snapshot.Source, Is.EqualTo("OrderService"));
        Assert.That(snapshot.CurrentStage, Is.EqualTo("Transform"));
        Assert.That(snapshot.DeliveryStatus, Is.EqualTo(DeliveryStatus.InFlight));
        Assert.That(snapshot.TraceId, Is.EqualTo("trace-abc-123"));
        Assert.That(snapshot.SpanId, Is.EqualTo("span-xyz-456"));
        Assert.That(snapshot.RetryCount, Is.EqualTo(2));
    }

    // ── Challenge 3: AI Trace Analysis Integration ──────────────────────────

    [Test]
    public async Task Challenge3_AiTraceAnalysisIntegration()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "PaymentEvent",
                Source = "PaymentGateway",
                Stage = "Ingestion",
                Status = DeliveryStatus.Pending,
            },
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "PaymentEvent",
                Source = "Validator",
                Stage = "Validation",
                Status = DeliveryStatus.Failed,
                Details = "Schema validation failed: missing required field 'amount'",
            },
        };

        var log = Substitute.For<IObservabilityEventLog>();
        log.GetByCorrelationIdAsync(correlationId, Arg.Any<CancellationToken>())
            .Returns(events);

        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        traceAnalyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("The payment message failed schema validation at the Validation stage. " +
                     "The required field 'amount' is missing from the payload.");

        var inspector = new MessageStateInspector(
            log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var result = await inspector.WhereIsByCorrelationAsync(correlationId);

        Assert.That(result.Found, Is.True);
        Assert.That(result.OllamaAvailable, Is.True);
        Assert.That(result.Summary, Does.Contain("amount"));
        Assert.That(result.LatestStage, Is.EqualTo("Validation"));
        Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.Failed));

        await traceAnalyzer.Received(1).WhereIsMessageAsync(
            correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
