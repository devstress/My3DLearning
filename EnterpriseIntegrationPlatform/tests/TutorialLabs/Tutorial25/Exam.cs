// ============================================================================
// Tutorial 25 – Dead Letter Queue (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Multiple failures all reach the DLQ with correct reasons
//   🟡 Intermediate  — Original envelope metadata and priority are preserved
//   🔴 Advanced      — Missing DLQ topic configuration throws and no message is sent
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial25;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Multiple failures all reach the DLQ ──────────────
    //
    // SCENARIO: Three order messages fail for different reasons
    //           (MaxRetriesExceeded, PoisonMessage, ValidationFailed). Each
    //           is published to the DLQ and the reasons are verified.
    //
    // WHAT YOU PROVE: All failed messages reach the DLQ with their correct
    //                 DeadLetterReason preserved in order.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_MultipleFailures_AllReachDlq()
    {
        await using var output = new MockEndpoint("exam-dlq");
        var publisher = CreatePublisher(output);

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e1 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e2 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e3 = null!;

        // TODO: await publisher.PublishAsync(...)
        // TODO: await publisher.PublishAsync(...)
        // TODO: await publisher.PublishAsync(...)

        output.AssertReceivedOnTopic("dlq-topic", 3);

        var all = output.GetAllReceived<DeadLetterEnvelope<string>>("dlq-topic");
        Assert.That(all[0].Payload.Reason, Is.EqualTo(DeadLetterReason.MaxRetriesExceeded));
        Assert.That(all[1].Payload.Reason, Is.EqualTo(DeadLetterReason.PoisonMessage));
        Assert.That(all[2].Payload.Reason, Is.EqualTo(DeadLetterReason.ValidationFailed));
    }

    // ── 🟡 INTERMEDIATE — Original envelope metadata preserved ─────────
    //
    // SCENARIO: An authentication failure envelope with custom metadata
    //           (userId, region) and High priority is sent to the DLQ. The
    //           received dead-letter envelope must contain the full original.
    //
    // WHAT YOU PROVE: The publisher preserves all original envelope fields
    //                 including Payload, Source, MessageType, Metadata, and
    //                 Priority inside the DeadLetterEnvelope wrapper.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_OriginalEnvelope_MetadataPreserved()
    {
        await using var output = new MockEndpoint("exam-meta");
        var publisher = CreatePublisher(output);

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic original = null!;

        // TODO: await publisher.PublishAsync(...)

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
    //
    // SCENARIO: A DeadLetterPublisher is created with an empty
    //           DeadLetterTopic. Publishing any message must throw
    //           InvalidOperationException and no message should be sent.
    //
    // WHAT YOU PROVE: The configuration guard prevents publishing when
    //                 the topic is not configured, and the endpoint
    //                 receives nothing.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_MissingDeadLetterTopic_Throws()
    {
        await using var output = new MockEndpoint("exam-notopic");
        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a DeadLetterPublisher with appropriate configuration
        dynamic publisher = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;

        Assert.ThrowsAsync<InvalidOperationException>(async () => {
            // TODO: await publisher.PublishAsync(...)
            });

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
#endif
