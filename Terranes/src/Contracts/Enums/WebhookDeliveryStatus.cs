namespace Terranes.Contracts.Enums;

/// <summary>
/// Delivery status of a webhook call.
/// </summary>
public enum WebhookDeliveryStatus
{
    /// <summary>Delivery is pending.</summary>
    Pending,

    /// <summary>Successfully delivered (HTTP 2xx).</summary>
    Delivered,

    /// <summary>Delivery failed after retries.</summary>
    Failed,

    /// <summary>Delivery is being retried.</summary>
    Retrying
}
