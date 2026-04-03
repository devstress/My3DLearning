namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Defines Recovery Point Objective (RPO) and Recovery Time Objective (RTO) targets.
/// </summary>
public sealed record RecoveryObjective
{
    /// <summary>Unique identifier for this objective set.</summary>
    public required string ObjectiveId { get; init; }

    /// <summary>
    /// Maximum acceptable data loss measured in time.
    /// A lower RPO means less data loss tolerance.
    /// </summary>
    public required TimeSpan Rpo { get; init; }

    /// <summary>
    /// Maximum acceptable downtime before service must be restored.
    /// A lower RTO means faster recovery is required.
    /// </summary>
    public required TimeSpan Rto { get; init; }

    /// <summary>Optional description of the objective.</summary>
    public string? Description { get; init; }
}
