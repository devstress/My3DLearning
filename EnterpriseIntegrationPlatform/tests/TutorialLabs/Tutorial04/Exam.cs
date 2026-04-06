// ============================================================================
// Tutorial 04 – The Integration Envelope (Exam)
// ============================================================================
// Coding challenges: populate full metadata, build a multi-hop causation
// chain, and round-trip an envelope through JSON serialization.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace TutorialLabs.Tutorial04;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Envelope with Full Metadata ────────────────────────────

    [Test]
    public void Challenge1_FullMetadata_AllHeaderConstants()
    {
        // Populate an envelope's Metadata dictionary with every
        // MessageHeaders constant that has a sensible string value.
        var envelope = IntegrationEnvelope<string>.Create(
            "full-metadata-payload", "MetadataService", "metadata.test") with
        {
            Priority = MessagePriority.High,
            Intent = MessageIntent.Command,
            ReplyTo = "reply-topic",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            SequenceNumber = 0,
            TotalCount = 1,
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.TraceId] = "trace-001",
                [MessageHeaders.SpanId] = "span-001",
                [MessageHeaders.ContentType] = "application/json",
                [MessageHeaders.SchemaVersion] = "1.0",
                [MessageHeaders.SourceTopic] = "commands-topic",
                [MessageHeaders.ConsumerGroup] = "cmd-processors",
                [MessageHeaders.LastAttemptAt] = DateTimeOffset.UtcNow.ToString("O"),
                [MessageHeaders.RetryCount] = "0",
                [MessageHeaders.ReplyTo] = "reply-topic",
                [MessageHeaders.ExpiresAt] = DateTimeOffset.UtcNow.AddMinutes(30).ToString("O"),
                [MessageHeaders.SequenceNumber] = "0",
                [MessageHeaders.TotalCount] = "1",
                [MessageHeaders.Intent] = "Command",
                [MessageHeaders.MessageHistory] = "[]",
                [MessageHeaders.ReplayId] = Guid.NewGuid().ToString(),
            },
        };

        // Verify all 15 metadata entries are present.
        Assert.That(envelope.Metadata, Has.Count.EqualTo(15));
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.TraceId), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.SpanId), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.ContentType), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.SchemaVersion), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.SourceTopic), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.ConsumerGroup), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.LastAttemptAt), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.RetryCount), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.ReplyTo), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.ExpiresAt), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.SequenceNumber), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.TotalCount), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.Intent), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.MessageHistory), Is.True);
        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.ReplayId), Is.True);
    }

    // ── Challenge 2: Multi-Hop Causation Chain ──────────────────────────────

    [Test]
    public void Challenge2_CausationChain_A_CausesB_CausesC()
    {
        // Envelope A: the originating command.
        var envelopeA = IntegrationEnvelope<string>.Create(
            payload: "PlaceOrder",
            source: "WebApp",
            messageType: "order.place") with
        {
            Intent = MessageIntent.Command,
        };

        // Envelope B: caused by A (order placed event).
        var envelopeB = IntegrationEnvelope<string>.Create(
            payload: "OrderPlaced",
            source: "OrderService",
            messageType: "order.placed",
            correlationId: envelopeA.CorrelationId,
            causationId: envelopeA.MessageId) with
        {
            Intent = MessageIntent.Event,
        };

        // Envelope C: caused by B (invoice generated).
        var envelopeC = IntegrationEnvelope<string>.Create(
            payload: "InvoiceGenerated",
            source: "BillingService",
            messageType: "invoice.generated",
            correlationId: envelopeA.CorrelationId,
            causationId: envelopeB.MessageId) with
        {
            Intent = MessageIntent.Document,
        };

        // All three share the same CorrelationId for end-to-end tracing.
        Assert.That(envelopeB.CorrelationId, Is.EqualTo(envelopeA.CorrelationId));
        Assert.That(envelopeC.CorrelationId, Is.EqualTo(envelopeA.CorrelationId));

        // The causation chain links: A → B → C.
        Assert.That(envelopeA.CausationId, Is.Null, "A has no parent");
        Assert.That(envelopeB.CausationId, Is.EqualTo(envelopeA.MessageId));
        Assert.That(envelopeC.CausationId, Is.EqualTo(envelopeB.MessageId));

        // Each has a unique MessageId.
        var ids = new[] { envelopeA.MessageId, envelopeB.MessageId, envelopeC.MessageId };
        Assert.That(ids.Distinct().Count(), Is.EqualTo(3));
    }

    // ── Challenge 3: JSON Serialization Round-Trip ──────────────────────────

    [Test]
    public void Challenge3_JsonSerialization_RoundTrip()
    {
        var original = IntegrationEnvelope<string>.Create(
            payload: "serialize-me",
            source: "SerializerService",
            messageType: "test.serialize") with
        {
            SchemaVersion = "2.0",
            Priority = MessagePriority.Critical,
            Intent = MessageIntent.Event,
            ReplyTo = "reply-channel",
            ExpiresAt = DateTimeOffset.Parse("2099-12-31T23:59:59+00:00"),
            SequenceNumber = 5,
            TotalCount = 10,
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ContentType] = "application/json",
                [MessageHeaders.TraceId] = "trace-xyz",
            },
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        // Serialize to JSON.
        var json = JsonSerializer.Serialize(original, options);
        Assert.That(json, Is.Not.Null.And.Not.Empty);

        // Deserialize back.
        var restored = JsonSerializer.Deserialize<IntegrationEnvelope<string>>(json, options);
        Assert.That(restored, Is.Not.Null);

        // Verify all fields survived the round-trip.
        Assert.That(restored!.MessageId, Is.EqualTo(original.MessageId));
        Assert.That(restored.CorrelationId, Is.EqualTo(original.CorrelationId));
        Assert.That(restored.CausationId, Is.EqualTo(original.CausationId));
        Assert.That(restored.Source, Is.EqualTo(original.Source));
        Assert.That(restored.MessageType, Is.EqualTo(original.MessageType));
        Assert.That(restored.SchemaVersion, Is.EqualTo("2.0"));
        Assert.That(restored.Priority, Is.EqualTo(MessagePriority.Critical));
        Assert.That(restored.Intent, Is.EqualTo(MessageIntent.Event));
        Assert.That(restored.Payload, Is.EqualTo("serialize-me"));
        Assert.That(restored.ReplyTo, Is.EqualTo("reply-channel"));
        Assert.That(restored.SequenceNumber, Is.EqualTo(5));
        Assert.That(restored.TotalCount, Is.EqualTo(10));
        Assert.That(restored.Metadata[MessageHeaders.ContentType],
            Is.EqualTo("application/json"));
        Assert.That(restored.Metadata[MessageHeaders.TraceId],
            Is.EqualTo("trace-xyz"));
    }
}
