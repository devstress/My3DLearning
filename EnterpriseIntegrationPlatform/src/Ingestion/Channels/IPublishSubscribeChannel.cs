using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Publish-Subscribe Channel — broadcasts each message to ALL subscribers.
/// Unlike Point-to-Point, every subscriber receives a copy. Each subscriber
/// gets its own unique consumer group so the broker treats each as an
/// independent consumer.
/// </summary>
public interface IPublishSubscribeChannel
{
    /// <summary>
    /// Publishes a message to all subscribers on the channel.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The canonical message envelope.</param>
    /// <param name="channel">Logical channel (topic/subject) name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string channel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to the channel. Each subscriber is assigned a unique consumer group
    /// to ensure broadcast (fan-out) delivery — every subscriber receives every message.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="channel">Logical channel (topic/subject) name.</param>
    /// <param name="subscriberId">
    /// Unique subscriber identifier. Used to derive a dedicated consumer group
    /// so this subscriber receives all messages independently of other subscribers.
    /// </param>
    /// <param name="handler">Callback invoked for each received message.</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription.</param>
    Task SubscribeAsync<T>(
        string channel,
        string subscriberId,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
