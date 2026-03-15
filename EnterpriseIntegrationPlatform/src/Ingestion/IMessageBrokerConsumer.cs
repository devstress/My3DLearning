using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Consumes <see cref="IntegrationEnvelope{T}"/> messages from a named topic
/// on the configured message broker.
/// </summary>
public interface IMessageBrokerConsumer : IAsyncDisposable
{
    /// <summary>
    /// Subscribes to the specified topic and invokes <paramref name="handler"/>
    /// for each received message. The subscription runs until
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="topic">Topic or subject to subscribe to.</param>
    /// <param name="consumerGroup">
    /// Logical consumer group name. Within a group, each message is delivered
    /// to exactly one consumer — ensuring recipient A does not block recipient B.
    /// </param>
    /// <param name="handler">Callback invoked for each received message.</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription.</param>
    Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
