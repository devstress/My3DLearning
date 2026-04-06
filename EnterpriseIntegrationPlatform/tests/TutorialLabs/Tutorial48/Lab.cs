// ============================================================================
// Tutorial 48 – Notification Use Cases (Lab)
// ============================================================================
// This lab exercises the notification and validation activity services:
// DefaultMessageValidationService, MessageValidationResult,
// DefaultMessageLoggingService, and INotificationActivityService.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial48;

[TestFixture]
public sealed class Lab
{
    // ── DefaultMessageValidationService Returns Success ──────────────────────

    [Test]
    public async Task ValidateAsync_ValidMessage_ReturnsSuccess()
    {
        var svc = new DefaultMessageValidationService(
            NullLogger<DefaultMessageValidationService>.Instance);

        var result = await svc.ValidateAsync("order.created", "{\"id\": 1}");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    // ── MessageValidationResult.Success Static ──────────────────────────────

    [Test]
    public void MessageValidationResult_Success_HasExpectedValues()
    {
        var result = MessageValidationResult.Success;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    // ── MessageValidationResult.Failure Static ──────────────────────────────

    [Test]
    public void MessageValidationResult_Failure_HasReasonAndInvalid()
    {
        var result = MessageValidationResult.Failure("Schema mismatch");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Schema mismatch"));
    }

    // ── DefaultMessageLoggingService Completes ──────────────────────────────

    [Test]
    public async Task LogAsync_Completes_WithoutError()
    {
        var svc = new DefaultMessageLoggingService(
            NullLogger<DefaultMessageLoggingService>.Instance);

        Assert.DoesNotThrowAsync(() =>
            svc.LogAsync(Guid.NewGuid(), "order.created", "Validated"));
    }

    // ── INotificationActivityService Interface Shape ─────────────────────────

    [Test]
    public void INotificationActivityService_InterfaceShape()
    {
        var type = typeof(INotificationActivityService);

        Assert.That(type.IsInterface, Is.True);
        Assert.That(type.GetMethod("PublishAckAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("PublishNackAsync"), Is.Not.Null);
    }

    // ── IPersistenceActivityService Interface Shape ──────────────────────────

    [Test]
    public void IPersistenceActivityService_InterfaceShape()
    {
        var type = typeof(IPersistenceActivityService);

        Assert.That(type.IsInterface, Is.True);
        Assert.That(type.GetMethod("SaveMessageAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("UpdateDeliveryStatusAsync"), Is.Not.Null);
    }

    // ── Mock INotificationActivityService PublishAckAsync ────────────────────

    [Test]
    public async Task Mock_NotificationService_VerifyAckCalled()
    {
        var mock = Substitute.For<INotificationActivityService>();

        await mock.PublishAckAsync(Guid.NewGuid(), Guid.NewGuid(), "ack-topic", CancellationToken.None);

        await mock.Received(1).PublishAckAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Is("ack-topic"),
            Arg.Any<CancellationToken>());
    }
}
