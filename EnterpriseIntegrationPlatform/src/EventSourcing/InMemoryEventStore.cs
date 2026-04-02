using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="IEventStore"/> backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>. Enforces optimistic concurrency on append.
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, List<EventEnvelope>> _streams = new();
    private readonly object _writeLock = new();
    private readonly int _maxEventsPerRead;
    private readonly ILogger<InMemoryEventStore> _logger;

    /// <summary>Initialises a new instance of <see cref="InMemoryEventStore"/>.</summary>
    public InMemoryEventStore(IOptions<EventSourcingOptions> options, ILogger<InMemoryEventStore> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _maxEventsPerRead = options.Value.MaxEventsPerRead;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<long> AppendAsync(string streamId, IReadOnlyList<EventEnvelope> events, long expectedVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            throw new ArgumentException("At least one event must be provided.", nameof(events));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_writeLock)
        {
            var stream = _streams.GetOrAdd(streamId, _ => []);
            var currentVersion = (long)stream.Count;

            if (currentVersion != expectedVersion)
            {
                throw new OptimisticConcurrencyException(streamId, expectedVersion, currentVersion);
            }

            var newVersion = currentVersion;
            foreach (var e in events)
            {
                newVersion++;
                var stamped = e with { Version = newVersion, StreamId = streamId };
                stream.Add(stamped);
            }

            _logger.LogDebug(
                "Appended {Count} event(s) to stream '{StreamId}'; version {OldVersion} → {NewVersion}",
                events.Count, streamId, currentVersion, newVersion);

            return Task.FromResult(newVersion);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(string streamId, long fromVersion, int count, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        cancellationToken.ThrowIfCancellationRequested();

        var effectiveCount = Math.Min(count, _maxEventsPerRead);

        if (!_streams.TryGetValue(streamId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<EventEnvelope>>(Array.Empty<EventEnvelope>());
        }

        List<EventEnvelope> snapshot;
        lock (_writeLock)
        {
            snapshot = [.. stream];
        }

        var result = snapshot
            .Where(e => e.Version >= fromVersion)
            .OrderBy(e => e.Version)
            .Take(effectiveCount)
            .ToList();

        return Task.FromResult<IReadOnlyList<EventEnvelope>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EventEnvelope>> ReadStreamBackwardAsync(string streamId, long fromVersion, int count, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        cancellationToken.ThrowIfCancellationRequested();

        var effectiveCount = Math.Min(count, _maxEventsPerRead);

        if (!_streams.TryGetValue(streamId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<EventEnvelope>>(Array.Empty<EventEnvelope>());
        }

        List<EventEnvelope> snapshot;
        lock (_writeLock)
        {
            snapshot = [.. stream];
        }

        var result = snapshot
            .Where(e => e.Version <= fromVersion)
            .OrderByDescending(e => e.Version)
            .Take(effectiveCount)
            .ToList();

        return Task.FromResult<IReadOnlyList<EventEnvelope>>(result);
    }
}
