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

    /// <summary>
    /// When <see langword="true"/>, rules fetched from the store are cached in memory
    /// and reused for subsequent evaluations until <see cref="CacheRefreshIntervalMs"/>
    /// elapses. Defaults to <see langword="true"/>.
    /// </summary>
    public bool CacheEnabled { get; set; } = true;

    /// <summary>
    /// Number of milliseconds between automatic cache refreshes.
    /// Only used when <see cref="CacheEnabled"/> is <see langword="true"/>.
    /// Defaults to 60 000 (1 minute).
    /// </summary>
    public int CacheRefreshIntervalMs { get; set; } = 60_000;
}
