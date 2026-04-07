using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents an aggregated analytics summary for a time period.
/// </summary>
/// <param name="EventType">The event type being summarised.</param>
/// <param name="Count">Total number of events in the period.</param>
/// <param name="UniqueUsers">Number of unique users.</param>
/// <param name="PeriodStartUtc">Start of the time period.</param>
/// <param name="PeriodEndUtc">End of the time period.</param>
public sealed record AnalyticsSummary(
    AnalyticsEventType EventType,
    int Count,
    int UniqueUsers,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc);
