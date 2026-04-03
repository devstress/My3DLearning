namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Service interface for notification-related Temporal activities.
/// Implementations publish Ack/Nack messages to the configured message broker
/// to complete the atomic notification loopback.
/// </summary>
public interface INotificationActivityService
{
    /// <summary>
    /// Publishes an Ack notification for a successfully delivered message.
    /// </summary>
    /// <param name="messageId">The delivered message identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="topic">The Ack subject/topic.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAckAsync(
        Guid messageId,
        Guid correlationId,
        string topic,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a Nack notification for a message that failed processing.
    /// </summary>
    /// <param name="messageId">The faulted message identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="reason">Human-readable failure reason.</param>
    /// <param name="topic">The Nack subject/topic.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishNackAsync(
        Guid messageId,
        Guid correlationId,
        string reason,
        string topic,
        CancellationToken cancellationToken = default);
}
