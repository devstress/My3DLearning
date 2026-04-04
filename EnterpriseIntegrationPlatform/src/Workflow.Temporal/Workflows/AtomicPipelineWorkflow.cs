using Temporalio.Workflows;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Workflows;

/// <summary>
/// Result of the <c>AtomicPipelineWorkflow</c>.
/// </summary>
/// <param name="MessageId">The message identifier.</param>
/// <param name="IsSuccess">Whether the pipeline completed successfully.</param>
/// <param name="FailureReason">Reason for failure when <paramref name="IsSuccess"/> is false.</param>
/// <param name="CompensatedSteps">
/// Steps that were rolled back when a Nack occurred. Empty on success.
/// </param>
public sealed record AtomicPipelineResult(
    Guid MessageId,
    bool IsSuccess,
    string? FailureReason = null,
    IReadOnlyList<string>? CompensatedSteps = null);

/// <summary>
/// Temporal workflow that orchestrates the full integration pipeline atomically
/// with saga compensation. Each successfully completed step is tracked; if a
/// later step fails (triggering a Nack), all previously committed steps are
/// rolled back in reverse order via compensation activities.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nack-triggered rollback:</b> When validation fails (or any late-stage step fails),
/// the workflow publishes a Nack and then executes compensation activities for
/// every step that was already ack'd (committed). This ensures true all-or-nothing
/// semantics — either the entire pipeline succeeds with an Ack, or everything is
/// rolled back and a Nack is published.
/// </para>
/// <para>
/// Pipeline steps:
/// <list type="number">
///   <item>Persist message as Pending (compensable: delete persisted message)</item>
///   <item>Log Received lifecycle event (compensable: log rollback)</item>
///   <item>Validate message payload</item>
///   <item>On success: Update status to Delivered → Publish Ack</item>
///   <item>On failure: Compensate all prior steps → Update status to Failed → Publish Nack</item>
/// </list>
/// </para>
/// </remarks>
[Workflow]
public class AtomicPipelineWorkflow
{
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

    private static readonly ActivityOptions CompensationActivityOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromMinutes(5),
        RetryPolicy = new Temporalio.Common.RetryPolicy
        {
            MaximumAttempts = 3,
            InitialInterval = TimeSpan.FromSeconds(2),
            BackoffCoefficient = 2.0f,
        },
    };

    [WorkflowRun]
    public async Task<AtomicPipelineResult> RunAsync(IntegrationPipelineInput input)
    {
        var completedSteps = new List<string>();

        // ── Step 1: Persist message as Pending ──────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) => act.PersistMessageAsync(input),
            PipelineActivityOptions);
        completedSteps.Add("PersistMessage");

        // ── Step 2: Log Received lifecycle event ────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.LogStageAsync(input.MessageId, input.MessageType, "Received"),
            PipelineActivityOptions);
        completedSteps.Add("LogReceived");

        // ── Step 3: Validate message ────────────────────────────────────────
        var validation = await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            ValidationActivityOptions);

        if (!validation.IsValid)
        {
            // Nack path: compensate all previously completed steps, then Nack
            return await HandleNackWithRollbackAsync(
                input, completedSteps, validation.Reason ?? "Validation failed");
        }

        // ── Step 4: Update status to Delivered ──────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.UpdateDeliveryStatusAsync(
                    input.MessageId, input.CorrelationId,
                    input.Timestamp, "Delivered"),
            PipelineActivityOptions);
        completedSteps.Add("UpdateStatusDelivered");

        // ── Step 5: Publish Ack (only if notifications are enabled) ──────────
        // Use Case 1: NotificationsEnabled=false → skip (backward compatible)
        // Use Case 2: NotificationsEnabled=true  → Channel Adapter delivered, publish Ack
        // Use Case 4: Feature flag off → service silently skips even if enabled
        if (input.NotificationsEnabled)
        {
            await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                (PipelineActivities act) =>
                    act.PublishAckAsync(input.MessageId, input.CorrelationId, input.AckSubject),
                PipelineActivityOptions);
        }

        return new AtomicPipelineResult(input.MessageId, true);
    }

    /// <summary>
    /// Compensates all previously completed steps in reverse order, then publishes
    /// a Nack. This ensures that a Nack at the end triggers rollback of every
    /// previously ack'd step.
    /// </summary>
    private async Task<AtomicPipelineResult> HandleNackWithRollbackAsync(
        IntegrationPipelineInput input,
        List<string> completedSteps,
        string failureReason)
    {
        var compensatedSteps = new List<string>();

        // Compensate in reverse order (last committed step first)
        foreach (var step in Enumerable.Reverse(completedSteps))
        {
            try
            {
                var success = await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                    (SagaCompensationActivities act) =>
                        act.CompensateStepAsync(input.CorrelationId, step),
                    CompensationActivityOptions);

                if (success)
                    compensatedSteps.Add(step);
            }
            catch (Exception)
            {
                // Log but continue — partial compensation is better than none
            }
        }

        // ── Save fault ──────────────────────────────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.SaveFaultAsync(
                    input.MessageId, input.CorrelationId,
                    input.MessageType, "AtomicPipeline", failureReason, 0),
            PipelineActivityOptions);

        // ── Update status to Failed ─────────────────────────────────────────
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.UpdateDeliveryStatusAsync(
                    input.MessageId, input.CorrelationId,
                    input.Timestamp, "Failed"),
            PipelineActivityOptions);

        // ── Publish Nack (only if notifications are enabled) ─────────────────
        // Use Case 1: NotificationsEnabled=false → skip (backward compatible)
        // Use Case 3: NotificationsEnabled=true  → Channel Adapter failed, publish Nack
        // Use Case 5: Feature flag off → service silently skips even if enabled
        if (input.NotificationsEnabled)
        {
            await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                (PipelineActivities act) =>
                    act.PublishNackAsync(
                        input.MessageId, input.CorrelationId,
                        failureReason, input.NackSubject),
                PipelineActivityOptions);
        }

        return new AtomicPipelineResult(
            input.MessageId,
            false,
            failureReason,
            compensatedSteps.AsReadOnly());
    }
}
