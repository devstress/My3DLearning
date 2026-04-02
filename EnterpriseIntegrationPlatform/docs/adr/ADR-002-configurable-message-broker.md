# ADR-002: Configurable Message Broker Layer

## Status

**Accepted** — March 2026 (architecture decision made during Phase 1 implementation)

## Context

The Enterprise Integration Platform requires a messaging layer for asynchronous communication between services. The original design used Apache Kafka as the sole message broker ("event-driven Kafka backbone"). However, this approach has fundamental limitations:

### Head-of-Line (HOL) Blocking

Kafka is partitioned and ordered per partition. Within a consumer group, each partition is consumed by exactly one consumer at a time. This creates per-partition serialization — a slow or poison message blocks progress behind it on that partition. In saga patterns with multiple recipients, if Recipient A is down, messages for Recipient B on the same partition are blocked. **Recipient A must not block Recipient B, even at 1 million recipients.**

### Cost at Scale

Kafka topics are expensive. Each topic has partitions, and each partition has replicas. With per-tenant topics at scale (e.g., 1 million tenants), the cost is prohibitive. The number of partitions across a cluster has practical limits that affect controller performance, memory usage, and failover time.

### Kafka's Strength

Kafka excels at what it was designed for: high-throughput, ordered, partitioned event streaming. It is the right choice for broadcast event streams, audit logs, fan-out analytics, and decoupled integration where replay and long retention are valuable.

## Decision

**We adopt a configurable message broker layer** with the right tool for each job:

1. **Kafka** — Retained for broadcast event streams, audit logs, fan-out analytics, and decoupled integration.
2. **NATS JetStream** (default) — For task-oriented message delivery, ingestion, routing, and DLQ processing. NATS uses per-subject queue groups that avoid HOL blocking between recipients. It is a lightweight single binary with a Docker image (`nats:latest`) that runs trivially in Aspire for local development.
3. **Apache Pulsar with Key_Shared** (switchable) — For large-scale production deployments where Pulsar's Key_Shared subscription type provides key-based distribution across consumers, built-in multi-tenancy, tiered storage, and geo-replication.

The broker choice between NATS and Pulsar is a deployment-time configuration switch. Both guarantee that **Recipient A does not block Recipient B, even at 1 million recipients.**

## Rationale

### NATS JetStream as Default

| Criterion                | NATS JetStream              | Kafka (for task delivery)            |
|--------------------------|-----------------------------|--------------------------------------|
| HOL blocking             | No — per-subject independence| Yes — per-partition serialization     |
| Setup complexity         | Single binary, Docker image | Multi-broker cluster                 |
| Local development        | Trivial (`nats:latest`)     | Requires multi-container setup       |
| Per-tenant cost at scale | Low — subjects are free     | High — topics are expensive          |
| Message acknowledgment   | Per-message                 | Offset-based (batch)                 |
| Cloud availability       | Synadia Cloud (managed)     | Confluent Cloud, MSK                 |

### Pulsar Key_Shared as Production Option

| Criterion                | Pulsar Key_Shared               | NATS JetStream                    |
|--------------------------|----------------------------------|-----------------------------------|
| Key-based distribution   | Native Key_Shared subscription  | Subject-based routing             |
| Per-recipient ordering   | Built-in per-key ordering       | Per-subject ordering              |
| Tiered storage           | Hot/warm/cold to object storage | JetStream file/memory storage     |
| Geo-replication          | Built-in                        | Requires NATS super-clusters      |
| Multi-tenancy            | Built-in tenant/namespace model | Subject naming conventions        |
| Setup complexity         | High (ZooKeeper + BookKeeper)   | Low (single binary)               |

### Kafka Retained for Streaming

Kafka remains the best choice for:
- **Audit log streaming** — High-throughput, ordered, with long retention for compliance.
- **Fan-out analytics** — Multiple independent consumer groups reading the same stream.
- **Event replay** — Offset reset enables replaying historical events.
- **Decoupled integration** — Publish/subscribe where consumers process at their own pace.

## Consequences

### Positive

- **No HOL blocking** — Recipient A never blocks Recipient B, regardless of scale.
- **Cost-effective at scale** — Millions of tenants without proportional topic/partition costs.
- **Easy local development** — NATS runs as a single Docker container in Aspire.
- **Production flexibility** — Switch to Pulsar for large-scale on-prem with Key_Shared semantics.
- **Right tool for each job** — Kafka excels at streaming; NATS/Pulsar excels at task delivery.

### Negative

- **Additional infrastructure** — Two message broker technologies to operate (Kafka + NATS or Pulsar).
- **Abstraction layer** — Broker abstraction adds a layer of indirection in the codebase.
- **Team learning curve** — Team must understand both Kafka and NATS/Pulsar operational models.

### Risks and Mitigations

| Risk                              | Mitigation                                                   |
|-----------------------------------|--------------------------------------------------------------|
| Operational complexity of two brokers | NATS is lightweight; Kafka is well-understood; both have managed cloud options |
| Broker abstraction leaking        | Keep abstraction thin; test with both providers in CI        |
| NATS JetStream maturity           | NATS is battle-tested at scale (Synadia, many large deployments) |
| Pulsar operational burden         | Only use Pulsar when scale justifies complexity; default to NATS |

## Alternatives Considered

### Kafka-Only

Rejected due to per-partition HOL blocking and per-tenant topic cost at scale. Kafka is not a universal middleware replacement.

### RabbitMQ

Considered but rejected — RabbitMQ is a mature queue broker but lacks modern features like built-in key-based distribution, tiered storage, and cloud-native single-binary deployment.

### Redis Streams

Considered but rejected — Redis Streams provides per-stream consumer groups but is primarily a data structure server with streaming bolted on, not a purpose-built messaging system.

## References

- [NATS JetStream Documentation](https://docs.nats.io/nats-concepts/jetstream)
- [Apache Pulsar Key_Shared Subscription](https://pulsar.apache.org/docs/concepts-messaging/#key_shared)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [Kafka Partition Ordering and Consumer Groups](https://kafka.apache.org/documentation/#intro_concepts_and_terms)
