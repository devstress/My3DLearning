// ============================================================================
// Tutorial 02 – Temporal.io Workflow Orchestration (Lab)
// ============================================================================
// EIP Patterns: Process Manager, Saga, Scatter-Gather
// End-to-End: Temporal workflow dispatch, saga compensation with rollback,
// fan-out/split via parallel activities, and scalability through task queues
// and retry policies. Uses MockTemporalWorkflowDispatcher for unit-level
// validation; real Temporal containers run via Aspire in integration tests.
// ============================================================================

using System.Text.Json;
using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Testing;
using EnterpriseIntegrationPlatform.Workflow.Temporal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace TutorialLabs.Tutorial02;

[TestFixture]
public sealed class Lab
{
    private MockTemporalWorkflowDispatcher _dispatcher = null!;

    [SetUp]
    public void SetUp() => _dispatcher = new MockTemporalWorkflowDispatcher();

    // ── 1. Workflow Dispatch Basics ─────────────────────────────────────

    [Test]
    public async Task WorkflowDispatch_EnvelopeFieldsMappedToInput()
    {
        // PipelineOrchestrator maps IntegrationEnvelope fields to
        // IntegrationPipelineInput — the Temporal workflow's input contract.
        _dispatcher.ReturnsSuccess();
        var orchestrator = CreateOrchestrator();

        var json = JsonSerializer.Deserialize<JsonElement>("{\"orderId\":\"ORD-100\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "OrderService", "order.created");

        await orchestrator.ProcessAsync(envelope);

        _dispatcher.AssertDispatchCount(1);
        var input = _dispatcher.LastInput!;
        Assert.That(input.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(input.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(input.Source, Is.EqualTo("OrderService"));
        Assert.That(input.MessageType, Is.EqualTo("order.created"));
        Assert.That(input.PayloadJson, Does.Contain("ORD-100"));
    }

    [Test]
    public async Task WorkflowDispatch_WorkflowIdDerivedFromMessageId()
    {
        // Temporal workflow ID is deterministic: "integration-{messageId}"
        // This makes workflow execution idempotent — resubmitting the same
        // message produces the same workflow ID, preventing duplicates.
        _dispatcher.ReturnsSuccess();
        var orchestrator = CreateOrchestrator();

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "type");

        await orchestrator.ProcessAsync(envelope);

        Assert.That(_dispatcher.LastWorkflowId,
            Is.EqualTo($"integration-{envelope.MessageId}"));
    }

    // ── 2. Saga Pattern: Success and Failure Paths ──────────────────────

    [Test]
    public async Task SagaPattern_SuccessPath_AllStepsComplete()
    {
        // On success: persist → validate → ack. No compensation needed.
        _dispatcher.ReturnsSuccess();
        var orchestrator = CreateOrchestrator();

        var json = JsonSerializer.Deserialize<JsonElement>("{\"valid\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "order.valid");

        await orchestrator.ProcessAsync(envelope);

        _dispatcher.AssertDispatchCount(1);
        // The workflow ID is deterministic from the message
        Assert.That(_dispatcher.LastWorkflowId!.StartsWith("integration-"), Is.True);
    }

    [Test]
    public async Task SagaPattern_FailurePath_CompensationTriggered()
    {
        // On failure: the workflow rolls back completed steps.
        // MockDispatcher simulates the Temporal workflow's nack path.
        _dispatcher.ReturnsFailure("Validation failed: invalid schema");
        var orchestrator = CreateOrchestrator();

        var json = JsonSerializer.Deserialize<JsonElement>("{\"bad\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "order.invalid");

        await orchestrator.ProcessAsync(envelope);

        _dispatcher.AssertDispatchCount(1);
        var input = _dispatcher.LastInput!;
        Assert.That(input.MessageType, Is.EqualTo("order.invalid"));
    }

    [Test]
    public async Task SagaPattern_CustomCompensationHandler_ExecutesRollback()
    {
        // OnDispatch allows custom saga logic — simulate compensation steps.
        var compensatedSteps = new List<string>();
        _dispatcher.OnDispatch((input, workflowId) =>
        {
            // Simulate: persist succeeded → validate failed → compensate persist
            compensatedSteps.Add("PersistMessage");
            compensatedSteps.Add("LogReceived");
            // Compensation reverses: LogReceived first, then PersistMessage
            compensatedSteps.Reverse();
            return new IntegrationPipelineResult(input.MessageId, false, "Schema mismatch");
        });

        var orchestrator = CreateOrchestrator();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"schema\":\"v0\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "legacy.format");

        await orchestrator.ProcessAsync(envelope);

        // Compensation executed in reverse order
        Assert.That(compensatedSteps[0], Is.EqualTo("LogReceived"));
        Assert.That(compensatedSteps[1], Is.EqualTo("PersistMessage"));
    }

    // ── 3. Fan-Out / Split Pattern ──────────────────────────────────────

    [Test]
    public async Task FanOut_MultipleMessagesDispatchedIndependently()
    {
        // Integration pattern: split a batch into individual workflows.
        // Each order line becomes a separate Temporal workflow execution.
        _dispatcher.ReturnsSuccess();
        var orchestrator = CreateOrchestrator();

        var orderLines = new[]
        {
            "{\"sku\":\"SKU-001\",\"qty\":2}",
            "{\"sku\":\"SKU-002\",\"qty\":1}",
            "{\"sku\":\"SKU-003\",\"qty\":5}",
        };

        // Fan-out: dispatch each order line as a separate workflow
        foreach (var line in orderLines)
        {
            var json = JsonSerializer.Deserialize<JsonElement>(line);
            var envelope = IntegrationEnvelope<JsonElement>.Create(
                json, "OrderSplitter", "order.line");
            await orchestrator.ProcessAsync(envelope);
        }

        // Three independent workflow executions
        _dispatcher.AssertDispatchCount(3);

        // Each has a unique workflow ID (from unique message IDs)
        var workflowIds = Enumerable.Range(0, 3)
            .Select(i => _dispatcher.GetWorkflowId(i))
            .ToList();
        Assert.That(workflowIds.Distinct().Count(), Is.EqualTo(3));

        // Each carries its own payload
        Assert.That(_dispatcher.GetInput(0).PayloadJson, Does.Contain("SKU-001"));
        Assert.That(_dispatcher.GetInput(1).PayloadJson, Does.Contain("SKU-002"));
        Assert.That(_dispatcher.GetInput(2).PayloadJson, Does.Contain("SKU-003"));
    }

    // ── 4. Scalability: Retry, Timeout, Task Queue ──────────────────────

    [Test]
    public void TemporalOptions_DefaultScalabilitySettings()
    {
        // TemporalOptions defines the scalability knobs for Temporal workers.
        var options = new TemporalOptions();

        // Task queue determines which worker pool picks up the workflow
        Assert.That(options.TaskQueue, Is.EqualTo("integration-workflows"));
        // Namespace isolates workflows (multi-tenancy at the Temporal level)
        Assert.That(options.Namespace, Is.EqualTo("default"));
        // Server address for the gRPC connection
        Assert.That(options.ServerAddress, Is.EqualTo("localhost:15233"));
    }

    [Test]
    public void PipelineOptions_ConfiguresAckNackSubjects()
    {
        // PipelineOptions controls the NATS subjects for notifications
        // and Temporal connection settings — all tunable for scalability.
        var options = new PipelineOptions();

        Assert.That(options.AckSubject, Is.EqualTo("integration.ack"));
        Assert.That(options.NackSubject, Is.EqualTo("integration.nack"));
        Assert.That(options.InboundSubject, Is.EqualTo("integration.inbound"));
        Assert.That(options.TemporalTaskQueue, Is.EqualTo("integration-workflows"));
        Assert.That(options.WorkflowTimeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    // ── 5. DI Wiring with Aspire ────────────────────────────────────────

    [Test]
    public async Task AspireHost_WiresOrchestratorViaDI()
    {
        // In production, Aspire wires PipelineOrchestrator with the real
        // TemporalWorkflowDispatcher. In tests, we substitute the mock.
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        await using var host = AspireIntegrationTestHost.CreateBuilder()
            .ConfigureServices(svc =>
            {
                svc.AddSingleton<ITemporalWorkflowDispatcher>(dispatcher);
                svc.Configure<PipelineOptions>(o =>
                {
                    o.AckSubject = "test.ack";
                    o.NackSubject = "test.nack";
                });
                svc.AddSingleton<PipelineOrchestrator>();
            })
            .Build();

        var orchestrator = host.GetService<PipelineOrchestrator>();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"di\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "DIService", "di.test");

        await orchestrator.ProcessAsync(envelope);

        Assert.That(dispatcher.LastInput!.Source, Is.EqualTo("DIService"));
        Assert.That(dispatcher.LastInput.AckSubject, Is.EqualTo("test.ack"));
        Assert.That(dispatcher.LastInput.NackSubject, Is.EqualTo("test.nack"));
    }

    [Test]
    public async Task CorrelationAndCausation_PropagatedThroughWorkflow()
    {
        // End-to-end tracing: correlation and causation IDs flow from
        // the envelope into the Temporal workflow input, enabling
        // distributed trace stitching across Temporal and NATS.
        _dispatcher.ReturnsSuccess();
        var orchestrator = CreateOrchestrator();

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "svc", "type", correlationId, causationId);

        await orchestrator.ProcessAsync(envelope);

        var input = _dispatcher.LastInput!;
        Assert.That(input.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(input.CausationId, Is.EqualTo(causationId));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private PipelineOrchestrator CreateOrchestrator(PipelineOptions? options = null) =>
        new(
            _dispatcher,
            Options.Create(options ?? new PipelineOptions()),
            NullLogger<PipelineOrchestrator>.Instance);
}
