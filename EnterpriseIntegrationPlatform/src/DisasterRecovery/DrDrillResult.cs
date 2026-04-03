namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Result of executing a disaster recovery drill.
/// </summary>
public sealed record DrDrillResult
{
    /// <summary>The scenario that was executed.</summary>
    public required DrDrillScenario Scenario { get; init; }

    /// <summary>Whether the drill completed successfully with all validations passing.</summary>
    public required bool Success { get; init; }

    /// <summary>Time taken to detect the simulated failure.</summary>
    public required TimeSpan DetectionTime { get; init; }

    /// <summary>Time taken to complete failover after detection.</summary>
    public required TimeSpan FailoverTime { get; init; }

    /// <summary>Total drill duration from start to full recovery (including failback if applicable).</summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>Measured data loss during the drill (zero if RPO was met).</summary>
    public required TimeSpan DataLoss { get; init; }

    /// <summary>Recovery objective validation result captured during the drill.</summary>
    public RecoveryPointValidationResult? ValidationResult { get; init; }

    /// <summary>UTC timestamp when the drill started.</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>UTC timestamp when the drill completed.</summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>Error details if the drill failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Whether failback was completed (if AutoFailback was enabled).</summary>
    public bool FailbackCompleted { get; init; }
}
