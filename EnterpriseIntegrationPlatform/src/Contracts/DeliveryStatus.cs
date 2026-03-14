namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Represents the delivery lifecycle of a message as it flows through the platform.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>The message has been created and is waiting to be picked up.</summary>
    Pending = 0,

    /// <summary>The message has been consumed by a processor and is currently being handled.</summary>
    InFlight = 1,

    /// <summary>The message was processed successfully.</summary>
    Delivered = 2,

    /// <summary>Processing failed on the most recent attempt.</summary>
    Failed = 3,

    /// <summary>The message is being retried after a previous failure.</summary>
    Retrying = 4,

    /// <summary>All retry attempts have been exhausted; the message has been moved to the dead-letter store.</summary>
    DeadLettered = 5,
}
