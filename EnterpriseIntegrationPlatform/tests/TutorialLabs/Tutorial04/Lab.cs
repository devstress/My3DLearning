// ============================================================================
// Tutorial 04 – Integration Envelope (Lab)
// ============================================================================
// EIP Pattern: Envelope Wrapper, Fault Message
// End-to-End: Record immutability (`with` expressions), FaultEnvelope
// creation from failed messages, MessageHistoryEntry for processing audits,
// all wrapper fields preserved through PointToPointChannel, and complex
// payloads with complete metadata.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial04;

public sealed record ShipmentPayload(
    string ShipmentId, string Carrier, decimal WeightKg, string[] Items);

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("output");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    // ── 1. Record Immutability & `with` Expressions ─────────────────────

    [Test]
    public void Envelope_WithExpression_CreatesNewInstanceOriginalUnchanged()
    {
        // IntegrationEnvelope is a C# record — immutable by design.
        // The `with` expression creates a shallow copy with overridden fields.
        // The original envelope is never modified.
        var original = IntegrationEnvelope<string>.Create(
            "original-payload", "OrigService", "event.created");

        var modified = original with
        {
            Priority = MessagePriority.Critical,
            Intent = MessageIntent.Command,
            SchemaVersion = "2.0",
        };

        // Original is unchanged
        Assert.That(original.Priority, Is.EqualTo(MessagePriority.Normal));
        Assert.That(original.Intent, Is.Null);
        Assert.That(original.SchemaVersion, Is.EqualTo("1.0"));

        // Modified has overridden fields, but same identity
        Assert.That(modified.Priority, Is.EqualTo(MessagePriority.Critical));
        Assert.That(modified.Intent, Is.EqualTo(MessageIntent.Command));
        Assert.That(modified.MessageId, Is.EqualTo(original.MessageId));
    }

    [Test]
    public void FaultEnvelope_CreateFromFailedMessage_PreservesCorrelation()
    {
        // FaultEnvelope captures a failed message's identity for dead-letter
        // routing and later replay. The factory carries over CorrelationId,
        // MessageId, and MessageType from the original envelope.
        var original = IntegrationEnvelope<string>.Create(
            "bad-payload", "IngestService", "order.created");

        var fault = FaultEnvelope.Create(
            original, faultedBy: "ValidationService",
            reason: "Schema validation failed", retryCount: 3);

        Assert.That(fault.FaultId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(fault.OriginalMessageId, Is.EqualTo(original.MessageId));
        Assert.That(fault.CorrelationId, Is.EqualTo(original.CorrelationId));
        Assert.That(fault.OriginalMessageType, Is.EqualTo("order.created"));
        Assert.That(fault.FaultedBy, Is.EqualTo("ValidationService"));
        Assert.That(fault.FaultReason, Is.EqualTo("Schema validation failed"));
        Assert.That(fault.RetryCount, Is.EqualTo(3));
        Assert.That(fault.FaultedAt, Is.GreaterThan(DateTimeOffset.MinValue));
    }

    [Test]
    public void FaultEnvelope_WithException_CapturesErrorDetails()
    {
        // When a processing exception causes the fault, FaultEnvelope captures
        // the full exception type, message, and stack trace for diagnostics.
        var original = IntegrationEnvelope<string>.Create(
            "crash-payload", "IngestService", "payment.process");

        var exception = new InvalidOperationException("Insufficient funds");
        var fault = FaultEnvelope.Create(
            original, "PaymentService", "Processing failed", 1, exception);

        Assert.That(fault.ErrorDetails, Does.Contain("InvalidOperationException"));
        Assert.That(fault.ErrorDetails, Does.Contain("Insufficient funds"));
    }

    // ── 2. Message History & Audit Trail ─────────────────────────────────

    [Test]
    public void MessageHistoryEntry_RecordsProcessingSteps()
    {
        // MessageHistoryEntry tracks each processing step a message passes
        // through — the Message History EIP pattern for full audit trails.
        var entries = new List<MessageHistoryEntry>
        {
            new("Ingestion", DateTimeOffset.UtcNow, MessageHistoryStatus.Completed),
            new("Validation", DateTimeOffset.UtcNow, MessageHistoryStatus.Completed, "Schema OK"),
            new("Enrichment", DateTimeOffset.UtcNow, MessageHistoryStatus.Skipped, "No enrichment rules"),
            new("Delivery", DateTimeOffset.UtcNow, MessageHistoryStatus.Failed, "Timeout"),
        };

        Assert.That(entries, Has.Count.EqualTo(4));
        Assert.That(entries[0].Status, Is.EqualTo(MessageHistoryStatus.Completed));
        Assert.That(entries[2].Status, Is.EqualTo(MessageHistoryStatus.Skipped));
        Assert.That(entries[3].Status, Is.EqualTo(MessageHistoryStatus.Failed));
        Assert.That(entries[3].Detail, Is.EqualTo("Timeout"));
    }

    // ── 3. Envelope Fields End-to-End Through Channel ───────────────────

    [Test]
    public async Task Envelope_ExpiresAt_SurvivedChannelDelivery()
    {
        // ExpiresAt + IsExpired implement the Message Expiration pattern.
        // The channel preserves the timestamp for downstream consumers
        // to check and dead-letter expired messages.
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);

        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var envelope = IntegrationEnvelope<string>.Create(
            "expiring", "source", "type") with { ExpiresAt = expiry };

        await channel.SendAsync(envelope, "topic", CancellationToken.None);

        var received = _output.GetReceived<string>();
        Assert.That(received.ExpiresAt, Is.EqualTo(expiry));
        Assert.That(received.IsExpired, Is.False);
    }

    [Test]
    public async Task Envelope_ReplyTo_RequestReplyPatternThroughChannel()
    {
        // ReplyTo carries the Return Address — the topic where the sender
        // expects replies. This enables the Request-Reply EIP pattern.
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "get-price", "PricingClient", "price.request") with
        {
            ReplyTo = "pricing-replies",
            Intent = MessageIntent.Command,
        };

        await channel.SendAsync(envelope, "pricing-requests", CancellationToken.None);

        var received = _output.GetReceived<string>();
        Assert.That(received.ReplyTo, Is.EqualTo("pricing-replies"));
        Assert.That(received.Intent, Is.EqualTo(MessageIntent.Command));
    }

    [Test]
    public async Task Envelope_SplitSequence_ThroughChannel()
    {
        // SequenceNumber + TotalCount track position within a split batch.
        // All parts share the same CorrelationId for reassembly.
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);
        var correlationId = Guid.NewGuid();

        for (var i = 0; i < 3; i++)
        {
            var part = IntegrationEnvelope<string>.Create(
                $"chunk-{i}", "Splitter", "order.part",
                correlationId: correlationId) with
            {
                SequenceNumber = i,
                TotalCount = 3,
            };
            await channel.SendAsync(part, "parts", CancellationToken.None);
        }

        _output.AssertReceivedCount(3);
        var all = _output.GetAllReceived<string>("parts");
        Assert.That(all[0].SequenceNumber, Is.EqualTo(0));
        Assert.That(all[2].SequenceNumber, Is.EqualTo(2));
        Assert.That(all.Select(m => m.CorrelationId).Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Envelope_MetadataHeaders_WellKnownConstants()
    {
        // MessageHeaders provides well-known keys for the Metadata dictionary.
        // Using constants prevents typos and ensures cross-service consistency.
        var envelope = IntegrationEnvelope<string>.Create(
            "traced", "source", "type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ContentType] = "application/json",
                [MessageHeaders.TraceId] = "abc-123",
                [MessageHeaders.SpanId] = "span-456",
                [MessageHeaders.RetryCount] = "0",
            },
        };

        await _output.PublishAsync(envelope, "events");

        var received = _output.GetReceived<string>();
        Assert.That(received.Metadata, Has.Count.EqualTo(4));
        Assert.That(received.Metadata[MessageHeaders.ContentType], Is.EqualTo("application/json"));
        Assert.That(received.Metadata[MessageHeaders.TraceId], Is.EqualTo("abc-123"));
    }

    [Test]
    public async Task Envelope_AllFields_ComplexPayloadThroughChannel()
    {
        // A real-world envelope carries every wrapper field simultaneously.
        // The channel preserves the complete envelope without field loss.
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);

        var shipment = new ShipmentPayload("SHIP-1", "FedEx", 12.5m,
            new[] { "SKU-001", "SKU-002" });
        var correlationId = Guid.NewGuid();

        var envelope = IntegrationEnvelope<ShipmentPayload>.Create(
            shipment, "Warehouse", "shipment.dispatched",
            correlationId: correlationId) with
        {
            SchemaVersion = "2.0",
            Priority = MessagePriority.High,
            Intent = MessageIntent.Event,
            ReplyTo = "shipment-replies",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            SequenceNumber = 0,
            TotalCount = 3,
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ContentType] = "application/json",
            },
        };

        await channel.SendAsync(envelope, "shipments", CancellationToken.None);

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<ShipmentPayload>();
        Assert.That(received.Payload.ShipmentId, Is.EqualTo("SHIP-1"));
        Assert.That(received.Payload.Carrier, Is.EqualTo("FedEx"));
        Assert.That(received.SchemaVersion, Is.EqualTo("2.0"));
        Assert.That(received.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(received.Intent, Is.EqualTo(MessageIntent.Event));
        Assert.That(received.ReplyTo, Is.EqualTo("shipment-replies"));
        Assert.That(received.SequenceNumber, Is.EqualTo(0));
        Assert.That(received.TotalCount, Is.EqualTo(3));
        Assert.That(received.CorrelationId, Is.EqualTo(correlationId));
    }
}
