using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Scatter-Gather — broadcasts a request to multiple recipients and
/// aggregates the replies. Combines Recipient List + Aggregator.
/// Equivalent to BizTalk parallel convoy receiving responses from
/// multiple external systems.
/// </summary>
public interface IScatterGather<TRequest, TReply>
{
    /// <summary>
    /// Sends the request to all registered handlers and collects results.
    /// </summary>
    Task<IReadOnlyList<TReply>> ScatterAsync(
        IntegrationEnvelope<TRequest> request,
        CancellationToken ct = default);
}

/// <summary>
/// In-memory scatter-gather using delegate-based handlers.
/// </summary>
public sealed class ScatterGather<TRequest, TReply> : IScatterGather<TRequest, TReply>
{
    private readonly List<Func<IntegrationEnvelope<TRequest>, CancellationToken, Task<TReply>>> _handlers = new();

    /// <summary>Registers a handler that will receive the scattered request.</summary>
    public ScatterGather<TRequest, TReply> AddHandler(
        Func<IntegrationEnvelope<TRequest>, CancellationToken, Task<TReply>> handler)
    {
        _handlers.Add(handler);
        return this;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TReply>> ScatterAsync(
        IntegrationEnvelope<TRequest> request,
        CancellationToken ct = default)
    {
        var tasks = _handlers.Select(h => h(request, ct));
        var results = await Task.WhenAll(tasks);
        return results;
    }
}
