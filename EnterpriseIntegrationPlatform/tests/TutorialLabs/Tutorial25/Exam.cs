// ============================================================================
// Tutorial 25 – Dead Letter Queue (Exam)
// ============================================================================
// Coding challenges: publish with each distinct DeadLetterReason, verify
// the CausationId link from original to wrapper, and test the
// mock-based IDeadLetterPublisher contract.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial25;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Publish With Multiple Reason Codes ──────────────────────

    [Test]
    public async Task Challenge1_PublishWithDifferentReasons_AllSucceed()
    {
        // Publish three messages with different DeadLetterReason values and
        // verify the producer is called for each one.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "dlq-multi",
        });

        var publisher = new DeadLetterPublisher<string>(producer, options);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "msg.type");

        await publisher.PublishAsync(
            envelope, DeadLetterReason.MaxRetriesExceeded, "retries exhausted", 3, CancellationToken.None);
        await publisher.PublishAsync(
            envelope, DeadLetterReason.ProcessingTimeout, "timed out", 1, CancellationToken.None);
        await publisher.PublishAsync(
            envelope, DeadLetterReason.PoisonMessage, "corrupt payload", 1, CancellationToken.None);

        await producer.Received(3).PublishAsync(
            Arg.Any<IntegrationEnvelope<DeadLetterEnvelope<string>>>(),
            "dlq-multi",
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 2: CausationId Links Original To Wrapper ──────────────────

    [Test]
    public async Task Challenge2_CausationId_IsSetToOriginalMessageId()
    {
        // The wrapper envelope's CausationId should equal the original
        // envelope's MessageId — establishing a causal chain.
        IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
        var producer = Substitute.For<IMessageBrokerProducer>();
        producer
            .PublishAsync(
                Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "dlq",
        });

        var publisher = new DeadLetterPublisher<string>(producer, options);

        var original = IntegrationEnvelope<string>.Create(
            "important-data", "CriticalSvc", "order.created");

        await publisher.PublishAsync(
            original, DeadLetterReason.ValidationFailed, "invalid schema", 1, CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.CausationId, Is.EqualTo(original.MessageId));
    }

    // ── Challenge 3: Mock IDeadLetterPublisher Contract ─────────────────────

    [Test]
    public async Task Challenge3_MockPublisher_VerifyCorrectParameters()
    {
        // Use NSubstitute to mock IDeadLetterPublisher<string> and verify it
        // is called with the correct reason, error message, and attempt count.
        var mockPublisher = Substitute.For<IDeadLetterPublisher<string>>();

        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "SomeService", "event.type");

        await mockPublisher.PublishAsync(
            envelope,
            DeadLetterReason.MessageExpired,
            "TTL exceeded",
            attemptCount: 0,
            CancellationToken.None);

        await mockPublisher.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.MessageId == envelope.MessageId),
            DeadLetterReason.MessageExpired,
            "TTL exceeded",
            0,
            Arg.Any<CancellationToken>());
    }
}
