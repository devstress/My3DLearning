namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Result of executing the <c>SagaCompensationWorkflow</c>.
/// </summary>
/// <param name="CorrelationId">The correlation ID of the saga that was compensated.</param>
/// <param name="CompensatedSteps">The compensation steps that were successfully executed.</param>
/// <param name="FailedSteps">
/// The compensation steps that failed during rollback.
/// An empty list indicates full compensation success.
/// </param>
/// <param name="IsFullyCompensated">
/// <c>true</c> when all steps were compensated without error; <c>false</c> otherwise.
/// </param>
public record SagaCompensationResult(
    Guid CorrelationId,
    IReadOnlyList<string> CompensatedSteps,
    IReadOnlyList<string> FailedSteps,
    bool IsFullyCompensated);
