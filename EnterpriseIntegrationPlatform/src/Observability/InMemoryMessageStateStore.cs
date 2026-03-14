using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IMessageStateStore"/>.
/// Suitable for development, testing, and single-instance deployments.
/// For production use, replace with a durable implementation backed by
/// Cassandra or another distributed database.
/// </summary>
public sealed class InMemoryMessageStateStore : IMessageStateStore
{
    private readonly ConcurrentDictionary<Guid, List<MessageEvent>> _byCorrelation = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _businessKeyIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, List<MessageEvent>> _byMessageId = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // Index by correlation ID
            var correlationList = _byCorrelation.GetOrAdd(messageEvent.CorrelationId, _ => []);
            correlationList.Add(messageEvent);

            // Index by message ID
            var messageList = _byMessageId.GetOrAdd(messageEvent.MessageId, _ => []);
            messageList.Add(messageEvent);

            // Index by business key
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
    public Task<IReadOnlyList<MessageEvent>> GetByMessageIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        if (_byMessageId.TryGetValue(messageId, out var events))
        {
            lock (_lock)
            {
                IReadOnlyList<MessageEvent> result = events.OrderBy(e => e.RecordedAt).ToList();
                return Task.FromResult(result);
            }
        }

        return Task.FromResult<IReadOnlyList<MessageEvent>>(Array.Empty<MessageEvent>());
    }

    /// <inheritdoc />
    public Task<MessageEvent?> GetLatestByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (_byCorrelation.TryGetValue(correlationId, out var events))
        {
            lock (_lock)
            {
                var latest = events.OrderByDescending(e => e.RecordedAt).FirstOrDefault();
                return Task.FromResult(latest);
            }
        }

        return Task.FromResult<MessageEvent?>(null);
    }
}
