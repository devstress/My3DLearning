# Kafka Topic Topology

## Overview

Apache Kafka serves as the event backbone of the Enterprise Integration Platform. This document describes the topic structure, partitioning strategy, consumer group design, and retention policies that govern message flow through the platform.

## Topic Naming Convention

Topics follow a hierarchical naming convention:

```
eip.{tenant-id}.{domain}.{event-type}
```

**Examples:**
- `eip.acme.ingestion.messages` — Inbound messages for tenant Acme
- `eip.acme.routing.orders` — Routed order messages
- `eip.acme.ingestion.messages.dlq` — Dead letter queue for failed ingestion messages
- `eip.platform.audit.events` — Platform-wide audit events

## Topic Categories

### Ingestion Topics

Receive normalized `IntegrationEnvelope` messages from ingress adapters.

| Topic Pattern                          | Partitions | Retention | Purpose                              |
|----------------------------------------|------------|-----------|--------------------------------------|
| `eip.{tenant}.ingestion.messages`      | 12         | 7 days    | All inbound messages for a tenant    |
| `eip.{tenant}.ingestion.messages.dlq`  | 3          | 30 days   | Failed ingestion messages            |

### Routing Topics

Carry messages to specific processing pipelines based on content-based routing decisions.

| Topic Pattern                          | Partitions | Retention | Purpose                              |
|----------------------------------------|------------|-----------|--------------------------------------|
| `eip.{tenant}.routing.{type}`          | 6          | 3 days    | Routed messages by type              |
| `eip.{tenant}.routing.{type}.dlq`     | 3          | 30 days   | Failed routing messages              |

### Processing Topics

Internal topics for inter-activity communication when workflows produce intermediate results.

| Topic Pattern                          | Partitions | Retention | Purpose                              |
|----------------------------------------|------------|-----------|--------------------------------------|
| `eip.{tenant}.processing.transformed`  | 6          | 3 days    | Post-transformation messages         |
| `eip.{tenant}.processing.enriched`     | 6          | 3 days    | Post-enrichment messages             |

### Delivery Topics

Carry messages ready for outbound delivery via connectors.

| Topic Pattern                          | Partitions | Retention | Purpose                              |
|----------------------------------------|------------|-----------|--------------------------------------|
| `eip.{tenant}.delivery.{connector}`    | 6          | 3 days    | Messages for specific connectors     |
| `eip.{tenant}.delivery.{connector}.dlq`| 3         | 30 days   | Failed delivery messages             |

### Audit Topics

Platform-wide event recording for compliance and observability.

| Topic Pattern                          | Partitions | Retention | Purpose                              |
|----------------------------------------|------------|-----------|--------------------------------------|
| `eip.platform.audit.events`           | 12         | 90 days   | All processing audit events          |
| `eip.platform.audit.security`         | 6          | 365 days  | Security-related audit events        |

## Partitioning Strategy

### Partition Key Selection

| Topic Category | Partition Key        | Rationale                                         |
|----------------|----------------------|---------------------------------------------------|
| Ingestion      | `envelope_id`        | Even distribution across partitions               |
| Routing        | `correlation_id`     | Related messages go to same partition for ordering |
| Delivery       | `connector_id`       | Locality for connector-specific consumers         |
| Audit          | `tenant_id`          | Tenant-level ordering for compliance queries      |

### Partition Count Guidelines

- **High-throughput topics (ingestion):** 12–24 partitions to support parallel consumers.
- **Medium-throughput topics (routing, delivery):** 6–12 partitions.
- **Low-throughput topics (DLQ, audit):** 3–6 partitions.
- **Rule of thumb:** Partition count ≥ maximum expected consumer instances for the topic.

### Ordering Guarantees

Kafka guarantees ordering within a partition. The platform ensures ordered processing where required:

- Messages with the same `correlation_id` are routed to the same partition, preserving order for related messages.
- Unrelated messages may be processed out of order across partitions (intentional for parallelism).
- When strict global ordering is required (rare), use a single-partition topic.

## Consumer Group Design

### Consumer Groups

