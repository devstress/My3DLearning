# Reliability Patterns

## Overview

The Enterprise Integration Platform is designed for enterprise-grade reliability. Every message accepted by the platform is guaranteed to be processed to completion or explicitly routed to a dead letter queue for human review. This document describes the reliability patterns, guarantees, and failure modes.

## Delivery Guarantees

### At-Least-Once Delivery

The platform guarantees at-least-once delivery at every boundary:

1. **Ingress → Kafka:** Messages are published to Kafka with `acks=all` and synchronous confirmation. The ingress API returns 202 Accepted only after Kafka acknowledges the write.
2. **Kafka → Temporal:** Consumer offsets are committed only after the Temporal workflow is successfully started.
3. **Temporal → Activities:** Temporal guarantees activity execution. Failed activities are retried according to the configured retry policy.
4. **Activities → Connectors:** Connector delivery is confirmed before the activity reports success. Failed deliveries trigger retries or DLQ routing.

### Idempotent Processing

Since at-least-once delivery can produce duplicate messages, every processing step is designed to be idempotent:

- **Deduplication table** — Cassandra stores processed `MessageId` values with a configurable TTL (default: 7 days). Before processing, activities check for existing entries.
- **Upsert semantics** — Data writes use Cassandra's native upsert behavior, making repeated writes safe.
- **Idempotency keys** — Outbound connector calls include idempotency keys (derived from `EnvelopeId`) where the target system supports them.

### Message Deduplication Flow

```
Message arrives → Check dedup table (Cassandra)
                      │
                 ┌────┴────┐
                 │         │
              Exists    Not Found
                 │         │
                 ▼         ▼
              Skip      Process message
           (log dup)    Insert dedup key
                        Execute workflow
```

## Temporal Durability Guarantees

Temporal.io provides exceptional durability for workflow orchestration:

- **Workflow state persistence** — Every workflow state transition is durably persisted in Temporal's backend store before proceeding.
- **Automatic recovery** — If a worker crashes mid-activity, Temporal detects the timeout and reassigns the activity to another worker.
- **Event sourcing** — Workflow history is an immutable event log, enabling exact replay and debugging.
- **No single point of failure** — Temporal server runs as a multi-node cluster with leader election.

### Retry Policies

Default retry configuration for activities:

```json
{
  "maxAttempts": 5,
  "initialIntervalMs": 1000,
  "backoffCoefficient": 2.0,
  "maxIntervalMs": 60000,
  "nonRetryableErrors": [
    "ValidationException",
    "SchemaNotFoundException",
    "AuthenticationException"
  ]
}
```

- **Transient errors** (network timeouts, temporary unavailability) are retried automatically.
- **Permanent errors** (validation failures, authentication errors) are not retried; the message is routed to DLQ immediately.

## Cassandra Replication

Cassandra ensures data durability and availability:

- **Replication factor:** 3 (each piece of data is stored on 3 nodes).
- **Consistency level (writes):** `LOCAL_QUORUM` — Write succeeds when 2 of 3 replicas acknowledge.
- **Consistency level (reads):** `LOCAL_QUORUM` — Read returns data confirmed by 2 of 3 replicas.
- **Hinted handoff:** If a replica is temporarily down, hints are stored and replayed when it recovers.
- **Repair:** Weekly anti-entropy repair jobs ensure all replicas converge.

## SLA Targets

| Metric                         | Target       | Measurement                              |
|--------------------------------|--------------|------------------------------------------|
| Message processing availability| 99.95%       | Percentage of time ingress accepts msgs  |
| End-to-end latency (p50)       | < 2 seconds  | Ingress to connector delivery            |
| End-to-end latency (p99)       | < 30 seconds | Ingress to connector delivery            |
| Message loss rate              | 0%           | No accepted messages lost                |
| DLQ processing time            | < 4 hours    | Time to review and resolve DLQ messages  |
| Recovery time objective (RTO)  | < 15 minutes | Time to recover from infrastructure failure |
| Recovery point objective (RPO) | 0 messages   | No data loss on recovery                 |

## Failure Modes and Mitigations

### Kafka Broker Failure

**Impact:** Temporary inability to publish or consume messages.

**Mitigation:**
- Kafka runs as a multi-broker cluster (minimum 3 brokers).
- Replication factor of 3 ensures data survives broker failures.
- Producers retry with exponential backoff on send failures.
- Consumers resume from last committed offset on reconnection.

### Temporal Server Failure

**Impact:** Workflow execution pauses until Temporal recovers.

**Mitigation:**
- Temporal runs as a multi-node cluster with automatic leader election.
- Workflow state is persisted independently of Temporal workers.
- Workers automatically reconnect and resume pending activities.
- No workflow state is lost; execution continues from the last persisted state.

### Cassandra Node Failure

**Impact:** Temporary increased latency; no data loss with RF=3.

**Mitigation:**
- LOCAL_QUORUM consistency tolerates 1 node failure per datacenter.
- Hinted handoff ensures missed writes are replayed on recovery.
- Monitoring alerts trigger node replacement within SLA.

### Worker Process Crash

**Impact:** In-flight activities are interrupted.

**Mitigation:**
- Temporal detects activity timeout and reassigns to healthy workers.
- Kafka consumer rebalancing assigns orphaned partitions to remaining consumers.
- No manual intervention required for recovery.

### Network Partition

**Impact:** Components may be temporarily unable to communicate.

**Mitigation:**
- Kafka and Cassandra are partition-tolerant (AP systems with tunable consistency).
- Temporal handles network issues through activity heartbeats and timeouts.
- Circuit breakers prevent cascading failures during partitions.

## Data Integrity

### Envelope Immutability

Once created, an `IntegrationEnvelope` is never modified. Processing steps create new envelopes referencing the original via `CausationId`. This ensures a complete, auditable processing history.

### Audit Trail

Every state transition is recorded:
- Envelope creation (ingress timestamp, source, protocol)
- Workflow start and completion
- Activity execution results
- Connector delivery confirmations
- DLQ routing with error details

Audit records are stored in Cassandra with a retention period of 90 days (configurable per tenant).

## Testing Reliability

### Chaos Engineering

The platform supports chaos testing scenarios:
- **Kill random workers** — Verify Temporal reassigns activities.
- **Partition Kafka brokers** — Verify producers retry and consumers resume.
- **Stop Cassandra nodes** — Verify quorum reads/writes continue.
- **Inject network latency** — Verify timeouts and circuit breakers activate.

### Load Testing

Regular load tests validate:
- Sustained throughput at 2× expected peak
- Graceful degradation under 5× peak (backpressure, no crashes)
- Recovery time after infrastructure component restart
