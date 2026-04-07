// ============================================================================
// Tutorial 08 – Activities Pipeline (Lab)
// ============================================================================
// EIP Pattern: Pipes and Filters
// End-to-End: DefaultMessageValidationService for schema validation,
// IntegrationPipelineInput/Result record construction, multi-stage pipeline
// (Persist→Validate→Publish) with MockEndpoint and InvalidMessageChannel
// for DLQ routing on validation failure.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial08;

[TestFixture]
public sealed class Lab
{
    // ── 1. Validation Stage ─────────────────────────────────────────────

    [Test]
    public async Task ValidationStage_ValidJsonPayload_Succeeds()
    {
        // DefaultMessageValidationService checks that the payload is
        // non-empty valid JSON. This is the first gate in every pipeline.
        var validator = new DefaultMessageValidationService();

        var result = await validator.ValidateAsync("order.created", "{\"orderId\":\"ORD-1\"}");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    [Test]
    public async Task ValidationStage_EmptyPayload_FailsWithReason()
    {
        // Empty payloads are rejected immediately — no processing resources wasted.
        var validator = new DefaultMessageValidationService();

        var result = await validator.ValidateAsync("order.created", "");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Does.Contain("empty"));
    }

    [Test]
    public async Task ValidationStage_NonJsonPayload_FailsWithReason()
    {
        // Non-JSON payloads (plain text, XML, etc.) are rejected.
        var validator = new DefaultMessageValidationService();

        var result = await validator.ValidateAsync("order.created", "not-json");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Does.Contain("JSON"));
    }

    // ── 2. Pipeline Input/Result Records ────────────────────────────────

    [Test]
    public void IntegrationPipelineInput_RecordConstruction_AllFields()
    {
        // IntegrationPipelineInput is a positional record — use constructor
        // parameters, not init-only properties.
        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(), CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(), Timestamp: DateTimeOffset.UtcNow,
            Source: "OrderService", MessageType: "order.created",
            SchemaVersion: "1.0", Priority: 2,
            PayloadJson: "{\"id\":1}", MetadataJson: "{\"tenant\":\"acme\"}",
            AckSubject: "ack.orders", NackSubject: "nack.orders",
            NotificationsEnabled: true);

        Assert.That(input.Source, Is.EqualTo("OrderService"));
        Assert.That(input.Priority, Is.EqualTo(2));
        Assert.That(input.NotificationsEnabled, Is.True);
        Assert.That(input.AckSubject, Is.EqualTo("ack.orders"));
    }

    [Test]
    public void IntegrationPipelineResult_SuccessAndFailure_RecordSemantics()
    {
        // IntegrationPipelineResult reports the outcome of workflow execution.
        var success = new IntegrationPipelineResult(Guid.NewGuid(), IsSuccess: true);
        var failure = new IntegrationPipelineResult(
            Guid.NewGuid(), IsSuccess: false, FailureReason: "Validation failed");

        Assert.That(success.IsSuccess, Is.True);
        Assert.That(success.FailureReason, Is.Null);
        Assert.That(failure.IsSuccess, Is.False);
        Assert.That(failure.FailureReason, Is.EqualTo("Validation failed"));
    }

    // ── 3. Multi-Stage Pipeline ─────────────────────────────────────────

    [Test]
    public async Task PipelineChain_ValidateAndPublish_EndToEnd()
    {
        // Two-stage pipeline: Validate → Publish.
        // Valid messages flow through to the output channel.
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("output");

        var envelope = IntegrationEnvelope<string>.Create(
            "{\"item\":\"widget\"}", "FactoryService", "factory.produced");

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);
        Assert.That(result.IsValid, Is.True);

        var channel = new PointToPointChannel(
            output, output, NullLogger<PointToPointChannel>.Instance);
        await channel.SendAsync(envelope, "validated-queue", CancellationToken.None);

        output.AssertReceivedCount(1);
        Assert.That(output.GetReceived<string>().Payload, Does.Contain("widget"));
    }

    [Test]
    public async Task PipelineChain_ValidationFails_RoutesToInvalidChannel()
    {
        // Failed validation routes the message to the Invalid Message Channel
        // (dead letter queue) instead of the normal output.
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("invalid-output");

        var envelope = IntegrationEnvelope<string>.Create(
            "bad-data", "LegacySystem", "legacy.event");

        var result = await validator.ValidateAsync(
            envelope.MessageType, envelope.Payload);
        Assert.That(result.IsValid, Is.False);

        var invalidOptions = Options.Create(new InvalidMessageChannelOptions
            { InvalidMessageTopic = "invalid-msgs", Source = "Pipeline" });
        var invalidChannel = new InvalidMessageChannel(
            output, invalidOptions, NullLogger<InvalidMessageChannel>.Instance);

        await invalidChannel.RouteInvalidAsync(
            envelope, result.Reason!, CancellationToken.None);

        output.AssertReceivedCount(1);
        output.AssertReceivedOnTopic("invalid-msgs", 1);
    }

    [Test]
    public async Task PipelineChain_PersistValidatePublish_ThreeStages()
    {
        // Three-stage pipeline: Persist → Validate → Publish.
        // Each stage is exercised and verified independently.
        var persistence = new MockPersistenceActivityService();
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("pipeline-out");

        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(), CorrelationId: Guid.NewGuid(),
            CausationId: null, Timestamp: DateTimeOffset.UtcNow,
            Source: "Lab08", MessageType: "lab.event", SchemaVersion: "1.0",
            Priority: 1, PayloadJson: "{\"data\":true}", MetadataJson: null,
            AckSubject: "ack", NackSubject: "nack");

        // Stage 1: Persist
        await persistence.SaveMessageAsync(input);
        persistence.AssertSaveCount(1);

        // Stage 2: Validate
        var validation = await validator.ValidateAsync(input.MessageType, input.PayloadJson);
        Assert.That(validation.IsValid, Is.True);

        // Stage 3: Publish
        var envelope = IntegrationEnvelope<string>.Create(
            input.PayloadJson, input.Source, input.MessageType);
        await output.PublishAsync(envelope, "processed-topic");

        output.AssertReceivedCount(1);
        output.AssertReceivedOnTopic("processed-topic", 1);
    }

    [Test]
    public async Task PipelineChain_PersistValidateLogPublish_FourStages()
    {
        // Four-stage pipeline: Persist → Validate → Log → Publish.
        // MockMessageLoggingService tracks audit entries per MessageId.
        var persistence = new MockPersistenceActivityService();
        var logging = new MockMessageLoggingService();
        var validator = new DefaultMessageValidationService();
        await using var output = new MockEndpoint("audited");

        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(), CorrelationId: Guid.NewGuid(),
            CausationId: null, Timestamp: DateTimeOffset.UtcNow,
            Source: "AuditService", MessageType: "audit.event", SchemaVersion: "1.0",
            Priority: 1, PayloadJson: "{\"audit\":true}", MetadataJson: null,
            AckSubject: "ack", NackSubject: "nack");

        await persistence.SaveMessageAsync(input);
        var validation = await validator.ValidateAsync(input.MessageType, input.PayloadJson);
        Assert.That(validation.IsValid, Is.True);
        await logging.LogAsync(input.MessageId, input.MessageType, "Validated");

        var envelope = IntegrationEnvelope<string>.Create(
            input.PayloadJson, input.Source, input.MessageType);
        await output.PublishAsync(envelope, "audited-topic");

        output.AssertReceivedCount(1);
        logging.AssertLogged(input.MessageId, "Validated");
    }
}
