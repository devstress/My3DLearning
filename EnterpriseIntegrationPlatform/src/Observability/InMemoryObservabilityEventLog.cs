using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IObservabilityEventLog"/>.
/// This is the <b>observability-layer</b> event store — isolated from the
/// production <see cref="IMessageStateStore"/>.
/// <para>
/// For production use, replace with an implementation backed by
/// Elasticsearch (ELK), Loki, or Seq for durable, queryable log storage.
/// Metrics observability is handled separately by Prometheus via the
/// <c>/metrics</c> scraping endpoint.
/// </para>
/// </summary>
public sealed class InMemoryObservabilityEventLog : IObservabilityEventLog
{
    private readonly ConcurrentDictionary<Guid, List<MessageEvent>> _byCorrelation = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _businessKeyIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var correlationList = _byCorrelation.GetOrAdd(messageEvent.CorrelationId, _ => []);
            correlationList.Add(messageEvent);

            if (!string.IsNullOrWhiteSpace(messageEvent.BusinessKey))
            {
                var keys = _businessKeyIndex.GetOrAdd(messageEvent.BusinessKey, _ => []);
                if (!keys.Contains(messageEvent.CorrelationId))
                {
                    keys.Add(messageEvent.CorrelationId);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey,
        CancellationToken cancellationToken = default)
    {
        if (_businessKeyIndex.TryGetValue(businessKey, out var correlationIds))
        {
            lock (_lock)
            {
                var events = correlationIds
                    .Where(id => _byCorrelation.ContainsKey(id))
                    .SelectMany(id => _byCorrelation[id])
                    .OrderBy(e => e.RecordedAt)
                    .ToList();

                return Task.FromResult<IReadOnlyList<MessageEvent>>(events);
            }
        }

        return Task.FromResult<IReadOnlyList<MessageEvent>>(Array.Empty<MessageEvent>());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (_byCorrelation.TryGetValue(correlationId, out var events))
        {
            lock (_lock)
            {
                IReadOnlyList<MessageEvent> result = events.OrderBy(e => e.RecordedAt).ToList();
                return Task.FromResult(result);
            }
        }

        return Task.FromResult<IReadOnlyList<MessageEvent>>(Array.Empty<MessageEvent>());
    }
}
