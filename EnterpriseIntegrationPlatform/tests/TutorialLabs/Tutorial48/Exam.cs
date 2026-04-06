// ============================================================================
// Tutorial 48 – Notification Use Cases (Exam)
// ============================================================================
// Coding challenges: full notification flow, validation failure path,
// and persistence activity mocking.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial48;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full Notification Flow ──────────────────────────────────

    [Test]
    public async Task Challenge1_FullNotificationFlow_ValidateLogNotify()
    {
        var validator = new DefaultMessageValidationService();
        var logger = new DefaultMessageLoggingService(
            NullLogger<DefaultMessageLoggingService>.Instance);
        var notifier = Substitute.For<INotificationActivityService>();

        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();

        // Step 1: Validate
        var validation = await validator.ValidateAsync("order.created", "{\"id\": 1}");
        Assert.That(validation.IsValid, Is.True);

        // Step 2: Log
        await logger.LogAsync(msgId, "order.created", "Validated");

        // Step 3: Notify
        if (validation.IsValid)
        {
            await notifier.PublishAckAsync(msgId, corrId, "ack-topic", CancellationToken.None);
        }

        await notifier.Received(1).PublishAckAsync(
            msgId, corrId, "ack-topic", Arg.Any<CancellationToken>());
    }

    // ── Challenge 2: Validation Failure Triggers Nack ───────────────────────

    [Test]
    public async Task Challenge2_ValidationFailure_TriggersNack()
    {
        var validator = Substitute.For<IMessageValidationService>();
        validator.ValidateAsync("bad.type", Arg.Any<string>())
            .Returns(MessageValidationResult.Failure("Unknown message type"));

        var notifier = Substitute.For<INotificationActivityService>();

        var msgId = Guid.NewGuid();
        var corrId = Guid.NewGuid();

        var result = await validator.ValidateAsync("bad.type", "{}");

        if (!result.IsValid)
        {
            await notifier.PublishNackAsync(
                msgId, corrId, result.Reason!, "nack-topic", CancellationToken.None);
        }

        Assert.That(result.IsValid, Is.False);
        await notifier.Received(1).PublishNackAsync(
            msgId, corrId,
            Arg.Is<string>(s => s.Contains("Unknown")),
            "nack-topic",
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 3: Persistence Activity Mock ──────────────────────────────

    [Test]
    public async Task Challenge3_PersistenceActivity_SaveAndUpdateStatus()
    {
        var persistence = Substitute.For<IPersistenceActivityService>();

        var input = new IntegrationPipelineInput(Guid.NewGuid(), Guid.NewGuid(), null, DateTimeOffset.UtcNow, "OrderService", "order.created", "1.0", 1, "{\"orderId\": \"ORD-1\"}", null, "ack", "nack");

        await persistence.SaveMessageAsync(input, CancellationToken.None);
        await persistence.UpdateDeliveryStatusAsync(
            input.MessageId, input.CorrelationId, DateTimeOffset.UtcNow,
            "Delivered", CancellationToken.None);

        await persistence.Received(1).SaveMessageAsync(input, Arg.Any<CancellationToken>());
        await persistence.Received(1).UpdateDeliveryStatusAsync(
            input.MessageId, input.CorrelationId,
            Arg.Any<DateTimeOffset>(), "Delivered", Arg.Any<CancellationToken>());
    }
}
