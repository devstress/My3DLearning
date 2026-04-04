namespace EnterpriseIntegrationPlatform.Processing.Resequencer;

/// <summary>
/// Configuration options for the <see cref="MessageResequencer"/>.
/// </summary>
public sealed class ResequencerOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Resequencer";

    /// <summary>
    /// Maximum time to buffer an incomplete sequence before releasing whatever has arrived.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan ReleaseTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of concurrent sequences to buffer. Prevents unbounded memory growth.
    /// Default is 10,000.
    /// </summary>
    public int MaxConcurrentSequences { get; init; } = 10_000;
}