| Consumer Group                        | Consumes From                         | Purpose                                  |
|---------------------------------------|---------------------------------------|------------------------------------------|
| `eip-workflow-starters`               | `eip.*.ingestion.messages`            | Start Temporal workflows for new messages|
| `eip-audit-writers`                   | `eip.platform.audit.events`           | Write audit events to Cassandra          |
| `eip-dlq-monitor`                     | `eip.*.*.dlq`                         | Monitor DLQ topics for alerting          |
| `eip-metrics-collector`              | `eip.platform.audit.events`           | Calculate metrics from audit events      |

### Consumer Configuration

```json
{
  "groupId": "eip-workflow-starters",
  "autoOffsetReset": "earliest",
  "enableAutoCommit": false,
  "maxPollRecords": 100,
  "maxPollIntervalMs": 300000,
  "sessionTimeoutMs": 30000,
  "heartbeatIntervalMs": 10000,
  "isolationLevel": "read_committed"
}
```

### Offset Management

- **Manual commit:** Offsets are committed only after successful processing (at-least-once delivery).
- **Commit strategy:** Commit after each batch of messages is processed and workflows are started.
- **Reset policy:** `earliest` — on consumer group creation or offset expiration, process from the beginning.

## Retention Policies

### Retention Configuration

| Topic Category  | Time Retention | Size Retention | Cleanup Policy |
|-----------------|----------------|----------------|----------------|
| Ingestion       | 7 days         | 50 GB          | delete         |
| Routing         | 3 days         | 20 GB          | delete         |
| Delivery        | 3 days         | 20 GB          | delete         |
| DLQ             | 30 days        | 10 GB          | delete         |
| Audit           | 90 days        | 100 GB         | delete         |
| Security Audit  | 365 days       | 50 GB          | delete         |

### Compaction

Compacted topics are not used for message flow. If future requirements include entity state (e.g., connector configuration), compacted topics may be introduced with `cleanup.policy=compact`.

## Topic Creation

### Automated Topic Creation

Topics are created automatically by the platform's topic provisioning service when a new tenant is onboarded:

```
Tenant onboarding → Create ingestion topic → Create routing topics
                  → Create delivery topics → Create DLQ topics
```

### Topic Configuration Defaults

```properties
# Applied to all topics
min.insync.replicas=2
replication.factor=3
compression.type=lz4

# Ingestion topics
num.partitions=12
retention.ms=604800000        # 7 days
max.message.bytes=10485760    # 10 MB

# DLQ topics
num.partitions=3
retention.ms=2592000000       # 30 days
max.message.bytes=10485760    # 10 MB
```

## Monitoring

### Key Kafka Metrics

| Metric                                  | Alert Threshold           | Significance                          |
|-----------------------------------------|---------------------------|---------------------------------------|
| Consumer group lag                      | > 10,000 messages         | Processing falling behind             |
| Under-replicated partitions             | > 0 for 5 minutes        | Data durability at risk               |
| ISR shrink rate                         | > 0 for 5 minutes        | Broker health issue                   |
| Request latency (p99)                   | > 100ms                  | Kafka cluster under pressure          |
| Log directory disk usage                | > 80%                    | Capacity planning needed              |
| Active controller count                 | != 1                     | Cluster leadership issue              |

### Consumer Lag Monitoring

Consumer lag is the primary indicator of processing health:

```
Healthy:   Lag < 100 messages (real-time processing)
Warning:   Lag 100–10,000 (slight delay, monitor)
Critical:  Lag > 10,000 (processing significantly delayed, investigate)
Emergency: Lag growing continuously (consumer may be stuck, immediate action)
```

## Disaster Recovery

### Backup Strategy

- **Topic data:** Kafka MirrorMaker 2 replicates topics to a standby cluster.
- **Consumer offsets:** Replicated with MirrorMaker 2 offset sync.
- **Topic configuration:** Stored in version control (Infrastructure as Code).

### Recovery Procedure

1. Verify standby cluster health and replication status.
2. Update DNS/service discovery to point consumers to standby cluster.
3. Consumers resume from replicated offsets (minimal message reprocessing).
4. Monitor for duplicate processing; idempotent activities handle safely.
