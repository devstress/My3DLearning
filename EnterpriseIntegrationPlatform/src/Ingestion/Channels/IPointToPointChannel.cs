using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Point-to-Point Channel — ensures each message on a channel is consumed by exactly
/// one consumer in the consumer group (queue semantics). This wraps the broker's
/// consumer-group / queue-group delivery model.
/// </summary>
public interface IPointToPointChannel
{
    /// <summary>
    /// Sends a message to the point-to-point channel. Exactly one consumer
    /// in the target consumer group will receive it.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The canonical message envelope.</param>
    /// <param name="channel">Logical channel (topic/subject) name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        string channel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to the point-to-point channel within a consumer group.
    /// Each message is delivered to exactly one consumer in the group.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="channel">Logical channel (topic/subject) name.</param>
    /// <param name="consumerGroup">Consumer group ensuring point-to-point delivery.</param>
    /// <param name="handler">Callback invoked for each received message.</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription.</param>
    Task ReceiveAsync<T>(
        string channel,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
