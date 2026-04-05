using EnterpriseIntegrationPlatform.RuleEngine;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Configuration options for the <see cref="MessageFilter"/>.
/// Bound from the <c>MessageFilter</c> section of application configuration.
/// </summary>
public sealed class MessageFilterOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "MessageFilter";

    /// <summary>
    /// The predicates to evaluate. All conditions are combined using
    /// <see cref="Logic"/> (AND or OR).
    /// </summary>
    public IReadOnlyList<RuleCondition> Conditions { get; init; } = [];

    /// <summary>
    /// Determines how multiple conditions are combined.
    /// <see cref="RuleLogicOperator.And"/> (default) — all conditions must match.
    /// <see cref="RuleLogicOperator.Or"/> — at least one condition must match.
    /// </summary>
    public RuleLogicOperator Logic { get; init; } = RuleLogicOperator.And;

    /// <summary>
    /// The topic to which messages that pass the predicate are published.
    /// </summary>
    public required string OutputTopic { get; init; }

    /// <summary>
    /// Optional topic for discarded messages (e.g. a DLQ topic).
    /// When <see langword="null"/> or empty, discarded messages are silently dropped
    /// unless <see cref="RequireDiscardTopic"/> is <see langword="true"/>.
    /// </summary>
    public string? DiscardTopic { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the filter throws <see cref="InvalidOperationException"/>
    /// if a message fails the predicate and no <see cref="DiscardTopic"/> is configured.
    /// This enforces no-silent-drop semantics in production deployments.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool RequireDiscardTopic { get; init; }
}
