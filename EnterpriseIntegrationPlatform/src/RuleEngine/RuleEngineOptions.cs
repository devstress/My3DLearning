namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Configuration options for the <see cref="BusinessRuleEngine"/>.
/// Bound from the <c>RuleEngine</c> section of application configuration.
/// </summary>
public sealed class RuleEngineOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "RuleEngine";

    /// <summary>
    /// Whether the rule engine is enabled.
    /// When disabled, <see cref="IRuleEngine.EvaluateAsync{T}"/> returns an empty result.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of rules to evaluate per message before short-circuiting.
    /// Zero or negative means unlimited. Defaults to 0 (unlimited).
    /// </summary>
    public int MaxRulesPerEvaluation { get; set; }

    /// <summary>
    /// Pre-configured rules loaded from configuration.
    /// These are seeded into the <see cref="IRuleStore"/> at startup.
    /// </summary>
    public IReadOnlyList<BusinessRule> Rules { get; set; } = [];

    /// <summary>
    /// Timeout for regex condition evaluation to prevent catastrophic backtracking.
    /// Defaults to 5 seconds.
    /// </summary>
    public TimeSpan RegexTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
