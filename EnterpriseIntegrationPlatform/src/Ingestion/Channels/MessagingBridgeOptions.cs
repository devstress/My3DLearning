namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Configuration options for <see cref="MessagingBridge"/>.
/// </summary>
public sealed class MessagingBridgeOptions
{
    /// <summary>
    /// Consumer group name for the source subscription. Default is <c>"messaging-bridge"</c>.
    /// </summary>
    public string ConsumerGroup { get; set; } = "messaging-bridge";

    /// <summary>
    /// Maximum number of message IDs to track for deduplication.
    /// Once the window is full, the oldest IDs are evicted. Default is <c>10000</c>.
    /// </summary>
    public int DeduplicationWindowSize { get; set; } = 10_000;
}
