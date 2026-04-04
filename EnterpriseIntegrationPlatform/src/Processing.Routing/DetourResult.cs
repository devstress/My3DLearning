namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Describes the outcome of a <see cref="IDetour.RouteAsync{T}"/> call.
/// </summary>
/// <param name="Detoured">Whether the message was routed through the detour pipeline.</param>
/// <param name="TargetTopic">The topic the message was published to.</param>
/// <param name="Reason">Human-readable description of the routing decision.</param>
public sealed record DetourResult(
    bool Detoured,
    string TargetTopic,
    string Reason);
