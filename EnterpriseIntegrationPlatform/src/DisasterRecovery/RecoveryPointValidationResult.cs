namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Result of validating recovery objectives against current replication state.
/// </summary>
public sealed record RecoveryPointValidationResult
{
    /// <summary>The objective that was validated.</summary>
    public required RecoveryObjective Objective { get; init; }

    /// <summary>Whether the RPO target is currently met.</summary>
    public required bool RpoMet { get; init; }

    /// <summary>Whether the RTO target is currently met (based on last drill or failover).</summary>
    public required bool RtoMet { get; init; }

    /// <summary>Current replication lag compared against the RPO.</summary>
    public required TimeSpan CurrentLag { get; init; }

    /// <summary>Last measured failover duration compared against the RTO.</summary>
    public required TimeSpan LastFailoverDuration { get; init; }

    /// <summary>UTC timestamp of this validation.</summary>
    public required DateTimeOffset ValidatedAt { get; init; }

    /// <summary>Overall pass/fail: both RPO and RTO must be met.</summary>
    public bool Passed => RpoMet && RtoMet;
}
