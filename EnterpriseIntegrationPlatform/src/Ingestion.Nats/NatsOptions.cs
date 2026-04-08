namespace EnterpriseIntegrationPlatform.Ingestion.Nats;

/// <summary>
/// Configuration options for the NATS JetStream message broker provider.
/// Bound from the <c>NatsJetStream</c> configuration section via IOptions pattern.
/// </summary>
public sealed class NatsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "NatsJetStream";

    /// <summary>NATS server URL. Default: <c>nats://localhost:15222</c>.</summary>
    public string Url { get; set; } = "nats://localhost:15222";

    /// <summary>Maximum number of retry attempts for transient JetStream API operations (stream creation, get). Default: 3.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay in milliseconds between retry attempts (multiplied by attempt number for linear backoff). Default: 1000.</summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Validates the options and throws <see cref="ArgumentException"/> if invalid.
    /// </summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Url);
        if (MaxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxRetries), MaxRetries, "MaxRetries must be non-negative.");
        if (RetryDelayMs < 0)
            throw new ArgumentOutOfRangeException(nameof(RetryDelayMs), RetryDelayMs, "RetryDelayMs must be non-negative.");
    }
}
