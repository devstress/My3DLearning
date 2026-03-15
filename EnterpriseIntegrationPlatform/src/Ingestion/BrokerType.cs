namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Identifies the message broker implementation used for message ingestion.
/// The broker is selected at deployment time via configuration.
/// </summary>
public enum BrokerType
{
    /// <summary>
    /// NATS JetStream — lightweight, cloud-native single binary with per-subject
    /// filtering and queue groups that avoids Head-of-Line blocking between subjects.
    /// Default for local development, testing, and cloud deployments.
    /// </summary>
    NatsJetStream = 0,

    /// <summary>
    /// Apache Kafka — high-throughput, ordered, long-retention event streaming.
    /// Best suited for broadcast event streams, audit logs, fan-out analytics,
    /// and decoupled integration.
    /// </summary>
    Kafka = 1,

    /// <summary>
    /// Apache Pulsar with Key_Shared subscription — distributes messages by key
    /// (e.g., recipientId) across consumers. All messages for recipient A stay
    /// ordered while recipient B is processed by another consumer.
    /// Suitable for large-scale on-prem production deployments.
    /// </summary>
    Pulsar = 2,
}
