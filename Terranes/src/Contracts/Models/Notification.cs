using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a notification sent to a user or partner.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="RecipientId">Identifier of the recipient (user or partner).</param>
/// <param name="Type">Type of notification.</param>
/// <param name="Title">Short notification title.</param>
/// <param name="Message">Full notification message body.</param>
/// <param name="Status">Current delivery status.</param>
/// <param name="EntityId">Optional related entity (journey, quote, etc.).</param>
/// <param name="CreatedAtUtc">UTC timestamp when notification was created.</param>
/// <param name="DeliveredAtUtc">UTC timestamp when notification was delivered, or null.</param>
public sealed record Notification(
    Guid Id,
    Guid RecipientId,
    NotificationType Type,
    string Title,
    string Message,
    NotificationStatus Status,
    Guid? EntityId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DeliveredAtUtc);
