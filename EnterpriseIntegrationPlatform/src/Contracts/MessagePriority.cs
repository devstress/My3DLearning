namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Indicates the relative importance of a message.
/// Consumers may use this value to order or partition their processing queues.
/// </summary>
public enum MessagePriority
{
    /// <summary>Background or batch processing – processed after all higher-priority messages.</summary>
    Low = 0,

    /// <summary>Standard business message – the default priority level.</summary>
    Normal = 1,

    /// <summary>Time-sensitive message that should be processed before <see cref="Normal"/> messages.</summary>
    High = 2,

    /// <summary>Urgent message requiring immediate processing; bypasses ordinary queuing.</summary>
    Critical = 3,
}
