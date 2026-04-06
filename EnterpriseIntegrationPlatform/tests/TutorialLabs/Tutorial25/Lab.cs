// ============================================================================
// Tutorial 25 – Dead Letter Queue (Lab)
// ============================================================================
// This lab exercises the DeadLetterPublisher, DeadLetterReason enum,
// DeadLetterEnvelope construction, and the DeadLetterOptions defaults.
// You will verify correct DLQ publishing, reason codes, and error messages.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial25;

[TestFixture]
public sealed class Lab
{
    // ── Publish Routes To Dead Letter Topic ──────────────────────────────────

    [Test]
    public async Task Publish_SendsToConfiguredDeadLetterTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "dlq-topic",
        });

        var publisher = new DeadLetterPublisher<string>(producer, options);

        var envelope = IntegrationEnvelope<string>.Create(
            "bad-payload", "OrderSvc", "order.created");

        await publisher.PublishAsync(
            envelope,
            DeadLetterReason.MaxRetriesExceeded,
            "Failed after 3 retries",
            attemptCount: 3,
            CancellationToken.None);

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<DeadLetterEnvelope<string>>>(),
            "dlq-topic",
            Arg.Any<CancellationToken>());
    }

    // ── Missing DeadLetterTopic Throws ───────────────────────────────────────

    [Test]
    public void Publish_EmptyTopic_ThrowsInvalidOperationException()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "",
        });

        var publisher = new DeadLetterPublisher<string>(producer, options);
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "type");

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(
                envelope,
                DeadLetterReason.PoisonMessage,
                "error",
                1,
                CancellationToken.None));
    }

    // ── DeadLetterReason Enum Values ─────────────────────────────────────────

    [Test]
    public void DeadLetterReason_ContainsExpectedValues()
    {
        Assert.That(Enum.IsDefined(typeof(DeadLetterReason), DeadLetterReason.MaxRetriesExceeded), Is.True);
        Assert.That(Enum.IsDefined(typeof(DeadLetterReason), DeadLetterReason.PoisonMessage), Is.True);
        Assert.That(Enum.IsDefined(typeof(DeadLetterReason), DeadLetterReason.ProcessingTimeout), Is.True);
        Assert.That(Enum.IsDefined(typeof(DeadLetterReason), DeadLetterReason.ValidationFailed), Is.True);
        Assert.That(Enum.IsDefined(typeof(DeadLetterReason), DeadLetterReason.UnroutableMessage), Is.True);
        Assert.That(Enum.IsDefined(typeof(DeadLetterReason), DeadLetterReason.MessageExpired), Is.True);
    }

    // ── DeadLetterEnvelope Record Construction ───────────────────────────────

    [Test]
    public void DeadLetterEnvelope_RecordProperties_AreCorrect()
    {
        var original = IntegrationEnvelope<string>.Create(
            "payload", "Svc", "type");

        var dlEnvelope = new DeadLetterEnvelope<string>
        {
            OriginalEnvelope = original,
            Reason = DeadLetterReason.ValidationFailed,
            ErrorMessage = "Schema mismatch",
            FailedAt = DateTimeOffset.UtcNow,
            AttemptCount = 2,
        };

        Assert.That(dlEnvelope.OriginalEnvelope.Payload, Is.EqualTo("payload"));
        Assert.That(dlEnvelope.Reason, Is.EqualTo(DeadLetterReason.ValidationFailed));
        Assert.That(dlEnvelope.ErrorMessage, Is.EqualTo("Schema mismatch"));
        Assert.That(dlEnvelope.AttemptCount, Is.EqualTo(2));
    }

    // ── Options Default Values ──────────────────────────────────────────────

    [Test]
    public void Options_DefaultValues_AreCorrect()
    {
        var opts = new DeadLetterOptions();

        Assert.That(opts.DeadLetterTopic, Is.EqualTo(string.Empty));
        Assert.That(opts.MaxRetryAttempts, Is.EqualTo(3));
        Assert.That(opts.MessageType, Is.EqualTo("DeadLetter"));
    }

    // ── Publisher Preserves CorrelationId On Wrapper ─────────────────────────

    [Test]
    public async Task Publish_WrappedEnvelope_CarriesOriginalCorrelationId()
    {
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

        var originalCorrelationId = Guid.NewGuid();
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "type", correlationId: originalCorrelationId);

        await publisher.PublishAsync(
            envelope, DeadLetterReason.MessageExpired, "expired", 0, CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.CorrelationId, Is.EqualTo(originalCorrelationId));
    }

    // ── Publisher Uses Custom Source When Configured ─────────────────────────

    [Test]
    public async Task Publish_CustomSource_OverridesEnvelopeSource()
    {
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
            Source = "DLQ-Publisher",
        });

        var publisher = new DeadLetterPublisher<string>(producer, options);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "OriginalSvc", "type");

        await publisher.PublishAsync(
            envelope, DeadLetterReason.UnroutableMessage, "no route", 1, CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Source, Is.EqualTo("DLQ-Publisher"));
    }
}
