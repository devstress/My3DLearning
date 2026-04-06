// ============================================================================
// Tutorial 14 – Process Manager (Lab)
// ============================================================================
// This lab exercises the PipelineOrchestrator — the Process Manager pattern
// that converts an IntegrationEnvelope into an IntegrationPipelineInput and
// dispatches it to a Temporal workflow. You will verify input mapping, mock
// the Temporal dispatcher, and validate success/failure paths.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial14;

[TestFixture]
public sealed class Lab
{
    // ── Successful Dispatch — Workflow Returns Success ───────────────────────

    [Test]
    public async Task Process_SuccessfulWorkflow_CompletesWithoutError()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci => new IntegrationPipelineResult(
                ci.ArgAt<IntegrationPipelineInput>(0).MessageId,
                IsSuccess: true));

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "integration.ack",
            NackSubject = "integration.nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>(
            """{"orderId": "ORD-1", "amount": 100}""");

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "OrderService", "order.created");

        // Should complete without throwing.
        await orchestrator.ProcessAsync(envelope);

        // Verify the dispatcher was called exactly once.
        await dispatcher.Received(1).DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Input Mapping — Envelope Fields Map to Pipeline Input ────────────────

    [Test]
    public async Task Process_InputMapping_AllFieldsCorrectlyMapped()
    {
        IntegrationPipelineInput? capturedInput = null;

        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedInput = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(capturedInput.MessageId, IsSuccess: true);
            });

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "test.ack",
            NackSubject = "test.nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>(
            """{"key": "value"}""");

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "TestService", "test.event") with
        {
            Priority = MessagePriority.High,
            SchemaVersion = "2.0",
            Metadata = new Dictionary<string, string>
            {
                ["tenant"] = "acme",
            },
        };

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(capturedInput.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(capturedInput.Source, Is.EqualTo("TestService"));
        Assert.That(capturedInput.MessageType, Is.EqualTo("test.event"));
        Assert.That(capturedInput.SchemaVersion, Is.EqualTo("2.0"));
        Assert.That(capturedInput.Priority, Is.EqualTo((int)MessagePriority.High));
        Assert.That(capturedInput.AckSubject, Is.EqualTo("test.ack"));
        Assert.That(capturedInput.NackSubject, Is.EqualTo("test.nack"));
        Assert.That(capturedInput.PayloadJson, Does.Contain("value"));
        Assert.That(capturedInput.MetadataJson, Does.Contain("acme"));
    }

    // ── Workflow ID Derived from MessageId ───────────────────────────────────

    [Test]
    public async Task Process_WorkflowId_DerivedFromMessageId()
    {
        string? capturedWorkflowId = null;

        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedWorkflowId = ci.ArgAt<string>(1);
                var input = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(input.MessageId, IsSuccess: true);
            });

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Service", "event.type");

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedWorkflowId, Is.Not.Null);
        Assert.That(capturedWorkflowId, Is.EqualTo($"integration-{envelope.MessageId}"));
    }

    // ── Failed Workflow — Completes Without Throwing ─────────────────────────

    [Test]
    public async Task Process_FailedWorkflow_CompletesWithoutThrowing()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci => new IntegrationPipelineResult(
                ci.ArgAt<IntegrationPipelineInput>(0).MessageId,
                IsSuccess: false,
                FailureReason: "Validation failed"));

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Service", "event.type");

        // ProcessAsync should not throw even when the workflow fails.
        Assert.DoesNotThrowAsync(() => orchestrator.ProcessAsync(envelope));
    }

    // ── IntegrationPipelineInput Record Shape ───────────────────────────────

    [Test]
    public void PipelineInput_Record_HasExpectedProperties()
    {
        // Verify the IntegrationPipelineInput record has the expected shape.
        var input = new IntegrationPipelineInput(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            Timestamp: DateTimeOffset.UtcNow,
            Source: "TestSource",
            MessageType: "test.type",
            SchemaVersion: "1.0",
            Priority: 0,
            PayloadJson: "{}",
            MetadataJson: null,
            AckSubject: "ack",
            NackSubject: "nack");

        Assert.That(input.Source, Is.EqualTo("TestSource"));
        Assert.That(input.MessageType, Is.EqualTo("test.type"));
        Assert.That(input.PayloadJson, Is.EqualTo("{}"));
        Assert.That(input.MetadataJson, Is.Null);
        Assert.That(input.NotificationsEnabled, Is.False);
    }

    // ── IntegrationPipelineResult Record Shape ──────────────────────────────

    [Test]
    public void PipelineResult_Success_HasCorrectProperties()
    {
        var messageId = Guid.NewGuid();
        var result = new IntegrationPipelineResult(messageId, IsSuccess: true);

        Assert.That(result.MessageId, Is.EqualTo(messageId));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.FailureReason, Is.Null);
    }

    [Test]
    public void PipelineResult_Failure_HasReasonPopulated()
    {
        var messageId = Guid.NewGuid();
        var result = new IntegrationPipelineResult(
            messageId, IsSuccess: false, FailureReason: "Timeout exceeded");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Timeout exceeded"));
    }
}
