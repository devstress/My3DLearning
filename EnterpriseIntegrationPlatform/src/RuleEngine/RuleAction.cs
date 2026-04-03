namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Defines the action to execute when a <see cref="BusinessRule"/> matches.
/// </summary>
public sealed record RuleAction
{
    /// <summary>The type of action to perform.</summary>
    public required RuleActionType ActionType { get; init; }

    /// <summary>
    /// Target topic for <see cref="RuleActionType.Route"/> actions.
    /// Ignored for other action types.
    /// </summary>
    public string? TargetTopic { get; init; }

    /// <summary>
    /// Name of the transform to apply for <see cref="RuleActionType.Transform"/> actions.
    /// Ignored for other action types.
    /// </summary>
    public string? TransformName { get; init; }

    /// <summary>
    /// Human-readable reason for <see cref="RuleActionType.Reject"/> and
    /// <see cref="RuleActionType.DeadLetter"/> actions.
    /// </summary>
    public string? Reason { get; init; }
}
