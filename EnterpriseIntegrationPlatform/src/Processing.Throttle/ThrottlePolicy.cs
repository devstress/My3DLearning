namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// A throttle policy that can be applied to a <see cref="ThrottlePartitionKey"/>.
/// Represents the admin-configurable settings for a specific partition.
/// </summary>
public sealed class ThrottlePolicy
{
    /// <summary>Unique policy identifier.</summary>
    public required string PolicyId { get; init; }

    /// <summary>Human-readable policy name.</summary>
    public required string Name { get; init; }

    /// <summary>The partition this policy applies to.</summary>
    public required ThrottlePartitionKey Partition { get; init; }

    /// <summary>Maximum messages per second for this partition.</summary>
    public int MaxMessagesPerSecond { get; set; } = 100;

    /// <summary>Burst capacity (token bucket size) for this partition.</summary>
    public int BurstCapacity { get; set; } = 200;

    /// <summary>Maximum wait time before timeout for this partition.</summary>
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>When true, reject immediately on backpressure instead of waiting.</summary>
    public bool RejectOnBackpressure { get; set; }

    /// <summary>Whether this policy is currently active.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>When this policy was created or last modified (UTC).</summary>
    public DateTimeOffset LastModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
}
