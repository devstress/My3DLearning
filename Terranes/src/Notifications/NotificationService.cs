using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Notifications;

/// <summary>
/// In-memory implementation of <see cref="INotificationService"/>.
/// Manages notification creation, delivery tracking, and read status.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ConcurrentDictionary<Guid, Notification> _notifications = new();
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger) => _logger = logger;

    public Task<Notification> SendAsync(Guid recipientId, NotificationType type, string title, string message, Guid? entityId = null, CancellationToken cancellationToken = default)
    {
        if (recipientId == Guid.Empty)
            throw new ArgumentException("Recipient ID is required.", nameof(recipientId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        var now = DateTimeOffset.UtcNow;
        var notification = new Notification(
            Id: Guid.NewGuid(),
            RecipientId: recipientId,
            Type: type,
            Title: title,
            Message: message,
            Status: NotificationStatus.Delivered,
            EntityId: entityId,
            CreatedAtUtc: now,
            DeliveredAtUtc: now);

        if (!_notifications.TryAdd(notification.Id, notification))
            throw new InvalidOperationException("Notification ID conflict.");

        _logger.LogInformation("Sent notification {NotificationId} to {RecipientId}", notification.Id, recipientId);
        return Task.FromResult(notification);
    }

    public Task<Notification?> GetNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        _notifications.TryGetValue(notificationId, out var notification);
        return Task.FromResult(notification);
    }

    public Task<IReadOnlyList<Notification>> GetNotificationsForRecipientAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Notification> result = _notifications.Values
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Notification>> GetUnreadAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Notification> result = _notifications.Values
            .Where(n => n.RecipientId == recipientId && n.Status == NotificationStatus.Delivered)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<Notification> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (!_notifications.TryGetValue(notificationId, out var existing))
            throw new InvalidOperationException($"Notification {notificationId} not found.");

        if (existing.Status == NotificationStatus.Read)
            return Task.FromResult(existing);

        var updated = existing with { Status = NotificationStatus.Read };
        _notifications[notificationId] = updated;

        _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);
        return Task.FromResult(updated);
    }
}
