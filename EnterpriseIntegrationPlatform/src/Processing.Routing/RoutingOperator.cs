namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Comparison operator used to evaluate a <see cref="RoutingRule"/> against a field value.
/// </summary>
public enum RoutingOperator
{
    /// <summary>The field value must exactly equal the configured value (ordinal, case-insensitive).</summary>
    Equals,

    /// <summary>The field value must contain the configured value (ordinal, case-insensitive).</summary>
    Contains,

    /// <summary>The field value must start with the configured value (ordinal, case-insensitive).</summary>
    StartsWith,

    /// <summary>The field value must match the configured regular-expression pattern.</summary>
    Regex,
}
