namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Describes the outcome of a content-based routing evaluation.
/// </summary>
/// <param name="TargetTopic">
/// The message broker topic or subject to which the message was routed.
/// </param>
/// <param name="MatchedRule">
/// The <see cref="RoutingRule"/> that caused the routing decision, or
/// <see langword="null"/> when the default topic was used.
/// </param>
/// <param name="IsDefault">
/// <see langword="true"/> when the routing decision was made using the default topic
/// because no rule matched; <see langword="false"/> when a rule matched.
/// </param>
public sealed record RoutingDecision(
    string TargetTopic,
    RoutingRule? MatchedRule,
    bool IsDefault);
