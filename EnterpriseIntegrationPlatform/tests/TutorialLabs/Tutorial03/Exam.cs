// ============================================================================
// Tutorial 03 – Your First Message (Exam)
// ============================================================================
// Coding challenges covering publish/consume round trips, batch correlation,
// and consumer group patterns using mocked brokers.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial03;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Publish / Consume Round Trip ───────────────────────────

    [Test]
    public async Task Challenge1_PublishAndConsume_RoundTrip()
    {
        // Publish a message through a mocked producer, then simulate
        // delivery to a consumer handler and verify the payload survives.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();

        var payload = new OrderPayload("ORD-RT-1", "RoundTripWidget", 7);
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            payload, "OrderService", "order.created");

        // Publish
        await producer.PublishAsync(envelope, "orders");

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<OrderPayload>>(),
            Arg.Is("orders"),
            Arg.Any<CancellationToken>());

        // Consume: simulate the broker delivering the same envelope.
        IntegrationEnvelope<OrderPayload>? consumed = null;

        await consumer.SubscribeAsync<OrderPayload>(
            "orders",
            "order-processors",
            handler: msg =>
            {
                consumed = msg;
                return Task.CompletedTask;
            });

        // Manually invoke the handler to simulate message delivery.
        // In a real system the broker calls the handler; here we do it ourselves.
        Func<IntegrationEnvelope<OrderPayload>, Task> handler = msg =>
        {
            consumed = msg;
            return Task.CompletedTask;
        };
        await handler(envelope);

        Assert.That(consumed, Is.Not.Null);
        Assert.That(consumed!.Payload.OrderId, Is.EqualTo("ORD-RT-1"));
        Assert.That(consumed.MessageId, Is.EqualTo(envelope.MessageId));
    }

    // ── Challenge 2: Batch Correlation ──────────────────────────────────────

    [Test]
    public async Task Challenge2_MultipleEnvelopes_ShareCorrelationId()
    {
        // In a batch scenario, all messages in the batch share the same
        // CorrelationId so they can be traced as a single logical unit.
        var batchCorrelationId = Guid.NewGuid();
        var producer = Substitute.For<IMessageBrokerProducer>();

        var items = new[] { "Item-A", "Item-B", "Item-C" };
        var envelopes = items.Select(item =>
            IntegrationEnvelope<string>.Create(
                payload: item,
                source: "BatchService",
                messageType: "batch.item",
                correlationId: batchCorrelationId))
            .ToList();

        // Publish all batch items.
        foreach (var env in envelopes)
        {
            await producer.PublishAsync(env, "batch-topic");
        }

        // Verify all share the same CorrelationId.
        Assert.That(envelopes, Has.Count.EqualTo(3));
        Assert.That(envelopes.Select(e => e.CorrelationId).Distinct().Count(), Is.EqualTo(1));
        Assert.That(envelopes[0].CorrelationId, Is.EqualTo(batchCorrelationId));

        // Each message still has a unique MessageId.
        Assert.That(envelopes.Select(e => e.MessageId).Distinct().Count(), Is.EqualTo(3));

        // Verify the producer was called three times.
        await producer.Received(3).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("batch-topic"),
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 3: Consumer Group Patterns ────────────────────────────────

    [Test]
    public async Task Challenge3_CompetingConsumers_SameGroupName()
    {
        // Competing Consumers: multiple consumers in the SAME group.
        // Each message is delivered to exactly ONE consumer in the group.
        var consumer1 = Substitute.For<IMessageBrokerConsumer>();
        var consumer2 = Substitute.For<IMessageBrokerConsumer>();

        const string sharedGroup = "order-processors";

        await consumer1.SubscribeAsync<string>(
            "orders", sharedGroup, _ => Task.CompletedTask);

        await consumer2.SubscribeAsync<string>(
            "orders", sharedGroup, _ => Task.CompletedTask);

        // Both consumers subscribed to the same topic with the same group.
        await consumer1.Received(1).SubscribeAsync<string>(
            Arg.Is("orders"),
            Arg.Is(sharedGroup),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());

        await consumer2.Received(1).SubscribeAsync<string>(
            Arg.Is("orders"),
            Arg.Is(sharedGroup),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Challenge3_PublishSubscribe_DifferentGroupNames()
    {
        // Publish-Subscribe: multiple consumers in DIFFERENT groups.
        // Each message is delivered to ALL groups (fan-out).
        var analyticsConsumer = Substitute.For<IMessageBrokerConsumer>();
        var notificationConsumer = Substitute.For<IMessageBrokerConsumer>();

        await analyticsConsumer.SubscribeAsync<string>(
            "orders", "analytics-group", _ => Task.CompletedTask);

        await notificationConsumer.SubscribeAsync<string>(
            "orders", "notification-group", _ => Task.CompletedTask);

        // Verify different groups — each group gets its own copy of the message.
        await analyticsConsumer.Received(1).SubscribeAsync<string>(
            Arg.Is("orders"),
            Arg.Is("analytics-group"),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());

        await notificationConsumer.Received(1).SubscribeAsync<string>(
            Arg.Is("orders"),
            Arg.Is("notification-group"),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());
    }
}
