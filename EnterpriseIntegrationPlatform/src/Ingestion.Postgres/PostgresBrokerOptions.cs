namespace EnterpriseIntegrationPlatform.Ingestion.Postgres;

/// <summary>
/// Configuration options for the PostgreSQL message broker.
/// </summary>
public sealed class PostgresBrokerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Broker:Postgres";

    /// <summary>
    /// Npgsql connection string.
    /// Example: <c>Host=localhost;Port=5432;Database=eip;Username=eip;Password=eip</c>.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Polling interval in milliseconds for consumer fallback when pg_notify is missed.
    /// Default: 1000 ms. Lower values reduce latency but increase DB load.
    /// </summary>
    public int PollIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of messages fetched in a single poll batch.
    /// Default: 100.
    /// </summary>
    public int PollBatchSize { get; set; } = 100;

    /// <summary>
    /// Lock duration in seconds for competing consumer row locks.
    /// If a consumer crashes without ACKing, the row becomes available
    /// after this timeout. Default: 30 seconds.
    /// </summary>
    public int LockTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retention period in hours for delivered messages before cleanup.
    /// Default: 24 hours. Set to 0 to disable automatic cleanup.
    /// </summary>
    public int RetentionHours { get; set; } = 24;
}
