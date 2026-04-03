namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// A named, priority-sorted business rule with one or more conditions and an action.
/// </summary>
/// <remarks>
/// <para>
/// Conditions within a rule are combined using the <see cref="LogicOperator"/>:
/// <see cref="RuleLogicOperator.And"/> requires all conditions to match;
/// <see cref="RuleLogicOperator.Or"/> requires at least one condition to match.
/// </para>
/// <para>
/// Rules are evaluated in ascending <see cref="Priority"/> order; the first
/// matching rule determines the action. When <see cref="StopOnMatch"/> is
/// <see langword="true"/> (the default), evaluation halts after the first match.
/// </para>
/// </remarks>
public sealed record BusinessRule
{
    /// <summary>Unique human-readable name for this rule, used in logging and diagnostics.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Evaluation priority. Rules with lower values are evaluated first.
    /// When two rules have equal priority, their order is unspecified.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// How to combine multiple conditions — <see cref="RuleLogicOperator.And"/> (all must match)
    /// or <see cref="RuleLogicOperator.Or"/> (any must match).
    /// Defaults to <see cref="RuleLogicOperator.And"/>.
    /// </summary>
    public RuleLogicOperator LogicOperator { get; init; } = RuleLogicOperator.And;

    /// <summary>The conditions that must be satisfied for this rule to fire.</summary>
    public required IReadOnlyList<RuleCondition> Conditions { get; init; }

    /// <summary>The action to execute when this rule fires.</summary>
    public required RuleAction Action { get; init; }

    /// <summary>
    /// When <see langword="true"/>, rule evaluation stops after this rule matches.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool StopOnMatch { get; init; } = true;

    /// <summary>
    /// Whether this rule is enabled. Disabled rules are skipped during evaluation.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; init; } = true;
}
