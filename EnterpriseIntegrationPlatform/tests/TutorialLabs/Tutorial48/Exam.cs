// ============================================================================
// Tutorial 48 – Notification Use Cases (Exam)
// ============================================================================
// E2E challenges: conditional ack/nack, multi-message notification batch,
// and persistence activity flow via NatsBrokerEndpoint (real NATS JetStream
// via Aspire).
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
        await using var nats = AspireFixture.CreateNatsEndpoint("t48-exam-cond");
        var ackTopic = AspireFixture.UniqueTopic("t48-exam-ack");
        var nackTopic = AspireFixture.UniqueTopic("t48-exam-nack");
        var validator = new DefaultMessageValidationService();

        // Valid message → ack
        var r1 = await validator.ValidateAsync("order.created", "{\"id\":1}");
        await nats.PublishAsync(
            IntegrationEnvelope<string>.Create("ok", "pipeline", "notification"),
            r1.IsValid ? ackTopic : nackTopic, default);

        nats.AssertReceivedOnTopic(ackTopic, 1);
        nats.AssertReceivedCount(1);
    }

    [Test]
    public async Task Challenge2_BatchNotification_MultipleMessages()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t48-exam-batch");
        var topic = AspireFixture.UniqueTopic("t48-exam-batch-ack");
        var validator = new DefaultMessageValidationService();

        for (var i = 0; i < 5; i++)
        {
            var r = await validator.ValidateAsync("order.created", $"{{\"id\":{i}}}");
            await nats.PublishAsync(
                IntegrationEnvelope<string>.Create($"msg-{i}", "pipeline", "batch.notify"),
                topic, default);
        }

        nats.AssertReceivedCount(5);
        nats.AssertReceivedOnTopic(topic, 5);
    }

    [Test]
    public async Task Challenge3_PersistenceActivity_SaveAndUpdate()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t48-exam-persist");
        var topic = AspireFixture.UniqueTopic("t48-exam-delivery");
        var persistence = new MockPersistenceActivityService();

        var input = new IntegrationPipelineInput(
            Guid.NewGuid(), Guid.NewGuid(), null, DateTimeOffset.UtcNow,
            "OrderService", "order.created", "1.0", 1, "{}", null, "ack", "nack");

        await persistence.SaveMessageAsync(input, CancellationToken.None);
        await persistence.UpdateDeliveryStatusAsync(
            input.MessageId, input.CorrelationId, DateTimeOffset.UtcNow,
            "Delivered", CancellationToken.None);

        // Publish delivery confirmation
        await nats.PublishAsync(
            IntegrationEnvelope<string>.Create("Delivered", "pipeline", "delivery.status"),
            topic, default);

        persistence.AssertSaveCount(1);
        nats.AssertReceivedOnTopic(topic, 1);
    }
}
