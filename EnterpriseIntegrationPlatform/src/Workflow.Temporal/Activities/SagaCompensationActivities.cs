using EnterpriseIntegrationPlatform.Activities;
using Temporalio.Activities;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

/// <summary>
/// Temporal activity definitions for saga compensation.
/// Each activity corresponds to one compensation step and is executed in reverse
/// order of the original saga steps to roll back committed work.
/// </summary>
public sealed class SagaCompensationActivities
{
    private readonly ICompensationActivityService _compensationService;
    private readonly IMessageLoggingService _logging;

    /// <summary>Initialises a new instance of <see cref="SagaCompensationActivities"/>.</summary>
    public SagaCompensationActivities(
        ICompensationActivityService compensationService,
        IMessageLoggingService logging)
    {
        _compensationService = compensationService;
        _logging = logging;
    }

    /// <summary>
    /// Executes a single named compensation step for the given correlation ID.
    /// Returns <c>true</c> on success, <c>false</c> if the step could not be compensated.
    /// </summary>
    [Activity]
    public async Task<bool> CompensateStepAsync(Guid correlationId, string stepName)
    {
        await _logging.LogAsync(correlationId, stepName, $"CompensationStarted:{stepName}");
        var success = await _compensationService.CompensateAsync(correlationId, stepName);
        var stage = success ? $"CompensationSucceeded:{stepName}" : $"CompensationFailed:{stepName}";
        await _logging.LogAsync(correlationId, stepName, stage);
        return success;
    }
}
