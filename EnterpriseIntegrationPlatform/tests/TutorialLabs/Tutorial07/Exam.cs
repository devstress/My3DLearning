// ============================================================================
// Tutorial 07 – Temporal Workflows (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Wire PipelineOrchestrator via DI and dispatch through Aspire host
//   🟡 Intermediate — Handle workflow failure result gracefully
//   🔴 Advanced     — Correlation and causation IDs propagated through dispatch
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial07;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — DI-Wired Orchestrator Dispatch ───────────────────────
    //
    // SCENARIO: A microservice host registers PipelineOrchestrator through
    // dependency injection with configured Ack/Nack subjects. An incoming
    // order message must be dispatched through the DI-resolved orchestrator.
    //
    // WHAT YOU PROVE: You can wire PipelineOrchestrator via DI in an
    // AspireIntegrationTestHost and dispatch messages through the pipeline.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Starter_AspireHost_OrchestratorDispatchesViaDI()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        await using var host = AspireIntegrationTestHost.CreateBuilder()
            .ConfigureServices(svc =>
            {
                // TODO: Register the dispatcher as ITemporalWorkflowDispatcher singleton
                // TODO: Configure PipelineOptions with AckSubject = "ack.di" and NackSubject = "nack.di"
                // TODO: Register PipelineOrchestrator as a singleton
            })
            .Build();

        var orchestrator = host.GetService<PipelineOrchestrator>();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"test\":true}");
        // TODO: Create an IntegrationEnvelope<JsonElement> with payload json, source "DIService", type "di.test"
        IntegrationEnvelope<JsonElement> envelope = null!; // ← replace with IntegrationEnvelope<JsonElement>.Create(...)

        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Source, Is.EqualTo("DIService"));
        Assert.That(captured.AckSubject, Is.EqualTo("ack.di"));
    }

    // ── 🟡 INTERMEDIATE — Workflow Failure Handling ────────────────────────
    //
    // SCENARIO: A Temporal workflow returns a failure result due to validation
    // errors. The orchestrator must handle this gracefully — logging the
    // failure without throwing — so the message is not lost.
    //
    // WHAT YOU PROVE: You can handle workflow failure results without
    // propagating exceptions, keeping the message pipeline resilient.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Intermediate_WorkflowFailure_LogsWarning()
    {
        // TODO: Create a MockTemporalWorkflowDispatcher that returns failure with "Validation failed"
        MockTemporalWorkflowDispatcher dispatcher = null!; // ← replace with new MockTemporalWorkflowDispatcher().ReturnsFailure(...)

        // TODO: Create PipelineOptions via Options.Create
        // TODO: Create a PipelineOrchestrator with dispatcher, options, and NullLogger
        PipelineOrchestrator orchestrator = null!; // ← replace with new PipelineOrchestrator(dispatcher, Options.Create(new PipelineOptions()), NullLogger<PipelineOrchestrator>.Instance)

        var json = JsonSerializer.Deserialize<JsonElement>("{\"bad\":true}");
        // TODO: Create an IntegrationEnvelope<JsonElement> with payload json, source "svc", type "bad.type"
        IntegrationEnvelope<JsonElement> envelope = null!; // ← replace with IntegrationEnvelope<JsonElement>.Create(...)

        await orchestrator.ProcessAsync(envelope);

        dispatcher.AssertDispatchCount(1);
        Assert.That(dispatcher.LastInput!.MessageType, Is.EqualTo("bad.type"));
    }

    // ── 🔴 ADVANCED — Correlation and Causation Propagation ────────────────
    //
    // SCENARIO: A distributed order processing system propagates correlation
    // and causation IDs through every stage. When the orchestrator dispatches
    // to Temporal, both IDs must appear in the workflow input for tracing.
    //
    // WHAT YOU PROVE: You can propagate correlation and causation IDs from
    // the integration envelope through to the Temporal workflow input.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Advanced_CorrelationAndCausation_PropagatedToInput()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        var options = Options.Create(new PipelineOptions());
        // TODO: Create a PipelineOrchestrator with dispatcher, options, and NullLogger
        PipelineOrchestrator orchestrator = null!; // ← replace with new PipelineOrchestrator(...)

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        // TODO: Create an IntegrationEnvelope<JsonElement> with json, "svc", "type", correlationId, causationId
        IntegrationEnvelope<JsonElement> envelope = null!; // ← replace with IntegrationEnvelope<JsonElement>.Create(...)

        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured!.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(captured.CausationId, Is.EqualTo(causationId));
    }
}
#endif
