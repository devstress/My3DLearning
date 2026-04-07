using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Manages notifications sent to users and partners.
/// </summary>
public interface INotificationService
{
    /// <summary>Sends a notification to a recipient.</summary>
    Task<Notification> SendAsync(Guid recipientId, NotificationType type, string title, string message, Guid? entityId = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a notification by ID.</summary>
    Task<Notification?> GetNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>Gets all notifications for a recipient.</summary>
    Task<IReadOnlyList<Notification>> GetNotificationsForRecipientAsync(Guid recipientId, CancellationToken cancellationToken = default);

    /// <summary>Gets unread notifications for a recipient.</summary>
    Task<IReadOnlyList<Notification>> GetUnreadAsync(Guid recipientId, CancellationToken cancellationToken = default);

    /// <summary>Marks a notification as read.</summary>
    Task<Notification> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
