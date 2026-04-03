using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Messaging Bridge — forwards messages between two different broker instances
/// (e.g., Kafka→NATS, NATS→Pulsar) with envelope preservation and deduplication.
/// Enables cross-broker communication without modifying producers or consumers.
/// </summary>
public interface IMessagingBridge : IAsyncDisposable
{
    /// <summary>
    /// Starts the bridge: subscribes to the source broker channel and forwards
    /// each received message to the target broker channel with deduplication.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="sourceChannel">Channel name on the source broker.</param>
    /// <param name="targetChannel">Channel name on the target broker.</param>
    /// <param name="cancellationToken">Cancellation token to stop the bridge.</param>
    Task StartAsync<T>(
        string sourceChannel,
        string targetChannel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of messages successfully forwarded since the bridge started.
    /// </summary>
    long ForwardedCount { get; }

    /// <summary>
    /// Gets the number of duplicate messages detected and skipped.
    /// </summary>
    long DuplicateCount { get; }
}
