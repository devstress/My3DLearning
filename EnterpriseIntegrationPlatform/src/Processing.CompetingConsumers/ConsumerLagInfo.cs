namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Represents a point-in-time consumer lag measurement for a specific consumer group and topic.
/// </summary>
/// <param name="ConsumerGroup">The consumer group identifier.</param>
/// <param name="Topic">The topic being consumed.</param>
/// <param name="CurrentLag">The current lag in number of unconsumed messages.</param>
/// <param name="Timestamp">The time at which the lag was measured.</param>
public record ConsumerLagInfo(
    string ConsumerGroup,
    string Topic,
    long CurrentLag,
    DateTimeOffset Timestamp);
