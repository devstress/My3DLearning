namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Validates that Recovery Point Objectives (RPO) and Recovery Time Objectives (RTO)
/// are currently being met based on replication state and failover history.
/// </summary>
public interface IRecoveryPointValidator
{
    /// <summary>
    /// Registers a recovery objective for validation.
    /// </summary>
    /// <param name="objective">Recovery objective to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterObjectiveAsync(RecoveryObjective objective, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered recovery objectives.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of objectives.</returns>
    Task<IReadOnlyList<RecoveryObjective>> GetObjectivesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a specific recovery objective against current system state.
    /// </summary>
    /// <param name="objectiveId">Objective to validate.</param>
    /// <param name="currentLag">Current replication lag.</param>
    /// <param name="lastFailoverDuration">Last measured failover duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<RecoveryPointValidationResult> ValidateAsync(
        string objectiveId,
        TimeSpan currentLag,
        TimeSpan lastFailoverDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates all registered objectives against current system state.
    /// </summary>
    /// <param name="currentLag">Current replication lag.</param>
    /// <param name="lastFailoverDuration">Last measured failover duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of validation results.</returns>
    Task<IReadOnlyList<RecoveryPointValidationResult>> ValidateAllAsync(
        TimeSpan currentLag,
        TimeSpan lastFailoverDuration,
        CancellationToken cancellationToken = default);
}
