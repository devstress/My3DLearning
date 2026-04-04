namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Drains all messages from a specified topic or subject. Implements the
/// Channel Purger Enterprise Integration Pattern for clearing stale or
/// unwanted messages from a channel.
/// </summary>
public interface IChannelPurger
{
    /// <summary>
    /// Purges all pending messages from the specified topic by consuming
    /// and discarding them. Returns the count of purged messages.
    /// </summary>
    /// <param name="topic">The topic or subject to purge.</param>
    /// <param name="consumerGroup">The consumer group to use for the purge operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ChannelPurgeResult"/> describing the outcome.</returns>
    Task<ChannelPurgeResult> PurgeAsync(
        string topic,
        string consumerGroup,
        CancellationToken cancellationToken = default);
}
