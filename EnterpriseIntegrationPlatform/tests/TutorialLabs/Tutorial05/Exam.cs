// ============================================================================
// Tutorial 05 – Message Brokers (Exam)
// ============================================================================
// Coding challenges: multi-broker fan-out, consumer group isolation, and
// verifying message ordering via sequence numbers.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial05;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Multi-Broker Publishing ────────────────────────────────

    [Test]
    public async Task Challenge1_PublishSameMessage_ToDifferentBrokers()
    {
        // In a multi-broker architecture you might publish the same event
        // to NATS (for real-time) and Kafka (for long-term retention).
        var natsProducer = Substitute.For<IMessageBrokerProducer>();
        var kafkaProducer = Substitute.For<IMessageBrokerProducer>();
        var pulsarProducer = Substitute.For<IMessageBrokerProducer>();

        var envelope = IntegrationEnvelope<string>.Create(
            "critical-event", "AlertService", "alert.raised") with
        {
            Priority = MessagePriority.Critical,
            Intent = MessageIntent.Event,
        };

        // Publish the same envelope to all three brokers.
        await natsProducer.PublishAsync(envelope, "alerts");
        await kafkaProducer.PublishAsync(envelope, "alerts");
        await pulsarProducer.PublishAsync(envelope, "alerts");

        // Each broker received exactly one publish.
        await natsProducer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "critical-event"),
            Arg.Is("alerts"),
            Arg.Any<CancellationToken>());

        await kafkaProducer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "critical-event"),
            Arg.Is("alerts"),
            Arg.Any<CancellationToken>());

        await pulsarProducer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "critical-event"),
            Arg.Is("alerts"),
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 2: Consumer Groups with Different Group Names ─────────────

    [Test]
    public async Task Challenge2_DifferentConsumerGroups_ReceiveIndependently()
    {
        // Three independent consumer groups on the same topic.
        // Each group processes messages independently.
        var consumer = Substitute.For<IMessageBrokerConsumer>();

        var groups = new[] { "billing-group", "analytics-group", "audit-group" };
        const string topic = "order-events";

        foreach (var group in groups)
        {
            await consumer.SubscribeAsync<string>(
                topic, group, _ => Task.CompletedTask);
        }

        // Verify subscribe was called three times — once per group.
        await consumer.Received(3).SubscribeAsync<string>(
            Arg.Is(topic),
            Arg.Any<string>(),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());

        // Verify each group name was used exactly once.
        foreach (var group in groups)
        {
            await consumer.Received(1).SubscribeAsync<string>(
                Arg.Is(topic),
                Arg.Is(group),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>());
        }
    }

    // ── Challenge 3: Message Ordering via Sequence Numbers ──────────────────

    [Test]
    public async Task Challenge3_SequenceNumberedMessages_MaintainOrder()
    {
        // Publish a sequence of messages and verify ordering is preserved
        // by checking SequenceNumber and TotalCount on each envelope.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var correlationId = Guid.NewGuid();
        const int totalMessages = 5;

        var envelopes = Enumerable.Range(0, totalMessages)
            .Select(i => IntegrationEnvelope<string>.Create(
                payload: $"chunk-{i}",
                source: "Splitter",
                messageType: "data.chunk",
                correlationId: correlationId) with
            {
                SequenceNumber = i,
                TotalCount = totalMessages,
            })
            .ToList();

        // Publish all in order.
        foreach (var env in envelopes)
        {
            await producer.PublishAsync(env, "data-chunks");
        }

        // Verify the sequence numbers form an unbroken 0..N-1 range.
        for (var i = 0; i < totalMessages; i++)
        {
            Assert.That(envelopes[i].SequenceNumber, Is.EqualTo(i));
            Assert.That(envelopes[i].TotalCount, Is.EqualTo(totalMessages));
            Assert.That(envelopes[i].Payload, Is.EqualTo($"chunk-{i}"));
        }

        // All share the same CorrelationId.
        Assert.That(envelopes.Select(e => e.CorrelationId).Distinct().Count(),
            Is.EqualTo(1));

        // The producer received exactly totalMessages publish calls.
        await producer.Received(totalMessages).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("data-chunks"),
            Arg.Any<CancellationToken>());
    }
}
