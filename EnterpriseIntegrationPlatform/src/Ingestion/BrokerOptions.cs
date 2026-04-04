namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Configuration options for the message broker layer.
/// Bound from the <c>Broker</c> configuration section.
/// </summary>
public sealed class BrokerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Broker";

    /// <summary>
    /// The broker implementation to use for message ingestion.
    /// Defaults to <see cref="BrokerType.NatsJetStream"/>.
    /// </summary>
    public BrokerType BrokerType { get; set; } = BrokerType.NatsJetStream;

    /// <summary>
    /// Connection string or URL for the broker.
    /// For NATS: <c>nats://localhost:15222</c>.
    /// For Kafka: <c>localhost:9092</c>.
    /// For Pulsar: <c>pulsar://localhost:6650</c>.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum time in seconds for a transactional client operation to complete.
    /// If the transaction does not commit within this window, it is aborted and
    /// published messages are compensated. Defaults to 30 seconds.
    /// </summary>
    public int TransactionTimeoutSeconds { get; set; } = 30;
}
