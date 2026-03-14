# Message Broker Topology

## Overview

The Enterprise Integration Platform uses a configurable message broker layer. This document describes the topic and queue topology for all supported brokers — Apache Kafka for broadcast event streams and audit, and NATS JetStream (default) or Apache Pulsar with Key_Shared (large-scale production) for task-oriented delivery where recipient A must not block recipient B, even at 1 million recipients.

The broker choice is a deployment-time configuration switch per message flow category. Kafka and the queue broker run simultaneously, each handling the workload category it is best suited for.

## Why Multiple Brokers

Kafka is a strong backbone for high-throughput event streaming, but it is not a universal middleware replacement. Kafka is partitioned and ordered per partition; within a consumer group each partition is consumed by exactly one consumer at a time. This gives strong scalability but creates per-partition serialization — a slow or poison message blocks progress behind it on that partition (Head-of-Line blocking). Per-tenant topics at scale (e.g., 1 million tenants) are prohibitively expensive.

**NATS JetStream** (default for local dev and cloud) is a lightweight, cloud-native single binary with per-subject filtering and queue groups. NATS avoids HOL blocking between subjects, has very low operational overhead, and runs as a Docker container in Aspire (`nats:latest`). Ideal for local testing and cloud deployments.

**Apache Pulsar with Key_Shared** (switchable for large-scale production) distributes messages by key (e.g., recipientId) across consumers within a single subscription. All messages for one recipient stay ordered, while different recipients are processed independently. Pulsar provides built-in multi-tenancy, tiered storage, and geo-replication for large-scale on-prem deployments.

**Recipient A must not block Recipient B, even at 1 million recipients.** Both NATS and Pulsar satisfy this requirement.

| Concern                    | Kafka                              | NATS JetStream                      | Pulsar (Key_Shared)                 |
|----------------------------|------------------------------------|-------------------------------------|-------------------------------------|
| **Best for**               | Broadcast streams, audit, analytics| Local dev, cloud, task delivery     | Large-scale production delivery     |
| **HOL blocking risk**      | Per-partition serialization         | Per-subject independence            | Per-key independence                |
| **Per-tenant cost at scale** | High (topics expensive)           | Low (subjects are lightweight)      | Low (built-in multi-tenancy)        |
| **Ordering guarantee**     | Per-partition                       | Per-subject                         | Per-key (recipientId)               |
| **Message acknowledgment** | Offset-based (batch)               | Per-message (AckPolicy)             | Per-message                         |
| **Replay capability**      | Yes (offset reset)                  | Yes (consumer replay)               | Yes (cursor reset, tiered storage)  |
| **Setup complexity**       | Medium                              | Low (single binary, Docker)         | High (ZooKeeper + BookKeeper)       |
| **Aspire local dev**       | Docker container                    | Docker container (`nats:latest`)    | Heavy multi-container setup         |

## Kafka Topology (Streaming and Audit)

## Topic Naming Convention

Topics follow a hierarchical naming convention:

```
eip.{tenant-id}.{domain}.{event-type}
```

**Examples:**
- `eip.acme.audit.events` — Audit events for tenant Acme
- `eip.platform.audit.events` — Platform-wide audit events
- `eip.platform.audit.security` — Security audit events

## Kafka Topic Categories

Kafka is scoped to broadcast event streams and audit — workloads where its partitioned, ordered, high-throughput model excels. Task-oriented delivery (ingestion, routing, delivery, DLQ) uses the configurable queue broker (see NATS JetStream and Apache Pulsar sections below).

### Audit Topics

Platform-wide event recording for compliance and observability.

| Topic Pattern                          | Partitions | Retention | Purpose                              |
|----------------------------------------|------------|-----------|--------------------------------------|
| `eip.platform.audit.events`           | 12         | 90 days   | All processing audit events          |
| `eip.platform.audit.security`         | 6          | 365 days  | Security-related audit events        |

## Kafka Partitioning Strategy

### Partition Key Selection

| Topic Category | Partition Key        | Rationale                                         |
|----------------|----------------------|---------------------------------------------------|
| Audit          | `tenant_id`          | Tenant-level ordering for compliance queries      |
| Analytics      | `correlation_id`     | Related events go to same partition for ordering  |

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

## Kafka Consumer Group Design

### Consumer Groups

| Consumer Group                        | Consumes From                         | Purpose                                  |
|---------------------------------------|---------------------------------------|------------------------------------------|
| `eip-audit-writers`                   | `eip.platform.audit.events`           | Write audit events to Cassandra          |
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
| Audit           | 90 days        | 100 GB         | delete         |
| Security Audit  | 365 days       | 50 GB          | delete         |

