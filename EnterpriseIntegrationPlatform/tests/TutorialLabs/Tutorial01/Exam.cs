// ============================================================================
// Tutorial 01 – Introduction to Enterprise Integration (Exam)
// ============================================================================
// Coding challenges that test your understanding of the IntegrationEnvelope,
// message intents, causation chains, and record immutability.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace TutorialLabs.Tutorial01;

// A simple domain record used in the exam challenges.
public sealed record OrderPayload(string OrderId, string Product, int Quantity, decimal Price);

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Wrap a Domain Object in an Envelope ────────────────────

    [Test]
    public void Challenge1_CreateEnvelopeForOrderPayload()
    {
        // Create an OrderPayload and wrap it in an IntegrationEnvelope.
        var order = new OrderPayload("ORD-001", "Widget", 5, 29.99m);

        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            payload: order,
            source: "OrderService",
            messageType: "order.created");

        // Verify the envelope wraps the domain object correctly.
        Assert.That(envelope.Payload.OrderId, Is.EqualTo("ORD-001"));
        Assert.That(envelope.Payload.Product, Is.EqualTo("Widget"));
        Assert.That(envelope.Payload.Quantity, Is.EqualTo(5));
        Assert.That(envelope.Payload.Price, Is.EqualTo(29.99m));
        Assert.That(envelope.Source, Is.EqualTo("OrderService"));
        Assert.That(envelope.MessageType, Is.EqualTo("order.created"));
        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
    }

    // ── Challenge 2: Build a CausationId Chain ──────────────────────────────

    [Test]
    public void Challenge2_CausationIdLinking_MessageBCausedByA()
    {
        // Message A is the originating command.
        var messageA = IntegrationEnvelope<string>.Create(
            payload: "PlaceOrder",
            source: "WebApp",
            messageType: "order.place") with
        {
            Intent = MessageIntent.Command,
        };

        // Message B is caused by A — its CausationId points to A's MessageId
        // and both share the same CorrelationId for end-to-end tracing.
        var messageB = IntegrationEnvelope<string>.Create(
            payload: "OrderPlaced",
            source: "OrderService",
            messageType: "order.placed",
            correlationId: messageA.CorrelationId,
            causationId: messageA.MessageId) with
        {
            Intent = MessageIntent.Event,
        };

        // Verify the causal link.
        Assert.That(messageB.CausationId, Is.EqualTo(messageA.MessageId));
        Assert.That(messageB.CorrelationId, Is.EqualTo(messageA.CorrelationId));
        Assert.That(messageB.MessageId, Is.Not.EqualTo(messageA.MessageId));
    }

    // ── Challenge 3: Verify Envelope Immutability ───────────────────────────

    [Test]
    public void Challenge3_RecordImmutability_WithExpressionCreatesNewInstance()
    {
        // Records are immutable — you cannot change properties after creation.
        // The `with` expression creates a shallow copy with modified values.
        var original = IntegrationEnvelope<string>.Create(
            "original-payload", "TestService", "test.message");

        var modified = original with { Priority = MessagePriority.High };

        // The original is untouched.
        Assert.That(original.Priority, Is.EqualTo(MessagePriority.Normal));

        // The modified copy has the new priority but retains all other values.
        Assert.That(modified.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(modified.MessageId, Is.EqualTo(original.MessageId));
        Assert.That(modified.Payload, Is.EqualTo(original.Payload));

        // They are different object references.
        Assert.That(ReferenceEquals(original, modified), Is.False);
    }
}
