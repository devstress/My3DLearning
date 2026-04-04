using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Selective Consumer — wraps a message consumer with a predicate filter, consuming
/// only messages that match the specified criteria. Messages that do not match are
/// silently skipped (not redelivered to this consumer).
///
/// <para>
/// EIP Pattern: <c>Selective Consumer</c> (Chapter 10, p. 515 of Enterprise Integration Patterns).
/// </para>
/// </summary>
public interface ISelectiveConsumer : IAsyncDisposable
{
    /// <summary>
    /// Subscribes to the specified topic but only invokes the handler for messages
    /// that satisfy the <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="topic">Topic or subject to subscribe to.</param>
    /// <param name="consumerGroup">Logical consumer group name.</param>
    /// <param name="predicate">
    /// Filter predicate. Only messages for which this returns <c>true</c> are passed to the handler.
    /// </param>
    /// <param name="handler">Callback invoked for each matching message.</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription.</param>
    Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, bool> predicate,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
