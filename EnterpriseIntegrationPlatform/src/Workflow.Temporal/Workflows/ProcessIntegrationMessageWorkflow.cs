using Temporalio.Workflows;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Workflows;

/// <summary>
/// Temporal workflow that validates and logs an integration message.
/// Steps: receive → validate → log outcome.
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
