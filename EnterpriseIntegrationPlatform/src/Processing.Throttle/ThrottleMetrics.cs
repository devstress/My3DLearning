namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Observable metrics for the message processing throttle.
/// </summary>
public sealed record ThrottleMetrics
{
    /// <summary>Total tokens successfully acquired since startup.</summary>
    public required long TotalAcquired { get; init; }

    /// <summary>Total acquire attempts that were rejected (backpressure/timeout).</summary>
    public required long TotalRejected { get; init; }

    /// <summary>Current available tokens in the bucket.</summary>
    public required int AvailableTokens { get; init; }

    /// <summary>Maximum bucket capacity (burst size).</summary>
    public required int BurstCapacity { get; init; }

    /// <summary>Configured refill rate (tokens per second).</summary>
    public required int RefillRate { get; init; }

    /// <summary>Cumulative time callers have spent waiting for tokens.</summary>
    public required TimeSpan TotalWaitTime { get; init; }
}
