namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// The outcome of evaluating a set of business rules against a message.
/// </summary>
/// <param name="MatchedRules">
/// The business rules that matched the message, in the order they were evaluated.
/// Empty when no rule matched.
/// </param>
/// <param name="Actions">
/// The actions to execute based on the matched rules.
/// Empty when no rule matched.
/// </param>
/// <param name="HasMatch">
/// <see langword="true"/> when at least one rule matched; <see langword="false"/> otherwise.
/// </param>
/// <param name="RulesEvaluated">Total number of rules evaluated before termination.</param>
public sealed record RuleEvaluationResult(
    IReadOnlyList<BusinessRule> MatchedRules,
    IReadOnlyList<RuleAction> Actions,
    bool HasMatch,
    int RulesEvaluated);
