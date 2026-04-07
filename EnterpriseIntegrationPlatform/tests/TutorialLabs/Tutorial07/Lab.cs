// ============================================================================
// Tutorial 07 – Temporal Workflows (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how Temporal workflow configuration,
//          orchestrator dispatch, and failure handling work through
//          MockTemporalWorkflowDispatcher. Read the code and comments to
//          understand each concept before moving to the Exam.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. TemporalOptions defaults — section name, task queue, namespace
//   2. Workflow type discovery — four workflow types in the assembly
//   3. PipelineOptions defaults — Ack/Nack subject configuration
//   4. Orchestrator dispatch — envelope converted to pipeline input
//   5. Workflow ID generation — deterministic from MessageId
//   6. Payload serialization — JSON strings for Temporal replay
//   7. Priority mapping — enum to integer for Temporal serialization
//   8. Failure handling — dispatch failure handled without throwing
//   9. Dispatch tracking — multiple dispatches counted for verification
//
// INFRASTRUCTURE: MockTemporalWorkflowDispatcher
// ============================================================================

using System.Reflection;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Workflow.Temporal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial07;

[TestFixture]
public sealed class Lab
{
    // ── 1. Temporal Configuration & Workflow Discovery ───────────────────

    [Test]
    public void TemporalOptions_Defaults_TaskQueueAndNamespace()
    {
        // TemporalOptions binds to the "Temporal" appsettings section.
        // Defaults: NATS-free local dev with standard task queue.
        var options = new TemporalOptions();

        Assert.That(TemporalOptions.SectionName, Is.EqualTo("Temporal"));
        Assert.That(options.TaskQueue, Is.EqualTo("integration-workflows"));
        Assert.That(options.Namespace, Is.EqualTo("default"));
        Assert.That(options.ServerAddress, Is.EqualTo("localhost:15233"));
    }

    [Test]
    public void WorkflowTypes_AllFourExistInAssembly()
    {
        // The platform ships four workflow types for different orchestration patterns:
        // 1. ProcessIntegrationMessageWorkflow — basic message processing
        // 2. IntegrationPipelineWorkflow — Persist→Validate→Deliver/Fault→Ack/Nack
        // 3. AtomicPipelineWorkflow — compensate on failure
        // 4. SagaCompensationWorkflow — multi-step saga with compensating transactions
        var assembly = typeof(TemporalOptions).Assembly;
        var types = assembly.GetTypes().Select(t => t.Name).ToHashSet();

        Assert.That(types, Does.Contain("ProcessIntegrationMessageWorkflow"));
        Assert.That(types, Does.Contain("IntegrationPipelineWorkflow"));
        Assert.That(types, Does.Contain("AtomicPipelineWorkflow"));
        Assert.That(types, Does.Contain("SagaCompensationWorkflow"));
    }

    [Test]
    public void PipelineOptions_Defaults_AckNackSubjects()
    {
        // PipelineOptions configures where the pipeline sends Ack/Nack
        // notifications after successful or failed processing.
        var options = new PipelineOptions();

        Assert.That(options.AckSubject, Is.Not.Null);
        Assert.That(options.NackSubject, Is.Not.Null);
    }

    // ── 2. PipelineOrchestrator Dispatch ─────────────────────────────────

    [Test]
    public async Task PipelineOrchestrator_DispatchesCorrectInput()
    {
        // PipelineOrchestrator converts an IntegrationEnvelope<JsonElement>
        // into IntegrationPipelineInput and dispatches to Temporal.
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        var options = Options.Create(new PipelineOptions
            { AckSubject = "ack.test", NackSubject = "nack.test" });
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"orderId\":\"ORD-1\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "OrderService", "order.created");

        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Source, Is.EqualTo("OrderService"));
        Assert.That(captured.MessageType, Is.EqualTo("order.created"));
        Assert.That(captured.AckSubject, Is.EqualTo("ack.test"));
        Assert.That(captured.NackSubject, Is.EqualTo("nack.test"));
    }

    [Test]
    public async Task PipelineOrchestrator_WorkflowId_DerivedFromMessageId()
    {
        // Workflow ID = "integration-{MessageId}" — deterministic and idempotent.
        // Re-dispatching the same message won't create duplicate workflows.
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();
        var orchestrator = new PipelineOrchestrator(
            dispatcher, Options.Create(new PipelineOptions()),
            NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "type");

        await orchestrator.ProcessAsync(envelope);

        Assert.That(dispatcher.LastWorkflowId,
            Is.EqualTo($"integration-{envelope.MessageId}"));
    }

    [Test]
    public async Task PipelineOrchestrator_SerializesPayloadAndMetadata()
    {
        // The orchestrator serializes both payload and metadata to JSON strings
        // for Temporal workflow input — enabling replay and inspection.
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();
        var orchestrator = new PipelineOrchestrator(
            dispatcher, Options.Create(new PipelineOptions()),
            NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"key\":\"value\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "type") with
        {
            Metadata = new Dictionary<string, string> { ["tenant"] = "acme" },
        };

        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured!.PayloadJson, Does.Contain("key"));
        Assert.That(captured.MetadataJson, Does.Contain("tenant"));
    }

    [Test]
    public async Task PipelineOrchestrator_MapsPriorityAsInt()
    {
        // MessagePriority enum maps to an integer in IntegrationPipelineInput
        // for Temporal's serialization format.
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();
        var orchestrator = new PipelineOrchestrator(
            dispatcher, Options.Create(new PipelineOptions()),
            NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "type") with
        {
            Priority = MessagePriority.High,
        };

        await orchestrator.ProcessAsync(envelope);

        Assert.That(dispatcher.LastInput!.Priority,
            Is.EqualTo((int)MessagePriority.High));
    }

    // ── 3. Failure Handling & Dispatch Count ─────────────────────────────

    [Test]
    public async Task PipelineOrchestrator_DispatchFailure_HandledGracefully()
    {
        // When Temporal returns a failure result, the orchestrator logs
        // but doesn't throw — the message is not lost.
        var dispatcher = new MockTemporalWorkflowDispatcher()
            .ReturnsFailure("Validation failed: empty payload");
        var orchestrator = new PipelineOrchestrator(
            dispatcher, Options.Create(new PipelineOptions()),
            NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"bad\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "bad.type");

        await orchestrator.ProcessAsync(envelope);

        dispatcher.AssertDispatchCount(1);
        Assert.That(dispatcher.LastInput!.MessageType, Is.EqualTo("bad.type"));
    }

    [Test]
    public async Task PipelineOrchestrator_MultipleDispatches_CountTracked()
    {
        // MockTemporalWorkflowDispatcher tracks all dispatches for assertions.
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();
        var orchestrator = new PipelineOrchestrator(
            dispatcher, Options.Create(new PipelineOptions()),
            NullLogger<PipelineOrchestrator>.Instance);

        for (var i = 0; i < 3; i++)
        {
            var json = JsonSerializer.Deserialize<JsonElement>($"{{\"i\":{i}}}");
            var env = IntegrationEnvelope<JsonElement>.Create(json, "svc", "batch");
            await orchestrator.ProcessAsync(env);
        }

        dispatcher.AssertDispatchCount(3);
    }
}
