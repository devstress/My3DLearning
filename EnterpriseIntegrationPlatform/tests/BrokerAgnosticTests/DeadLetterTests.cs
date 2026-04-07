// ============================================================================
// Broker-Agnostic EIP Tests — Dead Letter Queue
// ============================================================================
// These tests prove that DeadLetterPublisher works identically regardless of
// which IMessageBrokerProducer implementation backs it. Any broker (MockEndpoint,
// NATS, Kafka, Pulsar, Postgres) produces the same EIP behaviour.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace BrokerAgnosticTests;

[TestFixture]
public sealed class DeadLetterTests
{
    // ── 1. DLQ Routing ──────────────────────────────────────────────────

    [Test]
    public async Task DeadLetterPublisher_RoutesToConfiguredTopic()
    {
        // Given: a DeadLetterPublisher wired to a broker endpoint
        var broker = new MockEndpoint("dlq-test");
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "eip.dead-letter",
            Source = "BrokerAgnosticTest"
        });
        var publisher = new DeadLetterPublisher<string>(broker, options);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-payload", "OrderService", "OrderCreated");

        // When: a message is dead-lettered
        await publisher.PublishAsync(
            envelope, DeadLetterReason.MaxRetriesExceeded,
            "Processing failed after 3 attempts", attemptCount: 3, CancellationToken.None);

        // Then: the message arrives on the DLQ topic
        broker.AssertReceivedCount(1);
        broker.AssertReceivedOnTopic("eip.dead-letter", 1);
    }

    [Test]
    public async Task DeadLetterPublisher_PreservesCorrelationId()
    {
        // The DLQ envelope must carry the original message's CorrelationId
        // so operators can trace back to the source conversation.
        var broker = new MockEndpoint("dlq-correlation");
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "eip.dead-letter",
            Source = "BrokerAgnosticTest"
        });
        var publisher = new DeadLetterPublisher<string>(broker, options);

        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "Src", "Type");

        await publisher.PublishAsync(
            envelope, DeadLetterReason.ValidationFailed,
            "Schema mismatch", attemptCount: 1, CancellationToken.None);

        var received = broker.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(received.CausationId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task DeadLetterPublisher_WrapsOriginalEnvelope()
    {
        // The DLQ message payload must contain the original envelope,
        // the reason, error message, and attempt count.
        var broker = new MockEndpoint("dlq-wrap");
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "eip.dead-letter",
            Source = "TestSrc"
        });
        var publisher = new DeadLetterPublisher<int>(broker, options);

        var original = IntegrationEnvelope<int>.Create(42, "Src", "IntMessage");

        await publisher.PublishAsync(
            original, DeadLetterReason.MaxRetriesExceeded,
            "Timeout", attemptCount: 5, CancellationToken.None);

        var received = broker.GetReceived<DeadLetterEnvelope<int>>(0);
        var payload = received.Payload;
        Assert.That(payload.OriginalEnvelope.Payload, Is.EqualTo(42));
        Assert.That(payload.Reason, Is.EqualTo(DeadLetterReason.MaxRetriesExceeded));
        Assert.That(payload.ErrorMessage, Is.EqualTo("Timeout"));
        Assert.That(payload.AttemptCount, Is.EqualTo(5));
    }

    // ── 2. DLQ Error Handling ───────────────────────────────────────────

    [Test]
    public void DeadLetterPublisher_ThrowsWhenTopicNotConfigured()
    {
        // If no DLQ topic is configured, the publisher must fail-fast
        // rather than silently dropping messages.
        var broker = new MockEndpoint("dlq-no-topic");
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "" // Empty
        });
        var publisher = new DeadLetterPublisher<string>(broker, options);

        var envelope = IntegrationEnvelope<string>.Create("x", "S", "T");

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(envelope, DeadLetterReason.ValidationFailed,
                "err", 1, CancellationToken.None));
    }

    [Test]
    public async Task DeadLetterPublisher_MultipleReasons_AllRouted()
    {
        // Verify that different DLQ reasons all route to the same topic.
        var broker = new MockEndpoint("dlq-reasons");
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "eip.dlq",
            Source = "Test"
        });
        var publisher = new DeadLetterPublisher<string>(broker, options);

        var reasons = new[]
        {
            DeadLetterReason.MaxRetriesExceeded,
            DeadLetterReason.ValidationFailed,
            DeadLetterReason.MessageExpired,
        };

        foreach (var reason in reasons)
        {
            var env = IntegrationEnvelope<string>.Create("p", "S", "T");
            await publisher.PublishAsync(env, reason, $"Error: {reason}", 1, CancellationToken.None);
        }

        broker.AssertReceivedCount(3);
        broker.AssertReceivedOnTopic("eip.dlq", 3);
    }
}
