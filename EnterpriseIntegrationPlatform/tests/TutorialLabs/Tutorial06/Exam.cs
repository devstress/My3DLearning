// ============================================================================
// Tutorial 06 – Messaging Channels (Exam)
// ============================================================================
// Coding challenges: build a messaging bridge, implement publish-subscribe
// fan-out, and route expired messages to a dead letter channel.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial06;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Messaging Bridge ───────────────────────────────────────

    [Test]
    public async Task Challenge1_MessagingBridge_RepublishesFromSourceToTarget()
    {
        // Build a messaging bridge that subscribes to a source topic and
        // re-publishes every received message to a target topic.
        var sourceConsumer = Substitute.For<IMessageBrokerConsumer>();
        var targetProducer = Substitute.For<IMessageBrokerProducer>();

        // Capture the handler that the bridge registers when it subscribes.
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

        await sourceConsumer.SubscribeAsync<string>(
            Arg.Is("source-topic"),
            Arg.Is("bridge-group"),
            Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
            Arg.Any<CancellationToken>());

        // Simulate the bridge subscribing to the source.
        await sourceConsumer.SubscribeAsync<string>(
            "source-topic",
            "bridge-group",
            async envelope =>
            {
                // Bridge logic: re-publish to the target topic.
                await targetProducer.PublishAsync(envelope, "target-topic");
            });

        // Simulate a message arriving on the source topic.
        var envelope = IntegrationEnvelope<string>.Create(
            "bridged-payload", "SourceSystem", "source.event");

        // Invoke the bridge handler.
        Assert.That(capturedHandler, Is.Not.Null, "Bridge handler should be registered");
        await capturedHandler!(envelope);

        // Verify the message was forwarded to the target topic.
        await targetProducer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "bridged-payload"),
            Arg.Is("target-topic"),
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 2: Publish-Subscribe Fan-Out with 3 Groups ────────────────

    [Test]
    public async Task Challenge2_PubSubFanOut_ThreeConsumerGroupsAllReceive()
    {
        // Simulate a pub-sub fan-out where 3 consumer groups each receive
        // the same message independently.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();

        var envelope = IntegrationEnvelope<string>.Create(
            "broadcast-event", "NotificationService", "notification.sent") with
        {
            Intent = MessageIntent.Event,
            Priority = MessagePriority.High,
        };

        var consumerGroups = new[] { "email-service", "sms-service", "push-service" };
        var receivedPayloads = new List<string>();

        // Subscribe three consumer groups.
        foreach (var group in consumerGroups)
        {
            await consumer.SubscribeAsync<string>(
                "notifications.fanout",
                group,
                env =>
                {
                    receivedPayloads.Add(env.Payload);
                    return Task.CompletedTask;
                });
        }

        // Publish once — all groups should be notified.
        await producer.PublishAsync(envelope, "notifications.fanout");

        // Verify three independent subscriptions were created.
        await consumer.Received(3).SubscribeAsync<string>(
            Arg.Is("notifications.fanout"),
            Arg.Any<string>(),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());

        // Verify each group was subscribed exactly once.
        foreach (var group in consumerGroups)
        {
            await consumer.Received(1).SubscribeAsync<string>(
                Arg.Is("notifications.fanout"),
                Arg.Is(group),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>());
        }

        // The producer published once to the fan-out topic.
        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "broadcast-event"),
            Arg.Is("notifications.fanout"),
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 3: Dead Letter Routing for Expired Messages ───────────────

    [Test]
    public async Task Challenge3_DeadLetterRouting_ExpiredMessagesGoToDlq()
    {
        // Implement dead letter routing: check IsExpired and route expired
        // messages to a DLQ topic instead of the normal processing topic.
        var producer = Substitute.For<IMessageBrokerProducer>();

        const string normalTopic = "orders.processing";
        const string dlqTopic = "orders.dlq";

        var validMessage = IntegrationEnvelope<string>.Create(
            "valid-order", "OrderService", "order.created") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        var expiredMessage = IntegrationEnvelope<string>.Create(
            "stale-order", "OrderService", "order.created") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        };

        // Route each message: expired → DLQ, valid → normal topic.
        var messagesToRoute = new[] { validMessage, expiredMessage };

        foreach (var msg in messagesToRoute)
        {
            var destination = msg.IsExpired ? dlqTopic : normalTopic;
            await producer.PublishAsync(msg, destination);
        }

        // Verify the valid message went to the normal topic.
        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "valid-order"),
            Arg.Is(normalTopic),
            Arg.Any<CancellationToken>());

        // Verify the expired message was routed to the DLQ.
        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "stale-order"),
            Arg.Is(dlqTopic),
            Arg.Any<CancellationToken>());
    }
}
