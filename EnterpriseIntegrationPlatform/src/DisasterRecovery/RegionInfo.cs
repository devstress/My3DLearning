namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Describes a region participating in disaster recovery.
/// </summary>
public sealed record RegionInfo
{
    /// <summary>Unique region identifier (e.g. "us-east-1", "eu-west-1").</summary>
    public required string RegionId { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Current failover state of the region.</summary>
    public FailoverState State { get; init; } = FailoverState.Standby;

    /// <summary>UTC timestamp of the last health check.</summary>
    public DateTimeOffset LastHealthCheck { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether this region is currently the primary.</summary>
    public bool IsPrimary => State is FailoverState.Primary;
}
