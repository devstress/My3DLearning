using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Event-Driven Consumer — receives messages automatically via push-based subscription.
/// The broker pushes messages to the consumer as they arrive. Maps to NATS JetStream's
/// push-based subscription and Pulsar's Key_Shared subscription model.
///
/// <para>
/// EIP Pattern: <c>Event-Driven Consumer</c> (Chapter 10, p. 498 of Enterprise Integration Patterns).
/// </para>
/// </summary>
public interface IEventDrivenConsumer : IAsyncDisposable
{
    /// <summary>
    /// Starts receiving messages from the specified topic. The handler is invoked
    /// automatically for each message pushed by the broker. The subscription
    /// runs until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="topic">Topic or subject to subscribe to.</param>
    /// <param name="consumerGroup">Logical consumer group name.</param>
    /// <param name="handler">Callback invoked for each received message.</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription.</param>
    Task StartAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
