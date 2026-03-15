using System.Collections.Concurrent;

using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Aggregator — collects related messages and combines them into a single
/// output message once a completion condition is met.
/// Equivalent to BizTalk Convoy / Sequential Convoy pattern for aggregating
/// related messages by correlation ID.
/// </summary>
public interface IMessageAggregator<T, TAggregated>
{
    /// <summary>Adds a message to the aggregation buffer.</summary>
    void Add(IntegrationEnvelope<T> envelope);

    /// <summary>Returns true when enough messages have been collected.</summary>
    bool IsComplete(Guid correlationId);

    /// <summary>Produces the aggregated result and clears the buffer.</summary>
    IntegrationEnvelope<TAggregated> Harvest(Guid correlationId);
}

/// <summary>
/// Count-based aggregator that produces a result after a specified number
/// of correlated messages arrive.
/// </summary>
public sealed class CountBasedAggregator<T> : IMessageAggregator<T, IReadOnlyList<T>>
{
    private readonly int _expectedCount;
    private readonly ConcurrentDictionary<Guid, List<IntegrationEnvelope<T>>> _buffer = new();

    public CountBasedAggregator(int expectedCount)
    {
        _expectedCount = expectedCount;
    }

    /// <inheritdoc />
    public void Add(IntegrationEnvelope<T> envelope)
    {
        _buffer.AddOrUpdate(
            envelope.CorrelationId,
            _ => new List<IntegrationEnvelope<T>> { envelope },
            (_, list) => { list.Add(envelope); return list; });
    }

    /// <inheritdoc />
    public bool IsComplete(Guid correlationId) =>
        _buffer.TryGetValue(correlationId, out var list) && list.Count >= _expectedCount;

    /// <inheritdoc />
    public IntegrationEnvelope<IReadOnlyList<T>> Harvest(Guid correlationId)
    {
        if (!_buffer.TryRemove(correlationId, out var list) || list.Count == 0)
            throw new InvalidOperationException(
                $"No messages buffered for correlation {correlationId}");

        var payloads = list.Select(e => e.Payload).ToList();
        var first = list[0];

        return IntegrationEnvelope<IReadOnlyList<T>>.Create(
            payloads,
            first.Source,
            first.MessageType + ".Aggregated",
            correlationId);
    }
}
