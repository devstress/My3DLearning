namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Configuration options for the message processing throttle.
/// Bound from the <c>Throttle</c> configuration section.
/// </summary>
/// <remarks>
/// Throttling controls how fast messages are <em>processed</em> by delaying
/// or queuing them — it does <strong>not</strong> reject requests (unlike rate
/// limiting, which returns HTTP 429).
/// </remarks>
public sealed class ThrottleOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Throttle";

    /// <summary>
    /// Maximum messages processed per second (token-bucket refill rate).
    /// Defaults to 100 messages/second.
    /// </summary>
    public int MaxMessagesPerSecond { get; set; } = 100;

    /// <summary>
    /// Maximum burst size — the token bucket capacity.
    /// Allows short bursts above <see cref="MaxMessagesPerSecond"/>.
    /// Defaults to 200 tokens.
    /// </summary>
    public int BurstCapacity { get; set; } = 200;

    /// <summary>
    /// Maximum time a message may wait in the throttle queue before it is
    /// considered timed out. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When <c>true</c> and the queue is full, the throttle signals backpressure
    /// to the caller instead of waiting. Defaults to <c>false</c> (wait).
    /// </summary>
    public bool RejectOnBackpressure { get; set; }
}
