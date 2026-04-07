namespace Terranes.Contracts.Enums;

/// <summary>
/// Delivery status of a notification.
/// </summary>
public enum NotificationStatus
{
    /// <summary>Notification is queued for delivery.</summary>
    Queued,

    /// <summary>Notification has been delivered.</summary>
    Delivered,

    /// <summary>Notification has been read by the recipient.</summary>
    Read,

    /// <summary>Delivery failed.</summary>
    Failed
}
