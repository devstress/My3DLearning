// ============================================================================
// Tutorial 01 – Introduction to Enterprise Integration (Lab)
// ============================================================================
// This lab introduces the foundational concepts of Enterprise Integration
// Patterns (EIP) and maps them to the platform's canonical types.  You will
// create IntegrationEnvelopes using the static factory method, inspect
// auto-generated fields, and explore the three message intents.
// ============================================================================

using System.Reflection;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NUnit.Framework;

namespace TutorialLabs.Tutorial01;

[TestFixture]
public sealed class Lab
{
    // ── Creating an Envelope with the Factory Method ────────────────────────

    [Test]
    public void Create_WithStringPayload_PopulatesAllRequiredFields()
    {
        // The static factory generates MessageId, CorrelationId, and Timestamp
        // automatically, so you only supply the business-relevant arguments.
        var envelope = IntegrationEnvelope<string>.Create(
            payload: "Hello, EIP!",
            source: "Tutorial01",
            messageType: "greeting.created");

        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.Timestamp, Is.Not.EqualTo(default(DateTimeOffset)));
        Assert.That(envelope.Source, Is.EqualTo("Tutorial01"));
        Assert.That(envelope.MessageType, Is.EqualTo("greeting.created"));
        Assert.That(envelope.Payload, Is.EqualTo("Hello, EIP!"));
    }

    [Test]
    public void Create_DefaultValues_AreReasonable()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "source", "type");

        // Defaults defined on the record
        Assert.That(envelope.SchemaVersion, Is.EqualTo("1.0"));
        Assert.That(envelope.Priority, Is.EqualTo(MessagePriority.Normal));
        Assert.That(envelope.CausationId, Is.Null);
        Assert.That(envelope.ReplyTo, Is.Null);
        Assert.That(envelope.ExpiresAt, Is.Null);
        Assert.That(envelope.SequenceNumber, Is.Null);
        Assert.That(envelope.TotalCount, Is.Null);
        Assert.That(envelope.Intent, Is.Null);
        Assert.That(envelope.Metadata, Is.Empty);
    }

    [Test]
    public void Create_TimestampIsUtcAndRecent()
    {
        var before = DateTimeOffset.UtcNow;
        var envelope = IntegrationEnvelope<int>.Create(42, "lab", "number");
        var after = DateTimeOffset.UtcNow;

        Assert.That(envelope.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(envelope.Timestamp, Is.LessThanOrEqualTo(after));
    }

    // ── Message Intents ─────────────────────────────────────────────────────

    [Test]
    public void CommandIntent_RepresentsAnActionRequest()
    {
        // A Command Message tells the receiver to DO something.
        var command = IntegrationEnvelope<string>.Create(
            "PlaceOrder", "OrderService", "order.place") with
        {
            Intent = MessageIntent.Command,
        };

        Assert.That(command.Intent, Is.EqualTo(MessageIntent.Command));
    }

    [Test]
    public void DocumentIntent_RepresentsDataTransfer()
    {
        // A Document Message carries data for the receiver to process.
        var document = IntegrationEnvelope<string>.Create(
            "{\"sku\":\"ABC\"}", "CatalogService", "product.catalog") with
        {
            Intent = MessageIntent.Document,
        };

        Assert.That(document.Intent, Is.EqualTo(MessageIntent.Document));
    }

    [Test]
    public void EventIntent_RepresentsNotification()
    {
        // An Event Message notifies that something has already happened.
        var evt = IntegrationEnvelope<string>.Create(
            "OrderPlaced", "OrderService", "order.placed") with
        {
            Intent = MessageIntent.Event,
        };

        Assert.That(evt.Intent, Is.EqualTo(MessageIntent.Event));
    }

    // ── Mapping EIP Patterns to Platform Types ──────────────────────────────

    [Test]
    public void PlatformTypes_MessageChannel_ProducerInterfaceExists()
    {
        // EIP: Message Channel → IMessageBrokerProducer
        var producerType = typeof(IMessageBrokerProducer);
        Assert.That(producerType, Is.Not.Null);
        Assert.That(producerType.IsInterface, Is.True);

        var publishMethod = producerType.GetMethod("PublishAsync");
        Assert.That(publishMethod, Is.Not.Null, "PublishAsync method must exist");
    }

    [Test]
    public void PlatformTypes_MessageEndpoint_ConsumerInterfaceExists()
    {
        // EIP: Message Endpoint → IMessageBrokerConsumer
        var consumerType = typeof(IMessageBrokerConsumer);
        Assert.That(consumerType, Is.Not.Null);
        Assert.That(consumerType.IsInterface, Is.True);

        var subscribeMethod = consumerType.GetMethod("SubscribeAsync");
        Assert.That(subscribeMethod, Is.Not.Null, "SubscribeAsync method must exist");
    }

    [Test]
    public void PlatformTypes_CanonicalDataModel_EnvelopeIsRecord()
    {
        // EIP: Canonical Data Model → IntegrationEnvelope<T>
        // Records are classes with value-equality semantics.
        var envelopeType = typeof(IntegrationEnvelope<string>);
        Assert.That(envelopeType.IsClass, Is.True);

        // Records implement IEquatable<T>
        var equatable = typeof(IEquatable<IntegrationEnvelope<string>>);
        Assert.That(equatable.IsAssignableFrom(envelopeType), Is.True);
    }
}