### Compaction

Compacted topics are not used for message flow. If future requirements include entity state (e.g., connector configuration), compacted topics may be introduced with `cleanup.policy=compact`.

## Topic Creation

### Automated Topic Creation

Kafka audit and analytics topics are created automatically by the platform's provisioning service:

```
Platform startup → Create audit topics → Create analytics topics
```

### Topic Configuration Defaults

```properties
# Applied to all Kafka topics (audit and analytics only)
min.insync.replicas=2
replication.factor=3
compression.type=lz4

# Audit topics
num.partitions=12
retention.ms=7776000000       # 90 days
max.message.bytes=10485760    # 10 MB

# Security audit topics
num.partitions=6
retention.ms=31536000000      # 365 days
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

---

## NATS JetStream Topology (Default — Local Dev and Cloud)

### Design Principle

**Recipient A must not block Recipient B, even at 1 million recipients.** NATS JetStream uses per-subject filtering with queue groups. Each recipient's messages flow through an independent subject — a slow or failing recipient never blocks any other recipient.

### Setup

NATS runs as a single Docker container in Aspire:

```
docker image: nats:latest
port: 4222 (client), 8222 (monitoring)
JetStream enabled with --jetstream flag
```

### Subject Naming Convention

```
eip.{tenant}.{domain}.{identifier}
```

**Examples:**
- `eip.acme.ingestion.messages` — Inbound messages for tenant Acme
- `eip.acme.delivery.http-erp` — Delivery to the HTTP ERP connector
- `eip.acme.delivery.sftp-partner` — Delivery to the SFTP partner connector
- `eip.acme.dlq.delivery` — Dead letter for failed deliveries

### Queue Groups

NATS queue groups provide competing-consumer semantics — multiple consumers in the same queue group share the workload, and each message is delivered to exactly one consumer in the group:

```
Subject: eip.acme.delivery.http-erp
  └── Queue Group: delivery-workers
        ├── Worker 1 (receives some messages)
        ├── Worker 2 (receives some messages)
        └── Worker 3 (receives some messages)
```

Each subject/queue-group pair operates independently. Slow consumption on `eip.acme.delivery.http-erp` does not affect `eip.acme.delivery.sftp-partner`.

### JetStream Streams

JetStream streams provide persistence and replay:

| Stream Name               | Subjects                              | Retention   | Purpose                    |
|---------------------------|---------------------------------------|-------------|----------------------------|
| `EIP_INGESTION_{tenant}`  | `eip.{tenant}.ingestion.>`            | Limits-based| Inbound message persistence|
| `EIP_DELIVERY_{tenant}`   | `eip.{tenant}.delivery.>`             | Limits-based| Delivery persistence       |
| `EIP_DLQ_{tenant}`        | `eip.{tenant}.dlq.>`                  | Limits-based| DLQ persistence            |

### JetStream Configuration

```
max_msgs: 1,000,000
max_bytes: 1GB
max_age: 7 days (ingestion), 3 days (delivery), 30 days (DLQ)
storage: file
replicas: 1 (dev), 3 (production)
```

### NATS Monitoring

| Metric                                  | Alert Threshold           | Significance                          |
|-----------------------------------------|---------------------------|---------------------------------------|
| Consumer pending count                  | > 10,000 messages         | Consumer falling behind               |
| Consumer ack pending age                | > 5 minutes               | Stuck message, investigate consumer   |
| Stream message count                    | > 80% of max_msgs         | Approaching retention limit           |
| Server connections                      | > 80% of max              | Capacity planning needed              |

---

## Apache Pulsar Topology (Switchable — Large-Scale Production)

### Design Principle

**Recipient A must not block Recipient B, even at 1 million recipients.** Pulsar's Key_Shared subscription type distributes messages by key (e.g., recipientId) across consumers — all messages for one recipient stay ordered and go to the same consumer, while different recipients are processed independently by other consumers.

### Key_Shared Subscription Model

```
Topic: persistent://eip/acme/delivery
  └── Subscription: delivery-workers (Key_Shared)
        ├── Consumer 1 ← messages where key hash → consumer 1
        │     (e.g., recipient-A, recipient-D, recipient-G)
        ├── Consumer 2 ← messages where key hash → consumer 2
        │     (e.g., recipient-B, recipient-E, recipient-H)
        └── Consumer 3 ← messages where key hash → consumer 3
              (e.g., recipient-C, recipient-F, recipient-I)
