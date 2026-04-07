using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a webhook delivery attempt.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="WebhookId">Associated webhook registration.</param>
/// <param name="EventId">The platform event that triggered this delivery.</param>
/// <param name="Status">Delivery status.</param>
/// <param name="AttemptCount">Number of delivery attempts made.</param>
/// <param name="LastAttemptAtUtc">UTC timestamp of the last delivery attempt.</param>
public sealed record WebhookDelivery(
    Guid Id,
    Guid WebhookId,
    Guid EventId,
    WebhookDeliveryStatus Status,
    int AttemptCount,
    DateTimeOffset LastAttemptAtUtc);
