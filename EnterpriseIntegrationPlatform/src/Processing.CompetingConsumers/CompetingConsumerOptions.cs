namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Configuration options for the competing consumers pattern.
/// Bound from the <c>CompetingConsumers</c> configuration section.
/// </summary>
public sealed class CompetingConsumerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "CompetingConsumers";

    /// <summary>
    /// Minimum number of consumer instances to maintain.
    /// </summary>
    public int MinConsumers { get; set; } = 1;

    /// <summary>
    /// Maximum number of consumer instances allowed.
    /// </summary>
    public int MaxConsumers { get; set; } = 10;

    /// <summary>
    /// Consumer lag threshold (in messages) that triggers a scale-up decision.
    /// </summary>
    public long ScaleUpThreshold { get; set; } = 1000;

    /// <summary>
    /// Consumer lag threshold (in messages) that triggers a scale-down decision.
    /// </summary>
    public long ScaleDownThreshold { get; set; } = 100;

    /// <summary>
    /// Cooldown period in milliseconds between consecutive scaling decisions to prevent flapping.
    /// </summary>
    public int CooldownMs { get; set; } = 30_000;

    /// <summary>
    /// The topic to consume from.
    /// </summary>
    public string TargetTopic { get; set; } = string.Empty;

    /// <summary>
    /// The consumer group identifier.
    /// </summary>
    public string ConsumerGroup { get; set; } = string.Empty;
}
