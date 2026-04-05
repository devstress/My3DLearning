using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// Thread-safe, in-process implementation of <see cref="IMessageAggregateStore{T}"/>.
/// </summary>
/// <remarks>
/// State is held in memory and does not survive process restarts. This is appropriate
/// for development, testing, and scenarios where the surrounding workflow (e.g. Temporal)
/// guarantees re-delivery of all messages on failure. For durable production deployments
/// replace this with a Cassandra- or Redis-backed store.
/// </remarks>
/// <typeparam name="T">The payload type of the individual messages.</typeparam>
public sealed class InMemoryMessageAggregateStore<T> : IMessageAggregateStore<T>
{
    private readonly ConcurrentDictionary<Guid, List<IntegrationEnvelope<T>>> _groups = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<IntegrationEnvelope<T>>> AddAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var group = _groups.GetOrAdd(envelope.CorrelationId, _ => []);

        IReadOnlyList<IntegrationEnvelope<T>> snapshot;
        lock (group)
        {
            // Idempotent on MessageId — skip duplicates from redelivered messages.
            if (group.Any(e => e.MessageId == envelope.MessageId))
            {
                snapshot = group.AsReadOnly();
            }
            else
            {
                group.Add(envelope);
                snapshot = group.AsReadOnly();
            }
        }

        return Task.FromResult(snapshot);
    }

    /// <inheritdoc />
    public Task RemoveGroupAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        _groups.TryRemove(correlationId, out _);
        return Task.CompletedTask;
    }
}
