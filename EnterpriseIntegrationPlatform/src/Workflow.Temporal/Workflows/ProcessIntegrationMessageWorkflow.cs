using Temporalio.Workflows;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Workflows;

/// <summary>
/// Input parameters for <see cref="ProcessIntegrationMessageWorkflow"/>.
/// </summary>
/// <param name="MessageId">Unique identifier of the message being processed.</param>
/// <param name="MessageType">Logical message type name.</param>
/// <param name="PayloadJson">JSON-serialised payload to validate and route.</param>
public record ProcessIntegrationMessageInput(
    Guid MessageId,
    string MessageType,
    string PayloadJson);

/// <summary>
/// Result of executing <see cref="ProcessIntegrationMessageWorkflow"/>.
/// </summary>
/// <param name="MessageId">The message that was processed.</param>
/// <param name="IsValid">Whether the message passed validation.</param>
/// <param name="FailureReason">Reason for failure, if any.</param>
public record ProcessIntegrationMessageResult(
    Guid MessageId,
    bool IsValid,
    string? FailureReason = null);

/// <summary>
/// Sample Temporal workflow that validates and logs an integration message.
/// This is the first concrete workflow in the platform and demonstrates:
///   1. Invoking typed activities with retry policies
///   2. Conditional branching based on activity results
///   3. Compensation-ready structure for future saga patterns
/// </summary>
[Workflow]
public class ProcessIntegrationMessageWorkflow
{
    /// <summary>
    /// Activity options applied to every activity call in this workflow.
    /// StartToCloseTimeout limits how long a single activity execution may run.
    /// </summary>
    private static readonly ActivityOptions DefaultActivityOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromMinutes(1),
    };

    [WorkflowRun]
    public async Task<ProcessIntegrationMessageResult> RunAsync(
        ProcessIntegrationMessageInput input)
    {
        // Step 1 — Log receipt
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.LogProcessingStageAsync(input.MessageId, input.MessageType, "Received"),
            DefaultActivityOptions);

        // Step 2 — Validate
        var validation = await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            DefaultActivityOptions);

        if (!validation.IsValid)
        {
            // Log validation failure
            await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                (IntegrationActivities act) =>
                    act.LogProcessingStageAsync(
                        input.MessageId, input.MessageType, "ValidationFailed"),
                DefaultActivityOptions);

            return new ProcessIntegrationMessageResult(
                input.MessageId, false, validation.Reason);
        }

        // Step 3 — Log successful validation
        await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.LogProcessingStageAsync(input.MessageId, input.MessageType, "Validated"),
            DefaultActivityOptions);

        return new ProcessIntegrationMessageResult(input.MessageId, true);
    }
}
