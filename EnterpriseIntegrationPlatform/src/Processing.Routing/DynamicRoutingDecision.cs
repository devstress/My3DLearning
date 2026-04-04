namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Describes the outcome of a dynamic routing evaluation.
/// </summary>
/// <param name="Destination">
/// The message broker topic or subject to which the message was routed.
/// </param>
/// <param name="MatchedEntry">
/// The <see cref="DynamicRouteEntry"/> that caused the routing decision, or
/// <see langword="null"/> when the fallback topic was used.
/// </param>
/// <param name="IsFallback">
/// <see langword="true"/> when the routing decision was made using the fallback topic
/// because no routing table entry matched; <see langword="false"/> when an entry matched.
/// </param>
/// <param name="ConditionValue">
/// The value extracted from the envelope that was used to look up the routing table.
/// </param>
public sealed record DynamicRoutingDecision(
    string Destination,
    DynamicRouteEntry? MatchedEntry,
    bool IsFallback,
    string? ConditionValue);
