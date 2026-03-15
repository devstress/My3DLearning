using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Evaluates business rules against an integration message and returns
/// a set of actions to apply.
/// </summary>
public interface IRuleEngine
{
    /// <summary>Evaluates all matching rules for a message.</summary>
    Task<RuleEvaluationResult> EvaluateAsync<T>(
        IntegrationEnvelope<T> envelope, CancellationToken ct = default);
}

/// <summary>
/// Result of evaluating business rules against a message.
/// </summary>
/// <param name="Matched">True if at least one rule matched.</param>
/// <param name="Actions">Ordered list of actions to execute.</param>
public record RuleEvaluationResult(
    bool Matched,
    IReadOnlyList<RuleAction> Actions);

/// <summary>
/// An action produced by the rule engine.
/// </summary>
/// <param name="ActionType">The type of action (e.g. "Route", "Transform", "Reject").</param>
/// <param name="Parameters">Key-value parameters for the action.</param>
public record RuleAction(
    string ActionType,
    IReadOnlyDictionary<string, string> Parameters);
