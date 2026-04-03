namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Determines how multiple <see cref="RuleCondition"/> instances within a
/// <see cref="BusinessRule"/> are combined.
/// </summary>
public enum RuleLogicOperator
{
    /// <summary>All conditions must match for the rule to fire.</summary>
    And,

    /// <summary>At least one condition must match for the rule to fire.</summary>
    Or,
}
