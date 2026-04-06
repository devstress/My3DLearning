// ============================================================================
// Tutorial 06 – Messaging Channels (Lab)
// ============================================================================
// This lab explores the core channel types from Enterprise Integration Patterns:
// Point-to-Point, Publish-Subscribe, Datatype Channel, and Invalid Message
// Channel.  You will use mocked producers and consumers to exercise each
// pattern and verify the behaviour.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial06;

[TestFixture]
public sealed class Lab
{
    // ── Point-to-Point Channel ──────────────────────────────────────────────

    [Test]
    public async Task PointToPoint_PublishToTopic_SingleConsumerReceives()
    {
        // In a Point-to-Point channel, only ONE consumer in a group receives
        // the message.  We mock the producer and verify a single publish call.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var envelope = IntegrationEnvelope<string>.Create(
            payload: "order-123",
            source: "OrderService",
            messageType: "order.created") with
        {
            Intent = MessageIntent.Command,
        };

        await producer.PublishAsync(envelope, "orders.point-to-point");

        // Exactly one publish to the target topic.
        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "order-123"),
            Arg.Is("orders.point-to-point"),
            Arg.Any<CancellationToken>());
    }

    // ── Publish-Subscribe Channel ───────────────────────────────────────────

    [Test]
    public async Task PubSub_MultipleConsumerGroups_EachGroupReceivesCopy()
    {
        // In Publish-Subscribe, EVERY subscriber group gets a copy.
        // We simulate three independent consumer groups subscribing to the same topic.
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var envelope = IntegrationEnvelope<string>.Create(
            "event-data", "EventService", "event.published") with
        {
            Intent = MessageIntent.Event,
        };

        // Three subscriber groups each get the same message.
        var groups = new[] { "billing-group", "analytics-group", "notifications-group" };

        foreach (var group in groups)
        {
            await consumer.SubscribeAsync<string>(
                "events.pubsub", group, _ => Task.CompletedTask);
        }

        // Publish the message.
        await producer.PublishAsync(envelope, "events.pubsub");

        // Verify all three groups subscribed independently.
        await consumer.Received(3).SubscribeAsync<string>(
            Arg.Is("events.pubsub"),
            Arg.Any<string>(),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());

        // Each group was subscribed exactly once.
        foreach (var group in groups)
        {
            await consumer.Received(1).SubscribeAsync<string>(
                Arg.Is("events.pubsub"),
                Arg.Is(group),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>());
        }
    }

    // ── Datatype Channel ────────────────────────────────────────────────────

    [Test]
    public async Task DatatypeChannel_DifferentTypes_RouteToSeparateTopics()
    {
        // A Datatype Channel routes each MessageType to its own dedicated topic,
        // ensuring consumers only see messages of the type they expect.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var orderEnvelope = IntegrationEnvelope<string>.Create(
            "new-order", "OrderService", "order.created");

        var paymentEnvelope = IntegrationEnvelope<string>.Create(
            "payment-received", "PaymentService", "payment.completed");

        var inventoryEnvelope = IntegrationEnvelope<string>.Create(
            "stock-updated", "InventoryService", "inventory.adjusted");

        // Each message type publishes to its own type-specific topic.
        await producer.PublishAsync(orderEnvelope, "datatype.order.created");
        await producer.PublishAsync(paymentEnvelope, "datatype.payment.completed");
        await producer.PublishAsync(inventoryEnvelope, "datatype.inventory.adjusted");

        // Verify three distinct topics received messages.
        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("datatype.order.created"),
            Arg.Any<CancellationToken>());

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("datatype.payment.completed"),
            Arg.Any<CancellationToken>());

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("datatype.inventory.adjusted"),
            Arg.Any<CancellationToken>());
    }

    // ── Invalid Message Channel (Expired Messages) ──────────────────────────

    [Test]
    public void InvalidMessageChannel_ExpiredEnvelope_IsExpiredReturnsTrue()
    {
        // An expired message should be routed to the Invalid Message Channel.
        // We verify the IsExpired property on an envelope with a past ExpiresAt.
        var expired = IntegrationEnvelope<string>.Create(
            "stale-data", "LegacySystem", "legacy.update") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };

        // The platform uses IsExpired to detect stale messages.
        Assert.That(expired.IsExpired, Is.True,
            "Envelope with ExpiresAt in the past should be expired");
    }

    [Test]
    public void InvalidMessageChannel_FutureExpiry_IsExpiredReturnsFalse()
    {
        // A message with a future ExpiresAt is still valid.
        var valid = IntegrationEnvelope<string>.Create(
            "fresh-data", "ModernSystem", "modern.update") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        Assert.That(valid.IsExpired, Is.False,
            "Envelope with ExpiresAt in the future should NOT be expired");
    }

    [Test]
    public void InvalidMessageChannel_NoExpiry_IsNeverExpired()
    {
        // A message without an ExpiresAt never expires.
        var noExpiry = IntegrationEnvelope<string>.Create(
            "persistent-data", "CoreService", "core.event");

        Assert.That(noExpiry.ExpiresAt, Is.Null);
        Assert.That(noExpiry.IsExpired, Is.False,
            "Envelope without ExpiresAt should never be expired");
    }
}
