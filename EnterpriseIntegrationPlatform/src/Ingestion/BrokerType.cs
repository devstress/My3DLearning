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

    /// <summary>
    /// PostgreSQL — uses a relational table as the message store with pg_notify
    /// for low-latency push delivery and SELECT … FOR UPDATE SKIP LOCKED for
    /// competing consumers. Ideal for lower-scale deployments (≤ 5,000 TPS)
    /// where teams already run Postgres and want to avoid a dedicated broker.
    /// Provides native ACID transactions via NpgsqlTransaction.
    /// </summary>
    Postgres = 3,

    /// <summary>
    /// Northguard — LinkedIn's next-generation log storage engine that replaces
    /// Kafka for ultra-high-scale workloads (32 trillion records/day, 17 PB+).
    /// Uses fine-grained log striping (segments and ranges) instead of monolithic
    /// partitions, with a Raft-backed sharded metadata layer for decentralised
    /// coordination. Automatic load balancing eliminates stop-the-world rebalances.
    /// Best suited for massive-scale event streaming where Kafka's centralised
    /// controller and partition-based replication become operational bottlenecks.
    /// Accessed via the Xinfra virtualised pub/sub gateway.
    /// </summary>
    Northguard = 4,
}
