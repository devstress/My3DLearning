namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Combines a <see cref="ThrottlePolicy"/> with its runtime <see cref="ThrottleMetrics"/>.
/// Returned by the admin-facing registry queries.
/// </summary>
public sealed record ThrottlePolicyStatus
{
    /// <summary>The policy configuration.</summary>
    public required ThrottlePolicy Policy { get; init; }

    /// <summary>Current runtime metrics for this policy's throttle.</summary>
    public required ThrottleMetrics Metrics { get; init; }
}
