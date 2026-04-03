namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Snapshot of replication health between a source and target region.
/// </summary>
public sealed record ReplicationStatus
{
    /// <summary>Source region identifier.</summary>
    public required string SourceRegionId { get; init; }

    /// <summary>Target region identifier.</summary>
    public required string TargetRegionId { get; init; }

    /// <summary>Current replication lag measured in time behind the source.</summary>
    public required TimeSpan Lag { get; init; }

    /// <summary>Number of pending replication items that have not yet been confirmed.</summary>
    public required long PendingItems { get; init; }

    /// <summary>Whether replication is currently healthy (lag within threshold).</summary>
    public required bool IsHealthy { get; init; }

    /// <summary>UTC timestamp when this status was captured.</summary>
    public required DateTimeOffset CapturedAt { get; init; }

    /// <summary>Last successfully replicated sequence number.</summary>
    public required long LastReplicatedSequence { get; init; }
}
