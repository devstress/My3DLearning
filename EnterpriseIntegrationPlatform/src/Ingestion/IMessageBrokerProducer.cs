using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Publishes <see cref="IntegrationEnvelope{T}"/> messages to a named topic
/// on the configured message broker.
/// </summary>
public interface IMessageBrokerProducer
{
    /// <summary>
    /// Publishes a message envelope to the specified topic.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The canonical message envelope to publish.</param>
    /// <param name="topic">Target topic or subject name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default);
}
