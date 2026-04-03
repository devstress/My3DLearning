namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Manages cross-region data replication monitoring and control.
/// </summary>
public interface IReplicationManager
{
    /// <summary>
    /// Gets the current replication status between two regions.
    /// </summary>
    /// <param name="sourceRegionId">Source region identifier.</param>
    /// <param name="targetRegionId">Target region identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current replication status.</returns>
    Task<ReplicationStatus> GetStatusAsync(string sourceRegionId, string targetRegionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets replication status for all configured replication pairs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of replication statuses.</returns>
    Task<IReadOnlyList<ReplicationStatus>> GetAllStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports a replication update from a source to a target region.
    /// </summary>
    /// <param name="sourceRegionId">Source region identifier.</param>
    /// <param name="targetRegionId">Target region identifier.</param>
    /// <param name="sequenceNumber">Replicated sequence number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReportReplicationAsync(string sourceRegionId, string targetRegionId, long sequenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the latest source sequence number for lag calculation.
    /// </summary>
    /// <param name="sourceRegionId">Source region identifier.</param>
    /// <param name="sequenceNumber">Current source sequence number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReportSourceProgressAsync(string sourceRegionId, long sequenceNumber, CancellationToken cancellationToken = default);
}
