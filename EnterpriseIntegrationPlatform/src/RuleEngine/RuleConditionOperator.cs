namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Comparison operator used to evaluate a <see cref="RuleCondition"/> against a field value.
/// </summary>
public enum RuleConditionOperator
{
    /// <summary>The field value must exactly equal the configured value (case-insensitive).</summary>
    Equals,

    /// <summary>The field value must contain the configured value as a substring (case-insensitive).</summary>
    Contains,

    /// <summary>The field value must match the configured regular-expression pattern.</summary>
    Regex,

    /// <summary>The field value must be one of the configured comma-separated values (case-insensitive).</summary>
    In,

    /// <summary>The field value must be numerically greater than the configured value.</summary>
    GreaterThan,
}
