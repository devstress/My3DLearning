namespace EnterpriseIntegrationPlatform.Ingestion.Kafka;

/// <summary>
/// Configuration options for the Apache Kafka message broker provider.
/// Bound from the <c>Kafka</c> configuration section via IOptions pattern.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Kafka";

    /// <summary>Kafka bootstrap servers (comma-separated). Default: <c>localhost:9092</c>.</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Number of acknowledgements the producer requires from the broker before considering a request complete.
    /// <c>all</c> = all in-sync replicas must acknowledge (strongest guarantee).
    /// Default: <c>all</c>.
    /// </summary>
    public string Acks { get; set; } = "all";

    /// <summary>
    /// When <c>true</c>, the producer will ensure that exactly one copy of each message is written
    /// (idempotent delivery). Requires Acks=all. Default: <c>true</c>.
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>
    /// Compression codec for producer messages. Default: <c>none</c>.
    /// Valid values: none, gzip, snappy, lz4, zstd.
    /// </summary>
    public string CompressionType { get; set; } = "none";

    /// <summary>
    /// Delay in milliseconds to wait for additional messages before sending a batch.
    /// Higher values improve throughput at the cost of latency. Default: 5.
    /// </summary>
    public int LingerMs { get; set; } = 5;

    /// <summary>
    /// Maximum size of a batch in bytes. Default: 16384 (16 KB).
    /// </summary>
    public int BatchSize { get; set; } = 16384;

    /// <summary>
    /// Consumer group session timeout in milliseconds. Default: 45000 (45 seconds).
    /// </summary>
    public int SessionTimeoutMs { get; set; } = 45000;

    /// <summary>
    /// Default consumer group ID. Overridden per subscription. Default: <c>eip-default</c>.
    /// </summary>
    public string GroupId { get; set; } = "eip-default";

    /// <summary>
    /// Consumer auto offset reset policy. Default: <c>earliest</c>.
    /// </summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>
    /// Whether the consumer auto-commits offsets. Default: <c>false</c> (manual commit).
    /// </summary>
    public bool EnableAutoCommit { get; set; }

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BootstrapServers);
        ArgumentException.ThrowIfNullOrWhiteSpace(Acks);
        if (LingerMs < 0)
            throw new ArgumentOutOfRangeException(nameof(LingerMs), LingerMs, "LingerMs must be non-negative.");
        if (BatchSize < 0)
            throw new ArgumentOutOfRangeException(nameof(BatchSize), BatchSize, "BatchSize must be non-negative.");
        if (SessionTimeoutMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(SessionTimeoutMs), SessionTimeoutMs, "SessionTimeoutMs must be positive.");
    }
}
