namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Result of a failover operation.
/// </summary>
public sealed record FailoverResult
{
    /// <summary>Whether the failover completed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>Region that was promoted to primary.</summary>
    public required string PromotedRegionId { get; init; }

    /// <summary>Region that was demoted from primary.</summary>
    public required string DemotedRegionId { get; init; }

    /// <summary>Time taken to complete the failover.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>UTC timestamp of failover completion.</summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>Human-readable error message if the failover failed.</summary>
    public string? ErrorMessage { get; init; }
}
