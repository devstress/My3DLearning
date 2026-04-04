using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Durable Subscriber — ensures that subscription state survives consumer restarts.
/// Even if the consumer goes offline, the broker retains undelivered messages so that
/// they are delivered when the consumer reconnects. This is inherent in Kafka consumer
/// groups, NATS JetStream durable consumers, and Pulsar durable subscriptions.
///
/// <para>
/// EIP Pattern: <c>Durable Subscriber</c> (Chapter 10, p. 522 of Enterprise Integration Patterns).
/// </para>
/// </summary>
public interface IDurableSubscriber : IAsyncDisposable
{
    /// <summary>
    /// Starts a durable subscription to the specified topic. The subscription state
    /// is identified by <paramref name="subscriptionName"/> and survives restarts.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="topic">Topic or subject to subscribe to.</param>
    /// <param name="subscriptionName">
    /// Durable subscription name. The broker uses this to track delivery state across restarts.
    /// </param>
    /// <param name="handler">Callback invoked for each received message.</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription.</param>
    Task SubscribeAsync<T>(
        string topic,
        string subscriptionName,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the subscriber is currently connected and receiving messages.
    /// </summary>
    bool IsConnected { get; }
}