```

If Consumer 1 is slow processing recipient-A messages, Consumers 2 and 3 continue processing their assigned recipients at full speed. No Head-of-Line blocking between recipients.

### Topic Naming Convention

Pulsar topics follow a hierarchical naming convention with built-in multi-tenancy:

```
persistent://{pulsar-tenant}/{namespace}/{topic}
```

**Examples:**
- `persistent://eip/acme/ingestion` — Inbound messages for tenant Acme
- `persistent://eip/acme/delivery` — Delivery messages keyed by recipientId
- `persistent://eip/acme/dlq` — Dead letter topic for failed deliveries

### Topic Categories

#### Ingestion Topics

| Topic Pattern                              | Subscription Type | Purpose                              |
|--------------------------------------------|-------------------|--------------------------------------|
| `persistent://eip/{tenant}/ingestion`      | Key_Shared        | Inbound messages, keyed by envelopeId|

#### Delivery Topics (Key_Shared by recipientId)

The key design: a single delivery topic per tenant with Key_Shared subscription keyed by recipientId. Pulsar automatically distributes messages across consumers by key — each recipient's messages are ordered and processed by the same consumer, while different recipients are processed independently.

| Topic Pattern                              | Subscription Type | Key            | Purpose                      |
|--------------------------------------------|-------------------|----------------|------------------------------|
| `persistent://eip/{tenant}/delivery`       | Key_Shared        | recipientId    | Delivery to all recipients   |

At 1 million recipients, this uses a single topic with Key_Shared — Pulsar handles the key distribution efficiently. No need for 1 million separate topics.

#### DLQ Topics

| Topic Pattern                              | Subscription Type | Purpose                              |
|--------------------------------------------|-------------------|--------------------------------------|
| `persistent://eip/{tenant}/dlq`            | Shared            | Failed messages for review/replay    |

### Subscription Types Reference

| Subscription Type | Behavior                                                  | Use Case                    |
|-------------------|-----------------------------------------------------------|-----------------------------|
| **Key_Shared**    | Messages with same key → same consumer; different keys distributed | Delivery by recipientId     |
| **Shared**        | Round-robin across consumers, no ordering                 | DLQ processing              |
| **Exclusive**     | Single consumer owns the subscription                     | Admin/control topics        |
| **Failover**      | Active-standby with automatic failover                    | High-availability consumers |

### Reliable Delivery

Pulsar tracks message acknowledgment per message (not offset-based like Kafka):

```
1. Consumer receives message via Key_Shared subscription
2. Consumer processes message successfully → acknowledge
3. If consumer crashes → message redelivered to another consumer with same key assignment
4. Negative acknowledge (nack) → message redelivered after configurable delay
5. Acknowledgment timeout → message redelivered automatically
```

### HOL Blocking Avoidance

With Kafka, a stuck message on partition N blocks all messages behind it on that partition. With Pulsar Key_Shared:

- Messages are distributed by key (recipientId) across consumers
- Recipient A's messages go to one consumer; recipient B's messages go to another
- If recipient A's consumer is slow or stuck, recipient B's consumer continues at full speed
- Per-message acknowledgment means a stuck message for recipient A does not block other messages for recipient A either (nack + redeliver)
- At 1 million recipients, keys are distributed across the consumer pool — no shared partition bottleneck

### Automated Topic Creation

Topics are created automatically when a new tenant is onboarded:

```
Tenant onboarding → Create ingestion topic
                  → Create delivery topic (Key_Shared by recipientId)
                  → Create DLQ topic
```

### Pulsar Retention and Tiered Storage

| Topic Category | Retention        | Tiered Storage | Rationale                    |
|----------------|------------------|----------------|------------------------------|
| Ingestion      | 7 days           | Warm after 1d  | Short-lived, high volume     |
| Delivery       | 3 days           | Warm after 1d  | Per-recipient, lower volume  |
| DLQ            | 30 days          | Cold after 7d  | Retained for review          |

Pulsar's tiered storage offloads older data to object storage (S3, GCS, Azure Blob) automatically, reducing broker storage costs.

### Pulsar Monitoring

| Metric                                  | Alert Threshold           | Significance                          |
|-----------------------------------------|---------------------------|---------------------------------------|
| Message backlog per subscription        | > 10,000 messages         | Consumer falling behind               |
| Oldest unacked message age              | > 5 minutes               | Stuck message, investigate consumer   |
| Publish rate vs. dispatch rate          | Diverging for 5 min       | Processing not keeping up             |
| Broker CPU/memory                       | > 70%                     | Capacity planning needed              |
| BookKeeper disk usage                   | > 70%                     | Add BookKeeper nodes                  |
