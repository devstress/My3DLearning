// ============================================================================
// Tutorial 07 – Temporal Workflows (Exam)
// ============================================================================
// Coding challenges: design a workflow activity chain and test cancellation
// token propagation patterns used in workflow activity execution.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial07;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Design a Validate → Transform → Route Activity Chain ───

    [Test]
    public async Task Challenge1_ActivityChain_ValidateThenTransformThenRoute()
    {
        // Design a three-step workflow activity chain:
        //   1. Validate the message payload
        //   2. Transform: enrich metadata (simulate by logging "Transformed")
        //   3. Route: log the final routing decision
        //
        // Each step depends on the previous one succeeding.
        var validationService = Substitute.For<IMessageValidationService>();
        var loggingService = Substitute.For<IMessageLoggingService>();

        var messageId = Guid.NewGuid();
        const string messageType = "invoice.received";
        const string payloadJson = "{\"invoiceId\": \"INV-999\", \"amount\": 1500.00}";

        // Configure mocks.
        validationService.ValidateAsync(messageType, payloadJson)
            .Returns(MessageValidationResult.Success);
        loggingService.LogAsync(messageId, messageType, Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Step 1: Validate.
        var result = await validationService.ValidateAsync(messageType, payloadJson);
        Assert.That(result.IsValid, Is.True, "Validation must pass before transform");

        // Step 2: Transform (log the transformation step).
        await loggingService.LogAsync(messageId, messageType, "Transformed");

        // Step 3: Route (log the routing decision).
        await loggingService.LogAsync(messageId, messageType, "Routed");

        // Verify the chain executed in order with exactly 1 call per step.
        Received.InOrder(() =>
        {
            validationService.ValidateAsync(messageType, payloadJson);
            loggingService.LogAsync(messageId, messageType, "Transformed");
            loggingService.LogAsync(messageId, messageType, "Routed");
        });
    }

    [Test]
    public async Task Challenge1_ActivityChain_ValidationFails_StopsChain()
    {
        // When validation fails, the chain should NOT proceed to transform or route.
        var validationService = Substitute.For<IMessageValidationService>();
        var loggingService = Substitute.For<IMessageLoggingService>();

        var messageId = Guid.NewGuid();
        const string messageType = "invoice.received";
        const string badPayload = ""; // Empty payload → validation fails.

        validationService.ValidateAsync(messageType, badPayload)
            .Returns(MessageValidationResult.Failure("Payload is empty"));

        // Step 1: Validate — fails.
        var result = await validationService.ValidateAsync(messageType, badPayload);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Payload is empty"));

        // Chain stops — transform and route are never called.
        if (!result.IsValid)
        {
            await loggingService.LogAsync(messageId, messageType, "ValidationFailed");
        }

        // Verify: only the validation and failure log were called.
        await validationService.Received(1).ValidateAsync(messageType, badPayload);
        await loggingService.Received(1).LogAsync(messageId, messageType, "ValidationFailed");
        await loggingService.DidNotReceive().LogAsync(messageId, messageType, "Transformed");
        await loggingService.DidNotReceive().LogAsync(messageId, messageType, "Routed");
    }

    // ── Challenge 2: Cancellation Token Propagation ─────────────────────────

    [Test]
    public async Task Challenge2_CancellationToken_PropagatedToActivities()
    {
        // Temporal propagates a CancellationToken to each activity.  Verify
        // that our activity services honour the token — when cancelled, the
        // operation should throw OperationCanceledException.
        var persistenceService = Substitute.For<IPersistenceActivityService>();

        using var cts = new CancellationTokenSource();

        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "TestService",
            MessageType: "test.cancel",
            SchemaVersion: "1.0",
            Priority: 0,
            PayloadJson: "{}",
            MetadataJson: null,
            AckSubject: "ack.test",
            NackSubject: "nack.test");

        // Configure the mock to throw OperationCanceledException when the token is cancelled.
        persistenceService.SaveMessageAsync(input, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ct = callInfo.ArgAt<CancellationToken>(1);
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        // Cancel the token BEFORE calling the activity.
        cts.Cancel();

        // The activity should respect the cancellation token.
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await persistenceService.SaveMessageAsync(input, cts.Token);
        });
    }

    [Test]
    public async Task Challenge2_CancellationToken_NotCancelled_ActivityCompletes()
    {
        // When the token is NOT cancelled, the activity completes normally.
        var persistenceService = Substitute.For<IPersistenceActivityService>();

        using var cts = new CancellationTokenSource();

        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "TestService",
            MessageType: "test.normal",
            SchemaVersion: "1.0",
            Priority: 0,
            PayloadJson: "{\"data\": true}",
            MetadataJson: null,
            AckSubject: "ack.test",
            NackSubject: "nack.test");

        persistenceService.SaveMessageAsync(input, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Should complete without exception.
        await persistenceService.SaveMessageAsync(input, cts.Token);

        await persistenceService.Received(1).SaveMessageAsync(input, Arg.Any<CancellationToken>());
    }
}
