using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Analytics;

/// <summary>
/// In-memory implementation of <see cref="IAnalyticsService"/>.
/// Tracks user engagement events and provides aggregated summaries.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly ConcurrentDictionary<Guid, AnalyticsEvent> _events = new();
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(ILogger<AnalyticsService> logger) => _logger = logger;

    public Task<AnalyticsEvent> TrackAsync(Guid userId, Guid tenantId, AnalyticsEventType eventType, Guid? entityId = null, string? metadata = null, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        var evt = new AnalyticsEvent(
            Id: Guid.NewGuid(),
            UserId: userId,
            TenantId: tenantId,
            EventType: eventType,
            EntityId: entityId,
            Metadata: metadata,
            TimestampUtc: DateTimeOffset.UtcNow);

        if (!_events.TryAdd(evt.Id, evt))
            throw new InvalidOperationException("Event ID conflict.");

        _logger.LogInformation("Tracked {EventType} event for user {UserId}", eventType, userId);
        return Task.FromResult(evt);
    }

    public Task<IReadOnlyList<AnalyticsEvent>> GetUserEventsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AnalyticsEvent> result = _events.Values
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<AnalyticsSummary> GetSummaryAsync(AnalyticsEventType eventType, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var matching = _events.Values
            .Where(e => e.EventType == eventType && e.TimestampUtc >= from && e.TimestampUtc <= to)
            .ToList();

        var summary = new AnalyticsSummary(
            EventType: eventType,
            Count: matching.Count,
            UniqueUsers: matching.Select(e => e.UserId).Distinct().Count(),
            PeriodStartUtc: from,
            PeriodEndUtc: to);

        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<(Guid EntityId, int Count)>> GetPopularEntitiesAsync(AnalyticsEventType eventType, int top = 10, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<(Guid EntityId, int Count)> result = _events.Values
            .Where(e => e.EventType == eventType && e.EntityId.HasValue)
            .GroupBy(e => e.EntityId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(top)
            .Select(g => (g.Key, g.Count()))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> GetTotalEventCountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_events.Count);
}
