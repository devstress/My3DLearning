// ============================================================================
// Tutorial 48 – Notification Use Cases (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — conditional ack nack_ correct topics
//   🟡 Intermediate  — batch notification_ multiple messages
//   🔴 Advanced      — persistence activity_ save and update
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial48;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_ConditionalAckNack_CorrectTopics()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t48-exam-cond");
        var ackTopic = AspireFixture.UniqueTopic("t48-exam-ack");
        var nackTopic = AspireFixture.UniqueTopic("t48-exam-nack");
        // TODO: Create a DefaultMessageValidationService with appropriate configuration
        dynamic validator = null!;

        // Valid message → ack
        // TODO: var r1 = await validator.ValidateAsync(...)
        dynamic r1 = null!;
        // TODO: await nats.PublishAsync(...)

        nats.AssertReceivedOnTopic(ackTopic, 1);
        nats.AssertReceivedCount(1);
    }

    [Test]
    public async Task Intermediate_BatchNotification_MultipleMessages()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t48-exam-batch");
        var topic = AspireFixture.UniqueTopic("t48-exam-batch-ack");
        // TODO: Create a DefaultMessageValidationService with appropriate configuration
        dynamic validator = null!;

        for (var i = 0; i < 5; i++)
        {
            // TODO: var r = await validator.ValidateAsync(...)
            dynamic r = null!;
            // TODO: await nats.PublishAsync(...)
        }

        nats.AssertReceivedCount(5);
        nats.AssertReceivedOnTopic(topic, 5);
    }

    [Test]
    public async Task Advanced_PersistenceActivity_SaveAndUpdate()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t48-exam-persist");
        var topic = AspireFixture.UniqueTopic("t48-exam-delivery");
        // TODO: Create a MockPersistenceActivityService with appropriate configuration
        dynamic persistence = null!;

        // TODO: Create a IntegrationPipelineInput with appropriate configuration
        dynamic input = null!;

        await persistence.SaveMessageAsync(input, CancellationToken.None);
        await persistence.UpdateDeliveryStatusAsync(
            input.MessageId, input.CorrelationId, DateTimeOffset.UtcNow,
            "Delivered", CancellationToken.None);

        // Publish delivery confirmation
        // TODO: await nats.PublishAsync(...)

        persistence.AssertSaveCount(1);
        nats.AssertReceivedOnTopic(topic, 1);
    }
}
#endif
