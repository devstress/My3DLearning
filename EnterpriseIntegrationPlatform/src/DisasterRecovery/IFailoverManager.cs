namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Manages automated failover between primary and standby regions.
/// </summary>
public interface IFailoverManager
{
    /// <summary>
    /// Registers a region in the failover topology.
    /// </summary>
    /// <param name="region">Region information to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterRegionAsync(RegionInfo region, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current primary region.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current primary region, or <c>null</c> if no primary is set.</returns>
    Task<RegionInfo?> GetPrimaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered regions and their current states.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of all regions.</returns>
    Task<IReadOnlyList<RegionInfo>> GetAllRegionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates failover from the current primary to the specified target region.
    /// </summary>
    /// <param name="targetRegionId">Region to promote to primary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the failover operation.</returns>
    Task<FailoverResult> FailoverAsync(string targetRegionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fails back to the original primary region after a failover event.
    /// </summary>
    /// <param name="originalPrimaryRegionId">Region to restore as primary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the failback operation.</returns>
    Task<FailoverResult> FailbackAsync(string originalPrimaryRegionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the health check timestamp for a region.
    /// </summary>
    /// <param name="regionId">Region to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateHealthCheckAsync(string regionId, CancellationToken cancellationToken = default);
}
