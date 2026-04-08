// ============================================================================
// Tutorial 25 – Dead Letter Queue (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Dead Letter Channel pattern
//          captures unprocessable messages with full diagnostic context.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Publish_MaxRetriesExceeded_SendsToDeadLetterTopic — message routed to DLQ topic
//   2. Publish_PreservesOriginalEnvelope                 — original payload and MessageId preserved
//   3. Publish_SetsCorrectReason                         — reason and error message recorded
//   4. Publish_TracksAttemptCount                         — attempt count captured
//   5. Publish_SetsFailedAtTimestamp                      — FailedAt timestamp set at publish time
//   6. Publish_PreservesCorrelationId                     — CorrelationId preserved on wrapper
//   7. Publish_AllReasonValues_AreSupported               — all DeadLetterReason values accepted
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial25;

[TestFixture]
public sealed class Lab
{
    // ── 1. Core DLQ Publishing ───────────────────────────────────────

    [Test]
    public async Task Publish_MaxRetriesExceeded_SendsToDeadLetterTopic()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t25-maxretry");
        var topic = AspireFixture.UniqueTopic("t25-dlq");
        var publisher = CreatePublisher(nats, topic);
        var envelope = IntegrationEnvelope<string>.Create("order-data", "OrderSvc", "order.created");

        await publisher.PublishAsync(envelope, DeadLetterReason.MaxRetriesExceeded,
            "Max retries exceeded", 3, CancellationToken.None);

        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Publish_PreservesOriginalEnvelope()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t25-preserve");
        var topic = AspireFixture.UniqueTopic("t25-dlq");
        var publisher = CreatePublisher(nats, topic);
        var envelope = IntegrationEnvelope<string>.Create("payload", "Svc", "type");

        await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage,
            "Unprocessable", 1, CancellationToken.None);

        var received = nats.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.Payload.OriginalEnvelope.Payload, Is.EqualTo("payload"));
        Assert.That(received.Payload.OriginalEnvelope.MessageId, Is.EqualTo(envelope.MessageId));
    }


    // ── 2. Dead-Letter Metadata ──────────────────────────────────────

    [Test]
    public async Task Publish_SetsCorrectReason()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t25-reason");
        var topic = AspireFixture.UniqueTopic("t25-dlq");
        var publisher = CreatePublisher(nats, topic);
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");

        await publisher.PublishAsync(envelope, DeadLetterReason.ValidationFailed,
            "Schema invalid", 1, CancellationToken.None);

        var received = nats.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.Payload.Reason, Is.EqualTo(DeadLetterReason.ValidationFailed));
        Assert.That(received.Payload.ErrorMessage, Is.EqualTo("Schema invalid"));
    }

    [Test]
    public async Task Publish_TracksAttemptCount()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t25-attempts");
        var topic = AspireFixture.UniqueTopic("t25-dlq");
        var publisher = CreatePublisher(nats, topic);
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");

        await publisher.PublishAsync(envelope, DeadLetterReason.ProcessingTimeout,
            "Timed out", 5, CancellationToken.None);

        var received = nats.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.Payload.AttemptCount, Is.EqualTo(5));
    }

    [Test]
    public async Task Publish_SetsFailedAtTimestamp()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t25-timestamp");
        var topic = AspireFixture.UniqueTopic("t25-dlq");
        var publisher = CreatePublisher(nats, topic);
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");
        var before = DateTimeOffset.UtcNow;

        await publisher.PublishAsync(envelope, DeadLetterReason.UnroutableMessage,
            "No route", 1, CancellationToken.None);

        var received = nats.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.Payload.FailedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(received.Payload.FailedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }


    // ── 3. Reason Coverage ───────────────────────────────────────────

    [Test]
    public async Task Publish_PreservesCorrelationId()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t25-corr");
        var topic = AspireFixture.UniqueTopic("t25-dlq");
        var publisher = CreatePublisher(nats, topic);
        var correlationId = Guid.NewGuid();
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type", correlationId);

        await publisher.PublishAsync(envelope, DeadLetterReason.MaxRetriesExceeded,
            "Exhausted", 3, CancellationToken.None);

        var received = nats.GetReceived<DeadLetterEnvelope<string>>(0);
        Assert.That(received.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task Publish_AllReasonValues_AreSupported()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t25-allreasons");
        var topic = AspireFixture.UniqueTopic("t25-dlq");
        var publisher = CreatePublisher(nats, topic);

        var reasons = new[]
        {
            DeadLetterReason.MaxRetriesExceeded,
            DeadLetterReason.PoisonMessage,
            DeadLetterReason.ProcessingTimeout,
            DeadLetterReason.ValidationFailed,
            DeadLetterReason.UnroutableMessage,
            DeadLetterReason.MessageExpired,
        };

        foreach (var reason in reasons)
        {
            var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");
            await publisher.PublishAsync(envelope, reason, $"Error: {reason}", 1, CancellationToken.None);
        }

        nats.AssertReceivedOnTopic(topic, reasons.Length);
    }

    private static DeadLetterPublisher<string> CreatePublisher(
        NatsBrokerEndpoint nats, string topic)
    {
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = topic,
        });

        return new DeadLetterPublisher<string>(nats, options);
    }
}
