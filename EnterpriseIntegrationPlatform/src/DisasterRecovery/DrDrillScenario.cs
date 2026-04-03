namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Defines a disaster recovery drill scenario to validate recovery procedures.
/// </summary>
public sealed record DrDrillScenario
{
    /// <summary>Unique drill scenario identifier.</summary>
    public required string ScenarioId { get; init; }

    /// <summary>Human-readable name for this drill scenario.</summary>
    public required string Name { get; init; }

    /// <summary>The type of disaster being simulated.</summary>
    public required DrDrillType DrillType { get; init; }

    /// <summary>Region to simulate failure in.</summary>
    public required string TargetRegionId { get; init; }

    /// <summary>Region that should take over during the drill.</summary>
    public required string FailoverRegionId { get; init; }

    /// <summary>Maximum duration for the drill before automatic cancellation.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Whether to automatically fail back after the drill.</summary>
    public bool AutoFailback { get; init; } = true;
}
