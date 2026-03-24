using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;
using Temporalio.Workflows;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Workflows;

/// <summary>
/// Temporal workflow that executes saga compensation steps in reverse order.
/// For each step that fails to compensate, the step name is recorded in
/// <see cref="SagaCompensationResult.FailedSteps"/>. The workflow continues with the
/// remaining steps rather than aborting — partial compensation is preferable to no
/// compensation.
/// </summary>
[Workflow]
public class SagaCompensationWorkflow
{
    private static readonly ActivityOptions DefaultActivityOptions = new()
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
    public async Task<SagaCompensationResult> RunAsync(SagaCompensationInput input)
    {
        // Execute compensation steps in reverse order (last-committed is undone first).
        var stepsToCompensate = input.CompensationSteps.Reverse().ToList();

        var compensated = new List<string>();
        var failed = new List<string>();

        foreach (var step in stepsToCompensate)
        {
            try
            {
                var success = await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                    (SagaCompensationActivities act) =>
                        act.CompensateStepAsync(input.CorrelationId, step),
                    DefaultActivityOptions);

                if (success)
                    compensated.Add(step);
                else
                    failed.Add(step);
            }
            catch (Exception)
            {
                // Record failure and continue with remaining steps.
                failed.Add(step);
            }
        }

        return new SagaCompensationResult(
            CorrelationId: input.CorrelationId,
            CompensatedSteps: compensated,
            FailedSteps: failed,
            IsFullyCompensated: failed.Count == 0);
    }
}
