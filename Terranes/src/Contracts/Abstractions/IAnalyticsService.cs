using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Tracks user engagement and platform analytics.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>Tracks an analytics event.</summary>
    Task<AnalyticsEvent> TrackAsync(Guid userId, Guid tenantId, AnalyticsEventType eventType, Guid? entityId = null, string? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>Gets events for a user.</summary>
    Task<IReadOnlyList<AnalyticsEvent>> GetUserEventsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets summary for a specific event type in a time period.</summary>
    Task<AnalyticsSummary> GetSummaryAsync(AnalyticsEventType eventType, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>Gets the most popular entities by event type.</summary>
    Task<IReadOnlyList<(Guid EntityId, int Count)>> GetPopularEntitiesAsync(AnalyticsEventType eventType, int top = 10, CancellationToken cancellationToken = default);

    /// <summary>Gets total event count.</summary>
    Task<int> GetTotalEventCountAsync(CancellationToken cancellationToken = default);
}
