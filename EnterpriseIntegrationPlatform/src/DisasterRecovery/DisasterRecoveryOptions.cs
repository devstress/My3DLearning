namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Configuration options for the Disaster Recovery infrastructure.
/// Bind from the <c>DisasterRecovery</c> configuration section.
/// </summary>
public sealed class DisasterRecoveryOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "DisasterRecovery";

    /// <summary>
    /// Maximum acceptable replication lag before the replication link is considered unhealthy.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan MaxReplicationLag { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Interval between automatic health checks for registered regions.
    /// Default is 10 seconds.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum number of drill results to retain in history.
    /// Default is 100.
    /// </summary>
    public int MaxDrillHistorySize { get; set; } = 100;

    /// <summary>
    /// Number of consecutive failed health checks before a region is marked offline.
    /// Default is 3.
    /// </summary>
    public int OfflineThreshold { get; set; } = 3;

    /// <summary>
    /// Estimated per-item replication time used to convert pending item counts to lag duration.
    /// Default is 1 millisecond per item.
    /// </summary>
    public TimeSpan PerItemReplicationTime { get; set; } = TimeSpan.FromMilliseconds(1);
}
