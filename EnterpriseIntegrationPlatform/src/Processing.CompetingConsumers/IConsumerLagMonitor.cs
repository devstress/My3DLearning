namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Monitors consumer lag for a given topic and consumer group.
/// </summary>
public interface IConsumerLagMonitor
{
    /// <summary>
    /// Retrieves the current lag information for the specified topic and consumer group.
    /// </summary>
    /// <param name="topic">The topic to query.</param>
    /// <param name="consumerGroup">The consumer group identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The current consumer lag information.</returns>
    Task<ConsumerLagInfo> GetLagAsync(string topic, string consumerGroup, CancellationToken cancellationToken);
}
