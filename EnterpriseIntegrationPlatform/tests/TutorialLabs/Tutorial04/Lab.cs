// ============================================================================
// Tutorial 04 – The Integration Envelope (Lab)
// ============================================================================
// A deep dive into every property of IntegrationEnvelope<T>.  You will test
// auto-generated identifiers, message expiration, metadata headers, sequence
// numbers, and the immutable record semantics that make envelopes safe to
// pass across service boundaries.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace TutorialLabs.Tutorial04;

// A rich domain payload to exercise complex envelope scenarios.
public sealed record ShipmentPayload(
    string ShipmentId,
    string Carrier,
    decimal WeightKg,
    string[] Items);

[TestFixture]
public sealed class Lab
{
    // ── All Properties with a Complex Payload ───────────────────────────────

    [Test]
    public void Envelope_WithComplexPayload_AllPropertiesAccessible()
    {
        var items = new[] { "SKU-001", "SKU-002" };
        var shipment = new ShipmentPayload("SHIP-1", "FedEx", 12.5m, items);
        var correlationId = Guid.NewGuid();

        var envelope = IntegrationEnvelope<ShipmentPayload>.Create(
            payload: shipment,
            source: "WarehouseService",
            messageType: "shipment.dispatched",
            correlationId: correlationId) with
        {
            SchemaVersion = "2.0",
            Priority = MessagePriority.High,
            Intent = MessageIntent.Event,
            ReplyTo = "shipment-replies",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            SequenceNumber = 0,
            TotalCount = 3,
        };

        Assert.That(envelope.Payload.ShipmentId, Is.EqualTo("SHIP-1"));
        Assert.That(envelope.Payload.Carrier, Is.EqualTo("FedEx"));
        Assert.That(envelope.Payload.WeightKg, Is.EqualTo(12.5m));
        Assert.That(envelope.Payload.Items, Has.Length.EqualTo(2));
        Assert.That(envelope.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(envelope.SchemaVersion, Is.EqualTo("2.0"));
        Assert.That(envelope.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(envelope.Intent, Is.EqualTo(MessageIntent.Event));
        Assert.That(envelope.ReplyTo, Is.EqualTo("shipment-replies"));
        Assert.That(envelope.ExpiresAt, Is.Not.Null);
        Assert.That(envelope.SequenceNumber, Is.EqualTo(0));
        Assert.That(envelope.TotalCount, Is.EqualTo(3));
    }

    // ── Unique MessageId Generation ─────────────────────────────────────────

    [Test]
    public void Create_GeneratesUniqueMessageIds()
    {
        var ids = Enumerable.Range(0, 100)
            .Select(_ => IntegrationEnvelope<string>.Create(
                "payload", "source", "type").MessageId)
            .ToList();

        Assert.That(ids.Distinct().Count(), Is.EqualTo(100),
            "Each envelope must have a globally unique MessageId");
    }

    [Test]
    public void Create_WithoutCorrelationId_GeneratesNewOne()
    {
        var env1 = IntegrationEnvelope<string>.Create("a", "src", "type");
        var env2 = IntegrationEnvelope<string>.Create("b", "src", "type");

        Assert.That(env1.CorrelationId, Is.Not.EqualTo(env2.CorrelationId));
    }

    // ── IsExpired ───────────────────────────────────────────────────────────

    [Test]
    public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "stale", "source", "type") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };

        Assert.That(envelope.IsExpired, Is.True);
    }

    [Test]
    public void IsExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "fresh", "source", "type") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        Assert.That(envelope.IsExpired, Is.False);
    }

    [Test]
    public void IsExpired_WhenExpiresAtIsNull_ReturnsFalse()
    {
        // Messages without an ExpiresAt never expire.
        var envelope = IntegrationEnvelope<string>.Create(
            "immortal", "source", "type");

        Assert.That(envelope.ExpiresAt, Is.Null);
        Assert.That(envelope.IsExpired, Is.False);
    }

    // ── Metadata Dictionary ─────────────────────────────────────────────────

    [Test]
    public void Metadata_AddAndReadHeaders()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "source", "type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ContentType] = "application/json",
                [MessageHeaders.TraceId] = "abc-123-trace",
                [MessageHeaders.SourceTopic] = "orders-topic",
            },
        };

        Assert.That(envelope.Metadata[MessageHeaders.ContentType],
            Is.EqualTo("application/json"));
        Assert.That(envelope.Metadata[MessageHeaders.TraceId],
            Is.EqualTo("abc-123-trace"));
        Assert.That(envelope.Metadata[MessageHeaders.SourceTopic],
            Is.EqualTo("orders-topic"));
        Assert.That(envelope.Metadata, Has.Count.EqualTo(3));
    }

    [Test]
    public void Metadata_DefaultIsEmptyDictionary()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "source", "type");

        Assert.That(envelope.Metadata, Is.Not.Null);
        Assert.That(envelope.Metadata, Is.Empty);
    }

    // ── SequenceNumber and TotalCount ───────────────────────────────────────

    [Test]
    public void SplitMessage_SequenceNumbers_AreCorrect()
    {
        // Simulate a Splitter that breaks a large order into three parts.
        var correlationId = Guid.NewGuid();
        var parts = Enumerable.Range(0, 3)
            .Select(i => IntegrationEnvelope<string>.Create(
                payload: $"Part-{i}",
                source: "Splitter",
                messageType: "order.part",
                correlationId: correlationId) with
            {
                SequenceNumber = i,
                TotalCount = 3,
            })
            .ToList();

        Assert.That(parts, Has.Count.EqualTo(3));

        for (var i = 0; i < 3; i++)
        {
            Assert.That(parts[i].SequenceNumber, Is.EqualTo(i));
            Assert.That(parts[i].TotalCount, Is.EqualTo(3));
            Assert.That(parts[i].CorrelationId, Is.EqualTo(correlationId));
        }
    }
}
