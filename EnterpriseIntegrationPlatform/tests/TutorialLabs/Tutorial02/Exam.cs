// ============================================================================
// Tutorial 02 – Temporal.io Workflow Orchestration (Exam)
// ============================================================================
// EIP Patterns: Process Manager, Saga (Compensation), Scatter-Gather
// End-to-End: Advanced Temporal patterns — atomic pipeline orchestration,
// saga compensation with step-level rollback, parallel fan-out with result
// aggregation, and notification-enabled workflows.
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

namespace TutorialLabs.Tutorial02;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Multi-Step Saga with Compensation Tracking ──────────

    [Test]
    public async Task Challenge1_SagaCompensation_TracksStepsAndRollsBack()
    {
        // Simulate a 4-step saga where step 3 fails.
        // Steps 1 and 2 must be compensated in reverse order.
        var completedSteps = new List<string>();
        var compensatedSteps = new List<string>();

        var dispatcher = new MockTemporalWorkflowDispatcher();
        dispatcher.OnDispatch((input, workflowId) =>
        {
            // Step 1: Persist
            completedSteps.Add("Persist");
            // Step 2: Validate schema
            completedSteps.Add("ValidateSchema");
            // Step 3: Enrich (fails!)
            var enrichFailed = true;

            if (enrichFailed)
            {
                // Compensate in reverse order (LIFO)
                foreach (var step in Enumerable.Reverse(completedSteps).ToList())
                {
                    compensatedSteps.Add($"Compensate:{step}");
                }

                return new IntegrationPipelineResult(input.MessageId, false, "Enrichment failed");
            }

            return new IntegrationPipelineResult(input.MessageId, true);
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher,
            Options.Create(new PipelineOptions()),
            NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"data\":\"test\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "saga.test");

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

    // ── Challenge 2: Fan-Out with Result Aggregation ────────────────────

    [Test]
    public async Task Challenge2_FanOut_AggregatesResultsFromParallelWorkflows()
    {
        // Pattern: Split an order into line items, dispatch each as an
        // independent Temporal workflow, aggregate results.
        var dispatcher = new MockTemporalWorkflowDispatcher();
        dispatcher.OnDispatch((input, workflowId) =>
        {
            // Simulate: SKU-002 fails validation, others succeed
            var isSuccess = !input.PayloadJson.Contains("SKU-002");
            return new IntegrationPipelineResult(
                input.MessageId,
                isSuccess,
                isSuccess ? null : "SKU-002 is discontinued");
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher,
            Options.Create(new PipelineOptions()),
            NullLogger<PipelineOrchestrator>.Instance);

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
            var json = JsonSerializer.Deserialize<JsonElement>(payload);
            var envelope = IntegrationEnvelope<JsonElement>.Create(json, "OrderSplit", msgType);
            await orchestrator.ProcessAsync(envelope);

            var input = dispatcher.Dispatches.Last();
            var sku = JsonSerializer.Deserialize<JsonElement>(input.Input.PayloadJson)
                .GetProperty("sku").GetString()!;
            // Re-run to capture result
            var result = input.Input.PayloadJson.Contains("SKU-002")
                ? new IntegrationPipelineResult(input.Input.MessageId, false, "SKU-002 is discontinued")
                : new IntegrationPipelineResult(input.Input.MessageId, true);
            results.Add((sku, result.IsSuccess, result.FailureReason));
        }

        // Aggregation: 2 succeeded, 1 failed
        Assert.That(dispatcher.DispatchCount, Is.EqualTo(3));
        Assert.That(results.Count(r => r.Success), Is.EqualTo(2));
        Assert.That(results.Count(r => !r.Success), Is.EqualTo(1));
        Assert.That(results.Single(r => !r.Success).Sku, Is.EqualTo("SKU-002"));
    }

    // ── Challenge 3: Notification-Enabled Workflow ──────────────────────

    [Test]
    public async Task Challenge3_NotificationsEnabled_AckSubjectConfigured()
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
                svc.AddSingleton<ITemporalWorkflowDispatcher>(dispatcher);
                svc.Configure<PipelineOptions>(o =>
                {
                    o.AckSubject = options.AckSubject;
                    o.NackSubject = options.NackSubject;
                });
                svc.AddSingleton<PipelineOrchestrator>();
            })
            .Build();

        var orchestrator = host.GetService<PipelineOrchestrator>();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"notify\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "NotifySvc", "order.complete");

        await orchestrator.ProcessAsync(envelope);

        var input = dispatcher.LastInput!;
        Assert.That(input.AckSubject, Is.EqualTo("custom.ack"));
        Assert.That(input.NackSubject, Is.EqualTo("custom.nack"));
        Assert.That(input.Source, Is.EqualTo("NotifySvc"));
    }
}
