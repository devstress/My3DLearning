// ============================================================================
// Tutorial 25 – Dead Letter Queue (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Multiple failures all reach the DLQ with correct reasons
//   🟡 Intermediate — Original envelope metadata and priority are preserved
//   🔴 Advanced     — Missing DLQ topic configuration throws and no message is sent
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial25;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — Multiple failures all reach the DLQ ──────────────

    [Test]
    public async Task Starter_MultipleFailures_AllReachDlq()
    {
        await using var output = new MockEndpoint("exam-dlq");
        var publisher = CreatePublisher(output);

        var e1 = IntegrationEnvelope<string>.Create("order-1", "svc", "order.created");
        var e2 = IntegrationEnvelope<string>.Create("order-2", "svc", "order.created");
        var e3 = IntegrationEnvelope<string>.Create("order-3", "svc", "order.created");

        await publisher.PublishAsync(e1, DeadLetterReason.MaxRetriesExceeded, "Retries exhausted", 3, CancellationToken.None);
        await publisher.PublishAsync(e2, DeadLetterReason.PoisonMessage, "Corrupt payload", 1, CancellationToken.None);
        await publisher.PublishAsync(e3, DeadLetterReason.ValidationFailed, "Invalid schema", 1, CancellationToken.None);

        output.AssertReceivedOnTopic("dlq-topic", 3);

        var all = output.GetAllReceived<DeadLetterEnvelope<string>>("dlq-topic");
        Assert.That(all[0].Payload.Reason, Is.EqualTo(DeadLetterReason.MaxRetriesExceeded));
        Assert.That(all[1].Payload.Reason, Is.EqualTo(DeadLetterReason.PoisonMessage));
        Assert.That(all[2].Payload.Reason, Is.EqualTo(DeadLetterReason.ValidationFailed));
    }

    // ── 🟡 INTERMEDIATE — Original envelope metadata preserved ─────────

    [Test]
    public async Task Intermediate_OriginalEnvelope_MetadataPreserved()
    {
        await using var output = new MockEndpoint("exam-meta");
        var publisher = CreatePublisher(output);

        var original = IntegrationEnvelope<string>.Create("sensitive-data", "AuthSvc", "auth.failed") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = "user-42",
                ["region"] = "eu-west",
            },
            Priority = MessagePriority.High,
        };

        await publisher.PublishAsync(original, DeadLetterReason.MessageExpired,
            "TTL exceeded", 1, CancellationToken.None);

        var received = output.GetReceived<DeadLetterEnvelope<string>>(0);
        var orig = received.Payload.OriginalEnvelope;
        Assert.That(orig.Payload, Is.EqualTo("sensitive-data"));
        Assert.That(orig.Source, Is.EqualTo("AuthSvc"));
        Assert.That(orig.MessageType, Is.EqualTo("auth.failed"));
        Assert.That(orig.Metadata["userId"], Is.EqualTo("user-42"));
        Assert.That(orig.Metadata["region"], Is.EqualTo("eu-west"));
        Assert.That(orig.Priority, Is.EqualTo(MessagePriority.High));
    }

    // ── 🔴 ADVANCED — Missing DLQ topic throws ────────────────────────

    [Test]
    public async Task Advanced_MissingDeadLetterTopic_Throws()
    {
        await using var output = new MockEndpoint("exam-notopic");
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "",
        });
        var publisher = new DeadLetterPublisher<string>(output, options);
        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "type");

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await publisher.PublishAsync(envelope, DeadLetterReason.PoisonMessage,
                "Bad message", 1, CancellationToken.None));

        output.AssertNoneReceived();
    }

    private static DeadLetterPublisher<string> CreatePublisher(MockEndpoint output)
    {
        var options = Options.Create(new DeadLetterOptions
        {
            DeadLetterTopic = "dlq-topic",
        });

        return new DeadLetterPublisher<string>(output, options);
    }
}
