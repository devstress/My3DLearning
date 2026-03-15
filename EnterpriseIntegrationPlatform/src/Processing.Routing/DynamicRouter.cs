using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Dynamic Router — routes messages based on rules that can be updated at
/// runtime without redeployment. Equivalent to BizTalk Business Rules Engine
/// (BRE) integration for routing decisions.
/// </summary>
public interface IDynamicRouter<T>
{
    /// <summary>Updates the routing rules at runtime.</summary>
    void UpdateRules(IEnumerable<DynamicRoutingRule<T>> rules);

    /// <summary>Evaluates rules and returns the destination.</summary>
    string? Route(IntegrationEnvelope<T> envelope);
}

/// <summary>
/// A single routing rule: predicate + destination + priority.
/// </summary>
public record DynamicRoutingRule<T>(
    Func<IntegrationEnvelope<T>, bool> Predicate,
    string Destination,
    int Priority = 0);

/// <summary>
/// In-memory dynamic router with runtime-updatable rules.
/// </summary>
public sealed class DynamicRouter<T> : IDynamicRouter<T>
{
    private volatile List<DynamicRoutingRule<T>> _rules = new();

    /// <inheritdoc />
    public void UpdateRules(IEnumerable<DynamicRoutingRule<T>> rules) =>
        _rules = rules.OrderByDescending(r => r.Priority).ToList();

    /// <inheritdoc />
    public string? Route(IntegrationEnvelope<T> envelope) =>
        _rules.FirstOrDefault(r => r.Predicate(envelope))?.Destination;
}
