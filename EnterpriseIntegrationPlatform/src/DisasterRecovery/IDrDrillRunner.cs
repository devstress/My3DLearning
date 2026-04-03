namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Runs disaster recovery drills to validate failover procedures and recovery objectives.
/// </summary>
public interface IDrDrillRunner
{
    /// <summary>
    /// Executes a disaster recovery drill scenario.
    /// </summary>
    /// <param name="scenario">Drill scenario to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the drill execution.</returns>
    Task<DrDrillResult> RunDrillAsync(DrDrillScenario scenario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the history of completed drill results.
    /// </summary>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of past drill results, ordered most recent first.</returns>
    Task<IReadOnlyList<DrDrillResult>> GetDrillHistoryAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the result of the most recent drill.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Last drill result, or <c>null</c> if no drills have been run.</returns>
    Task<DrDrillResult?> GetLastDrillResultAsync(CancellationToken cancellationToken = default);
}
