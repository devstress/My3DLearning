// ============================================================================
// Tutorial 41 – OpenClaw Web / Blazor UI Concepts (Lab)
// ============================================================================
// This lab exercises the underlying services behind the "Where is my message?"
// UI: MessageStateInspector, InspectionResult, ITraceAnalyzer, and
// IObservabilityEventLog via mocks and record shape validation.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial41;

[TestFixture]
public sealed class Lab
{
    // ── InspectionResult Record Shape ────────────────────────────────────────

    [Test]
    public void InspectionResult_RecordShape_HasExpectedProperties()
    {
        var result = new InspectionResult
        {
            Query = "ORD-123",
            Found = true,
            Summary = "Message delivered",
            OllamaAvailable = false,
            Events = new List<MessageEvent>(),
            LatestStage = "Delivery",
            LatestStatus = DeliveryStatus.Delivered,
        };

        Assert.That(result.Query, Is.EqualTo("ORD-123"));
        Assert.That(result.Found, Is.True);
        Assert.That(result.Summary, Is.EqualTo("Message delivered"));
        Assert.That(result.OllamaAvailable, Is.False);
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.LatestStage, Is.EqualTo("Delivery"));
        Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.Delivered));
    }

    // ── WhereIsByCorrelationAsync with Mocked Dependencies ──────────────────

    [Test]
    public async Task MessageStateInspector_WhereIsByCorrelationAsync_ReturnsResult()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "Order",
                Source = "Gateway",
                Stage = "Ingestion",
                Status = DeliveryStatus.Pending,
            },
        };

        var log = Substitute.For<IObservabilityEventLog>();
        log.GetByCorrelationIdAsync(correlationId, Arg.Any<CancellationToken>())
            .Returns(events);

        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        traceAnalyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message is at Ingestion stage");

        var inspector = new MessageStateInspector(
            log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var result = await inspector.WhereIsByCorrelationAsync(correlationId);

        Assert.That(result.Found, Is.True);
        Assert.That(result.Events, Has.Count.EqualTo(1));
        Assert.That(result.LatestStage, Is.EqualTo("Ingestion"));
    }

    // ── WhereIsAsync with Mocked Dependencies (Business Key Search) ─────────

    [Test]
    public async Task MessageStateInspector_WhereIsAsync_ReturnsResult()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "Shipment",
                Source = "Warehouse",
                Stage = "Routing",
                Status = DeliveryStatus.InFlight,
                BusinessKey = "SHIP-456",
            },
        };

        var log = Substitute.For<IObservabilityEventLog>();
        log.GetByBusinessKeyAsync("SHIP-456", Arg.Any<CancellationToken>())
            .Returns(events);

        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        traceAnalyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message is being routed");

        var inspector = new MessageStateInspector(
            log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var result = await inspector.WhereIsAsync("SHIP-456");

        Assert.That(result.Found, Is.True);
        Assert.That(result.Query, Is.EqualTo("SHIP-456"));
        Assert.That(result.LatestStage, Is.EqualTo("Routing"));
    }

    // ── CreateSnapshot Creates Valid Snapshot ────────────────────────────────

    [Test]
    public void MessageStateInspector_CreateSnapshot_CreatesValidSnapshot()
    {
        var log = Substitute.For<IObservabilityEventLog>();
        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();

        var inspector = new MessageStateInspector(
            log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("payload", "TestSvc", "order.created");

        var snapshot = inspector.CreateSnapshot(envelope, "Ingestion", DeliveryStatus.Pending);

        Assert.That(snapshot.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(snapshot.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(snapshot.CurrentStage, Is.EqualTo("Ingestion"));
        Assert.That(snapshot.DeliveryStatus, Is.EqualTo(DeliveryStatus.Pending));
        Assert.That(snapshot.Source, Is.EqualTo("TestSvc"));
        Assert.That(snapshot.MessageType, Is.EqualTo("order.created"));
    }

    // ── Mock ITraceAnalyzer.WhereIsMessageAsync Returns Analysis ────────────

    [Test]
    public async Task Mock_ITraceAnalyzer_WhereIsMessageAsync_ReturnsAnalysis()
    {
        var analyzer = Substitute.For<ITraceAnalyzer>();
        var correlationId = Guid.NewGuid();

        analyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message is in the dead-letter queue after 3 retries");

        var analysis = await analyzer.WhereIsMessageAsync(correlationId, "{}");

        Assert.That(analysis, Does.Contain("dead-letter"));
        await analyzer.Received(1).WhereIsMessageAsync(
            correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Mock IObservabilityEventLog.GetByBusinessKeyAsync Returns Events ────

    [Test]
    public async Task Mock_IObservabilityEventLog_GetByBusinessKeyAsync_ReturnsEvents()
    {
        var log = Substitute.For<IObservabilityEventLog>();
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "Invoice",
                Source = "Billing",
                Stage = "Transform",
                Status = DeliveryStatus.InFlight,
                BusinessKey = "INV-789",
            },
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "Invoice",
                Source = "Billing",
                Stage = "Delivery",
                Status = DeliveryStatus.Delivered,
                BusinessKey = "INV-789",
            },
        };

        log.GetByBusinessKeyAsync("INV-789", Arg.Any<CancellationToken>())
            .Returns(events);

        var result = await log.GetByBusinessKeyAsync("INV-789");

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Stage, Is.EqualTo("Transform"));
        Assert.That(result[1].Status, Is.EqualTo(DeliveryStatus.Delivered));
    }

    // ── InspectionResult.Found is False When No Events Found ────────────────

    [Test]
    public async Task InspectionResult_Found_IsFalse_WhenNoEventsFound()
    {
        var log = Substitute.For<IObservabilityEventLog>();
        log.GetByBusinessKeyAsync("MISSING-KEY", Arg.Any<CancellationToken>())
            .Returns(new List<MessageEvent>());

        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();

        var inspector = new MessageStateInspector(
            log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var result = await inspector.WhereIsAsync("MISSING-KEY");

        Assert.That(result.Found, Is.False);
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.Summary, Does.Contain("No messages found"));
    }
}
