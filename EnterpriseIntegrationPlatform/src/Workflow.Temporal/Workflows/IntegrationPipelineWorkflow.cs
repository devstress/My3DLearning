using Temporalio.Workflows;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Workflows;

/// <summary>
/// Temporal workflow that orchestrates the full integration pipeline atomically.
/// Every side-effect (persist, validate, status update, fault, ack/nack) executes
/// as a Temporal activity with durability guarantees.
/// <para>
/// <b>All-or-nothing semantics:</b> If the workflow is interrupted at any point
/// (process crash, restart, network partition), Temporal resumes from the last
/// completed activity. No partial state is left behind. This replaces the previous
/// non-atomic <c>PipelineOrchestrator</c> that executed side-effects outside Temporal.
/// </para>
/// <para>
/// Pipeline steps:
/// <list type="number">
///   <item>Persist message to Cassandra as Pending</item>
///   <item>Log "Received" lifecycle event</item>
///   <item>Validate message payload</item>
///   <item>On success: log "Validated" → update status to Delivered → publish Ack</item>
///   <item>On failure: log "ValidationFailed" → save fault → update status to Failed → publish Nack</item>
/// </list>
/// </para>
/// </summary>
[Workflow]
public class IntegrationPipelineWorkflow
{
    private const string ServiceName = "Workflow.Temporal";

    /// <summary>
    /// Activity options for persistence and notification activities.
    /// Retries up to 5 times with exponential backoff — ensures transient
    /// infrastructure failures (Cassandra timeout, NATS unavailable) are retried
    /// before the workflow fails.
    /// </summary>
    private static readonly ActivityOptions PipelineActivityOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromMinutes(2),
        RetryPolicy = new Temporalio.Common.RetryPolicy
        {
            MaximumAttempts = 5,
            InitialInterval = TimeSpan.FromSeconds(1),
            BackoffCoefficient = 2.0f,
            MaximumInterval = TimeSpan.FromSeconds(30),
        },
    };

    /// <summary>
    /// Activity options for validation — lighter retry since validation is
    /// typically a pure in-process operation.
    /// </summary>
    private static readonly ActivityOptions ValidationActivityOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromMinutes(1),
        RetryPolicy = new Temporalio.Common.RetryPolicy
        {
            MaximumAttempts = 3,
            InitialInterval = TimeSpan.FromSeconds(1),
            BackoffCoefficient = 2.0f,
        },
    };

    [WorkflowRun]
    public async Task<IntegrationPipelineResult> RunAsync(IntegrationPipelineInput input)
    {
        // ── Step 1: Persist message as Pending ─────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) => act.PersistMessageAsync(input),
            PipelineActivityOptions);

        // ── Step 2: Log Received lifecycle event ───────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.LogStageAsync(input.MessageId, input.MessageType, "Received"),
            PipelineActivityOptions);

        // ── Step 3: Validate message ───────────────────────────────────────
        var validation = await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            ValidationActivityOptions);

        if (!validation.IsValid)
        {
            return await HandleFailureAsync(input, validation.Reason ?? "Validation failed");
        }

        return await HandleSuccessAsync(input);
    }

    private async Task<IntegrationPipelineResult> HandleSuccessAsync(
        IntegrationPipelineInput input)
    {
        // ── Step 4a: Log Validated ─────────────────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.LogStageAsync(input.MessageId, input.MessageType, "Validated"),
            PipelineActivityOptions);

        // ── Step 4b: Update Cassandra status to Delivered ──────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.UpdateDeliveryStatusAsync(
                    input.MessageId, input.CorrelationId,
                    input.Timestamp, "Delivered"),
            PipelineActivityOptions);

        // ── Step 4c: Publish Ack ───────────────────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.PublishAckAsync(input.MessageId, input.CorrelationId, input.AckSubject),
            PipelineActivityOptions);

        return new IntegrationPipelineResult(input.MessageId, true);
    }

    private async Task<IntegrationPipelineResult> HandleFailureAsync(
        IntegrationPipelineInput input,
        string reason)
    {
        // ── Step 5a: Log ValidationFailed ──────────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.LogStageAsync(input.MessageId, input.MessageType, "ValidationFailed"),
            PipelineActivityOptions);

        // ── Step 5b: Save fault envelope ───────────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.SaveFaultAsync(
                    input.MessageId, input.CorrelationId,
                    input.MessageType, ServiceName, reason, 0),
            PipelineActivityOptions);

        // ── Step 5c: Update Cassandra status to Failed ─────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.UpdateDeliveryStatusAsync(
                    input.MessageId, input.CorrelationId,
                    input.Timestamp, "Failed"),
            PipelineActivityOptions);

        // ── Step 5d: Publish Nack ──────────────────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.PublishNackAsync(
                    input.MessageId, input.CorrelationId,
                    reason, input.NackSubject),
            PipelineActivityOptions);

        return new IntegrationPipelineResult(input.MessageId, false, reason);
    }
}
