// ============================================================================
// Tutorial 02 – Temporal.io Workflow Orchestration (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Multi-step saga with LIFO compensation tracking
//   🟡 Intermediate — Fan-out with per-workflow success/failure aggregation
//   🔴 Advanced     — Notification-enabled workflow with custom Ack/Nack via DI
// ============================================================================

using System.Text.Json;
using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

#pragma warning disable CS0219 // Variable is assigned but its value is never used (expected in fill-in-blank exam)

namespace TutorialLabs.Tutorial02;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Multi-Step Saga with Compensation Tracking ────────
    //
    // SCENARIO: A 4-step integration pipeline processes an incoming message.
    // Step 3 (Enrich) fails after steps 1 (Persist) and 2 (ValidateSchema)
    // have completed. The saga must compensate completed steps in reverse
    // order (LIFO) to maintain data consistency.
    //
    // WHAT YOU PROVE: You understand saga compensation — forward steps are
    // tracked, and on failure, completed steps are rolled back in reverse.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_SagaCompensation_TracksStepsAndRollsBack()
    {
        // Simulate a 4-step saga where step 3 fails.
        // Steps 1 and 2 must be compensated in reverse order.
        var completedSteps = new List<string>();
        var compensatedSteps = new List<string>();

        var dispatcher = new MockTemporalWorkflowDispatcher();
        dispatcher.OnDispatch((input, workflowId) =>
        {
            // TODO: Implement the saga steps:
            //   1. Add "Persist" to completedSteps
            //   2. Add "ValidateSchema" to completedSteps
            //   3. Simulate enrichFailed = true
            //   4. If enrichFailed, compensate in LIFO order by iterating
            //      Enumerable.Reverse(completedSteps) and adding $"Compensate:{step}"
            //      to compensatedSteps
            //   5. Return IntegrationPipelineResult with success=false, reason="Enrichment failed"
            //      (or success=true if not failed)
            return new IntegrationPipelineResult(input.MessageId, true);
        });

        // TODO: Create a PipelineOrchestrator with the dispatcher, default PipelineOptions, and NullLogger.
        PipelineOrchestrator orchestrator = null!; // ← replace with new PipelineOrchestrator(...)

        var json = JsonSerializer.Deserialize<JsonElement>("{\"data\":\"test\"}");

        // TODO: Create an IntegrationEnvelope<JsonElement> with payload=json, source="svc", type="saga.test"
        IntegrationEnvelope<JsonElement> envelope = null!; // ← replace with IntegrationEnvelope<JsonElement>.Create(...)

        await orchestrator.ProcessAsync(envelope);

        // Completed steps tracked in forward order
        Assert.That(completedSteps, Has.Count.EqualTo(2));
        Assert.That(completedSteps[0], Is.EqualTo("Persist"));
        Assert.That(completedSteps[1], Is.EqualTo("ValidateSchema"));

        // Compensation executed in reverse order
        Assert.That(compensatedSteps, Has.Count.EqualTo(2));
        Assert.That(compensatedSteps[0], Is.EqualTo("Compensate:ValidateSchema"));
        Assert.That(compensatedSteps[1], Is.EqualTo("Compensate:Persist"));
    }

    // ── 🟡 INTERMEDIATE — Fan-Out with Result Aggregation ──────────────
    //
    // SCENARIO: An order containing multiple line items is split into
    // independent Temporal workflows — one per SKU. Each workflow validates
    // its SKU independently (SKU-002 is discontinued and fails). Results
    // are aggregated to determine overall order status.
    //
    // WHAT YOU PROVE: You can fan out messages into parallel workflows,
    // handle mixed success/failure results, and aggregate outcomes.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_FanOut_AggregatesResultsFromParallelWorkflows()
    {
        // Pattern: Split an order into line items, dispatch each as an
        // independent Temporal workflow, aggregate results.
        var dispatcher = new MockTemporalWorkflowDispatcher();
        dispatcher.OnDispatch((input, workflowId) =>
        {
            // TODO: Simulate SKU validation — if input.PayloadJson contains "SKU-002"
            //       return failure with reason "SKU-002 is discontinued", otherwise success.
            return new IntegrationPipelineResult(input.MessageId, true);
        });

        // TODO: Create a PipelineOrchestrator with the dispatcher, default PipelineOptions, and NullLogger.
        PipelineOrchestrator orchestrator = null!; // ← replace with new PipelineOrchestrator(...)

        var orderLines = new[]
        {
            ("{\"sku\":\"SKU-001\",\"qty\":2}", "line.001"),
            ("{\"sku\":\"SKU-002\",\"qty\":1}", "line.002"),
            ("{\"sku\":\"SKU-003\",\"qty\":3}", "line.003"),
        };

        // Fan-out: process each line independently
        var results = new List<(string Sku, bool Success, string? Reason)>();
        foreach (var (payload, msgType) in orderLines)
        {
            // TODO: Deserialize payload to JsonElement, create an IntegrationEnvelope<JsonElement>
            //       with source "OrderSplit" and msgType, call orchestrator.ProcessAsync,
            //       then extract the SKU from dispatcher.Dispatches.Last() and build the result.
            //       Add (sku, result.IsSuccess, result.FailureReason) to results.
        }

        // Aggregation: 2 succeeded, 1 failed
        Assert.That(dispatcher.DispatchCount, Is.EqualTo(3));
        Assert.That(results.Count(r => r.Success), Is.EqualTo(2));
        Assert.That(results.Count(r => !r.Success), Is.EqualTo(1));
        Assert.That(results.Single(r => !r.Success).Sku, Is.EqualTo("SKU-002"));
    }

    // ── 🔴 ADVANCED — Notification-Enabled Workflow via DI ─────────────
    //
    // SCENARIO: A notification service completes an order and publishes
    // Ack/Nack to custom NATS subjects configured via DI. The full
    // AspireIntegrationTestHost DI container must wire PipelineOrchestrator
    // with the mock dispatcher and custom PipelineOptions.
    //
    // WHAT YOU PROVE: You can configure notification-enabled workflows
    // end-to-end through DI, with custom Ack/Nack subject routing.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_NotificationsEnabled_AckSubjectConfigured()
    {
        // When NotificationsEnabled=true, the workflow publishes Ack/Nack
        // to configurable NATS subjects. This tests the subject wiring.
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        var options = new PipelineOptions
        {
            AckSubject = "custom.ack",
            NackSubject = "custom.nack",
        };

        await using var host = AspireIntegrationTestHost.CreateBuilder()
            .ConfigureServices(svc =>
            {
                // TODO: Register the following services in the DI container:
                //   1. AddSingleton<ITemporalWorkflowDispatcher>(dispatcher)
                //   2. Configure<PipelineOptions> — set AckSubject and NackSubject from options
                //   3. AddSingleton<PipelineOrchestrator>()
            })
            .Build();

        var orchestrator = host.GetService<PipelineOrchestrator>();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"notify\":true}");

        // TODO: Create an IntegrationEnvelope<JsonElement> with payload=json, source="NotifySvc", type="order.complete"
        IntegrationEnvelope<JsonElement> envelope = null!; // ← replace with IntegrationEnvelope<JsonElement>.Create(...)

        await orchestrator.ProcessAsync(envelope);

        var input = dispatcher.LastInput!;
        Assert.That(input.AckSubject, Is.EqualTo("custom.ack"));
        Assert.That(input.NackSubject, Is.EqualTo("custom.nack"));
        Assert.That(input.Source, Is.EqualTo("NotifySvc"));
    }
}
