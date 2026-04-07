// ============================================================================
// Tutorial 48 – Notification Use Cases (Lab)
// ============================================================================
// EIP Pattern: Notification / Ack-Nack.
// E2E: Wire validation, logging, and notification services with MockEndpoint
// to verify ack/nack publish flow after validation.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial48;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("notify-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Validation & Notification ─────────────────────────────────

    [Test]
    public async Task Validate_Success_PublishesAck()
    {
        var validator = new DefaultMessageValidationService();
        var result = await validator.ValidateAsync("order.created", "{\"id\": 1}");

        Assert.That(result.IsValid, Is.True);

        // Publish ack notification
        var ack = IntegrationEnvelope<string>.Create("ack", "pipeline", "notification.ack");
        await _output.PublishAsync(ack, "ack-topic");
        _output.AssertReceivedOnTopic("ack-topic", 1);
    }

    [Test]
    public async Task Validate_Failure_PublishesNack()
    {
        var validator = new MockMessageValidationService()
            .WithResult("bad.type", MessageValidationResult.Failure("Unknown type"));

        var result = await validator.ValidateAsync("bad.type", "{}");
        Assert.That(result.IsValid, Is.False);

        var nack = IntegrationEnvelope<string>.Create(
            result.Reason!, "pipeline", "notification.nack");
        await _output.PublishAsync(nack, "nack-topic");
        _output.AssertReceivedOnTopic("nack-topic", 1);
    }


    // ── 2. Logging ───────────────────────────────────────────────────

    [Test]
    public async Task LogAsync_CompletesWithoutError()
    {
        var svc = new DefaultMessageLoggingService(
            NullLogger<DefaultMessageLoggingService>.Instance);

        Assert.DoesNotThrowAsync(() =>
            svc.LogAsync(Guid.NewGuid(), "order.created", "Validated"));
    }

    [Test]
    public void MessageValidationResult_Success_HasExpectedValues()
    {
        var result = MessageValidationResult.Success;
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }


    // ── 3. End-to-End Notification Flow ──────────────────────────────

    [Test]
    public void MessageValidationResult_Failure_HasReasonAndInvalid()
    {
        var result = MessageValidationResult.Failure("Schema mismatch");
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Schema mismatch"));
    }

    [Test]
    public async Task FullNotificationFlow_ValidateLogPublish()
    {
        var validator = new DefaultMessageValidationService();
        var logger = new DefaultMessageLoggingService(
            NullLogger<DefaultMessageLoggingService>.Instance);

        var msgId = Guid.NewGuid();
        var validation = await validator.ValidateAsync("order.created", "{\"id\": 1}");
        await logger.LogAsync(msgId, "order.created", "Validated");

        var envelope = IntegrationEnvelope<string>.Create(
            validation.IsValid ? "ack" : "nack", "pipeline", "notification.result");
        await _output.PublishAsync(envelope, validation.IsValid ? "ack-topic" : "nack-topic");

        _output.AssertReceivedOnTopic("ack-topic", 1);
    }
}
