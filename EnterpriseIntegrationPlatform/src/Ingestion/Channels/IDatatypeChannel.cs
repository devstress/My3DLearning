using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Datatype Channel — automatically resolves the target topic/subject from
/// <see cref="IntegrationEnvelope{T}.MessageType"/>. Each distinct message type
/// flows on its own dedicated channel, ensuring type-safe consumption.
/// </summary>
public interface IDatatypeChannel
{
    /// <summary>
    /// Publishes a message to the channel derived from its <see cref="IntegrationEnvelope{T}.MessageType"/>.
    /// The topic name is resolved as <c>{prefix}.{messageType}</c>.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The canonical message envelope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the channel (topic/subject) name for a given message type.
    /// </summary>
    /// <param name="messageType">The logical message type name.</param>
    /// <returns>The resolved channel name.</returns>
    string ResolveChannel(string messageType);
}
