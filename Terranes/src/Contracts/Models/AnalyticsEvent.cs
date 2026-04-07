using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents an analytics event tracked by the platform.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="UserId">User who triggered the event.</param>
/// <param name="TenantId">Tenant context.</param>
/// <param name="EventType">Type of analytics event.</param>
/// <param name="EntityId">Optional related entity identifier.</param>
/// <param name="Metadata">Optional key-value metadata (JSON string).</param>
/// <param name="TimestampUtc">UTC timestamp when the event occurred.</param>
public sealed record AnalyticsEvent(
    Guid Id,
    Guid UserId,
    Guid TenantId,
    AnalyticsEventType EventType,
    Guid? EntityId,
    string? Metadata,
    DateTimeOffset TimestampUtc);
