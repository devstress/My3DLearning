// ============================================================================
// Tutorial 48 – Notification Use Cases (Exam)
// ============================================================================
// E2E challenges: conditional ack/nack, multi-message notification batch,
// and persistence activity flow via MockEndpoint.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial48;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_ConditionalAckNack_CorrectTopics()
    {
        await using var output = new MockEndpoint("cond");
        var validator = new DefaultMessageValidationService();

        // Valid message → ack
        var r1 = await validator.ValidateAsync("order.created", "{\"id\":1}");
        await output.PublishAsync(
            IntegrationEnvelope<string>.Create("ok", "pipeline", "notification"),
            r1.IsValid ? "ack" : "nack");

        output.AssertReceivedOnTopic("ack", 1);
        output.AssertReceivedCount(1);
    }

    [Test]
    public async Task Challenge2_BatchNotification_MultipleMessages()
    {
        await using var output = new MockEndpoint("batch");
        var validator = new DefaultMessageValidationService();

        for (var i = 0; i < 5; i++)
        {
            var r = await validator.ValidateAsync("order.created", $"{{\"id\":{i}}}");
            await output.PublishAsync(
                IntegrationEnvelope<string>.Create($"msg-{i}", "pipeline", "batch.notify"),
                "batch-ack");
        }

        output.AssertReceivedCount(5);
        output.AssertReceivedOnTopic("batch-ack", 5);
    }

    [Test]
    public async Task Challenge3_PersistenceActivity_SaveAndUpdate()
    {
        await using var output = new MockEndpoint("persist");
        var persistence = new MockPersistenceActivityService();

        var input = new IntegrationPipelineInput(
            Guid.NewGuid(), Guid.NewGuid(), null, DateTimeOffset.UtcNow,
            "OrderService", "order.created", "1.0", 1, "{}", null, "ack", "nack");

        await persistence.SaveMessageAsync(input, CancellationToken.None);
        await persistence.UpdateDeliveryStatusAsync(
            input.MessageId, input.CorrelationId, DateTimeOffset.UtcNow,
            "Delivered", CancellationToken.None);

        // Publish delivery confirmation
        await output.PublishAsync(
            IntegrationEnvelope<string>.Create("Delivered", "pipeline", "delivery.status"),
            "delivery-confirmations");

        persistence.AssertSaveCount(1);
        output.AssertReceivedOnTopic("delivery-confirmations", 1);
    }
}
