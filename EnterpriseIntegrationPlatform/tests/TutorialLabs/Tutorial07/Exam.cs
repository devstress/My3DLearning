// ============================================================================
// Tutorial 07 – Temporal Workflows (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply Temporal workflow patterns in realistic
//          scenarios — host-based DI wiring, failure handling, and
//          correlation propagation through the full pipeline dispatch.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Wire PipelineOrchestrator via DI and dispatch through Aspire host
//   🟡 Intermediate — Handle workflow failure result gracefully
//   🔴 Advanced     — Correlation and causation IDs propagated through dispatch
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockTemporalWorkflowDispatcher / AspireIntegrationTestHost
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
                svc.AddSingleton<ITemporalWorkflowDispatcher>(dispatcher);
                svc.Configure<PipelineOptions>(o => { o.AckSubject = "ack.di"; o.NackSubject = "nack.di"; });
                svc.AddSingleton<PipelineOrchestrator>();
            })
            .Build();

        var orchestrator = host.GetService<PipelineOrchestrator>();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"test\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "DIService", "di.test");

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
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsFailure("Validation failed");

        var options = Options.Create(new PipelineOptions());
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"bad\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "bad.type");

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
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "svc", "type", correlationId, causationId);

        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured!.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(captured.CausationId, Is.EqualTo(causationId));
    }
}
