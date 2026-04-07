using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Notifications;

/// <summary>
/// In-memory implementation of <see cref="IEventBusService"/>.
/// Provides a simple pub/sub event bus for cross-service communication.
/// </summary>
public sealed class EventBusService : IEventBusService
{
    private readonly ConcurrentDictionary<Guid, PlatformEvent> _events = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<Guid>> _topicIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<EventBusService> _logger;

    public EventBusService(ILogger<EventBusService> logger) => _logger = logger;

    public Task<PlatformEvent> PublishAsync(string topic, string payload, Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic is required.", nameof(topic));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation ID is required.", nameof(correlationId));

        var evt = new PlatformEvent(
            Id: Guid.NewGuid(),
            Topic: topic,
            Payload: payload,
            CorrelationId: correlationId,
            PublishedAtUtc: DateTimeOffset.UtcNow);

        if (!_events.TryAdd(evt.Id, evt))
            throw new InvalidOperationException("Event ID conflict.");

        _topicIndex.AddOrUpdate(
            topic,
            _ => [evt.Id],
            (_, bag) => { bag.Add(evt.Id); return bag; });

        _logger.LogInformation("Published event {EventId}", evt.Id);
        return Task.FromResult(evt);
    }

    public Task<IReadOnlyList<PlatformEvent>> GetEventsForTopicAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (!_topicIndex.TryGetValue(topic, out var eventIds))
            return Task.FromResult<IReadOnlyList<PlatformEvent>>([]);

        IReadOnlyList<PlatformEvent> result = eventIds
            .Select(id => _events.TryGetValue(id, out var evt) ? evt : null)
            .Where(e => e is not null)
            .OrderByDescending(e => e!.PublishedAtUtc)
            .ToList()!;
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<PlatformEvent>> GetEventsForCorrelationAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlatformEvent> result = _events.Values
            .Where(e => e.CorrelationId == correlationId)
            .OrderByDescending(e => e.PublishedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> GetTotalEventCountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_events.Count);

    public Task<IReadOnlyDictionary<string, int>> GetTopicSummaryAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, int> result = _topicIndex
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        return Task.FromResult(result);
    }
}
