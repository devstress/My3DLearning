# Scalability Strategy

## Overview

The Enterprise Integration Platform is designed for horizontal scalability at every layer. Each component can be scaled independently based on its specific throughput requirements. This document describes the scaling dimensions, strategies, and capacity planning guidance for each infrastructure component.

## Scaling Dimensions

The platform scales across four primary dimensions:

1. **Message throughput** — Number of messages processed per second.
2. **Concurrent workflows** — Number of active Temporal workflow executions.
3. **Storage volume** — Total data stored across message payloads, audit logs, and workflow state.
4. **Tenant count** — Number of active tenants with isolated processing pipelines.

## Component Scaling Strategies

### Message Broker Scaling

The platform uses two brokers, each scaled according to its workload:

#### Kafka Scaling (Streaming and Audit)

Kafka provides the scaling mechanism for broadcast event streams and audit.

**Partition Strategy:**
- Audit topics are partitioned by `TenantId` to ensure ordered processing within a tenant.
- The default partition count is 12 per topic, supporting up to 12 parallel consumers.
- Partition count can be increased without downtime (note: rebalancing occurs).

**Consumer Group Scaling:**
- Each service type forms a consumer group (e.g., `audit-writers`, `metrics-collectors`).
- Adding consumer instances within a group automatically rebalances partitions.
- Maximum effective parallelism equals the partition count.

#### Queue Broker Scaling (Task-Oriented Delivery)

The configurable queue broker (NATS JetStream or Apache Pulsar) provides per-recipient scaling with no Head-of-Line blocking.

**Design Principle: Recipient A must not block Recipient B, even at 1 million recipients.**

**NATS JetStream (default for local dev and cloud):**
- Per-subject queue groups provide independent consumption per recipient.
- Adding consumers to a queue group distributes messages automatically.
- Lightweight single binary — trivial to scale horizontally.
- Docker image `nats:latest` runs in Aspire for local development.

**Apache Pulsar Key_Shared (switchable for large-scale production):**
- Messages keyed by recipientId are distributed across consumers automatically.
- All messages for one recipient stay ordered and go to the same consumer.
- Different recipients are processed by different consumers independently.
- Adding consumers to a subscription redistributes keys — scaling is transparent.

Both brokers support millions of tenants without the prohibitive cost of Kafka topics.

**Throughput Targets:**
| Broker        | Workload          | Target Throughput    | Scaling Mechanism        |
|---------------|-------------------|----------------------|--------------------------|
| Kafka         | Audit events      | 10,000–50,000 msg/s  | Add partitions/consumers |
| NATS/Pulsar   | Ingestion         | 10,000–50,000 msg/s  | Add queue group/subscription consumers |
| NATS/Pulsar   | Delivery (per-recipient) | 10,000–100,000 msg/s | Per-subject/key independence |
| NATS/Pulsar   | DLQ               | 100–1,000 msg/s      | Low volume, minimal scaling |

### Temporal Worker Scaling

Temporal workers execute workflow and activity logic. They are stateless and can be scaled horizontally.

**Worker Pools:**
- Workflow workers and activity workers are separated into distinct pools.
- Each pool connects to a specific Temporal task queue.
- Worker count is scaled based on pending task queue depth.

**Scaling Guidelines:**
- Start with 2–4 workflow workers and 4–8 activity workers per task queue.
- Monitor `temporal_worker_task_slots_available` metric.
- Scale up when available slots consistently drop below 20% of capacity.
- Use Kubernetes Horizontal Pod Autoscaler (HPA) with custom metrics.

**Activity Concurrency:**
- Each activity worker processes activities concurrently (default: 100 concurrent slots).
- CPU-bound activities (transformations) should have lower concurrency limits.
- I/O-bound activities (HTTP calls, SFTP transfers) can sustain higher concurrency.

### Cassandra Horizontal Scaling

Cassandra scales linearly by adding nodes to the cluster.

**Scaling Approach:**
- Start with a 3-node cluster (replication factor 3) for production.
- Add nodes in increments of 3 to maintain balanced token distribution.
- Use `NetworkTopologyStrategy` for multi-datacenter deployments.

**Data Distribution:**
- Partition keys are designed to distribute data evenly across nodes.
- Message tables use `(TenantId, MessageDate)` compound partition keys to prevent hot partitions.
- Time-windowed partitions (daily or hourly) support efficient range queries and TTL-based cleanup.

**Capacity Planning:**
| Data Type        | Avg Record Size | Daily Volume   | Monthly Storage |
|------------------|-----------------|----------------|-----------------|
| Messages         | 2–10 KB         | 10M records    | ~1.5–7.5 TB    |
| Dedup Keys       | 100 bytes       | 10M records    | ~30 GB          |
| Audit Events     | 500 bytes       | 50M records    | ~750 GB         |
| Workflow State   | 1–5 KB          | 5M records     | ~375 GB–1.8 TB |

### Stateless Service Scaling

All platform services (Ingress API, Admin API, Worker Service) are stateless and scale horizontally:

- **Ingress API** — Scale based on inbound request rate. Use HPA targeting CPU utilization (70%) or request rate.
- **Admin API** — Scale based on admin traffic. Typically 2–4 replicas are sufficient.
- **Worker Service** — Scale based on Kafka consumer lag and Temporal task queue depth.

### Aspire Orchestration

.NET Aspire manages service composition for local development and can inform production deployment:

- **Local:** Aspire starts all services, message brokers (Kafka, NATS via Docker images), Temporal, and Cassandra containers in a coordinated graph.
- **Production:** Aspire's service discovery model maps to Kubernetes service discovery.
- **Scaling Config:** Aspire resource definitions specify replica counts and resource limits that translate to Kubernetes manifests.

## Scaling Architecture

```
                    Load Balancer
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
         ┌────────┐ ┌────────┐ ┌────────┐
         │Ingress │ │Ingress │ │Ingress │   ← Scale by request rate
         │ API #1 │ │ API #2 │ │ API #N │
         └───┬────┘ └───┬────┘ └───┬────┘
             │          │          │
             ▼          ▼          ▼
    ┌──────────────────────────────────────┐
    │   Message Broker Layer               │
    │   Kafka (streams/audit)              │   ← Scale by partition count
    │   Redis Streams (task delivery)      │   ← Scale by consumer group
    └──────────────────────────────────────┘
             │          │          │
              ▼          ▼          ▼
         ┌────────┐ ┌────────┐ ┌────────┐
         │Worker  │ │Worker  │ │Worker  │   ← Scale by queue depth
         │  #1    │ │  #2    │ │  #N    │
         └───┬────┘ └───┬────┘ └───┬────┘
             │          │          │
             ▼          ▼          ▼
    ┌──────────────────────────────────────┐
    │     Cassandra Cluster (3+ nodes)     │   ← Scale by adding nodes
    └──────────────────────────────────────┘
```

## Capacity Planning Guidance

### Estimating Resource Requirements

1. **Determine peak message rate** — Messages per second at peak load.
2. **Calculate Kafka partition count** — Peak rate / per-partition throughput (typically 5,000–10,000 msg/s per partition).
3. **Size Temporal workers** — Peak concurrent workflows / workflows per worker (typically 100–500).
4. **Size Cassandra cluster** — Total data volume / per-node capacity (typically 1–2 TB per node).
5. **Add headroom** — Plan for 2× peak capacity to handle burst traffic and growth.

### Monitoring for Scale Decisions

| Metric                              | Threshold          | Action                        |
|-------------------------------------|--------------------|-------------------------------|
| Kafka consumer lag                  | > 10,000 messages  | Add consumer instances        |
| NATS/Pulsar pending count           | > 10,000 messages  | Add queue group/subscription consumers |
| Temporal task queue backlog         | > 1,000 pending    | Add Temporal workers          |
| Cassandra disk utilization          | > 60%              | Add Cassandra nodes           |
| Ingress API response latency (p99)  | > 500ms            | Add Ingress API replicas      |
| CPU utilization (any service)       | > 70% sustained    | Scale horizontally            |

## Anti-Patterns to Avoid

- **Single partition topics** — Eliminate parallelism; always use multiple partitions.
- **Fat envelopes** — Large payloads in Kafka degrade throughput; use claim check for payloads > 256 KB.
- **Unbounded workflow histories** — Long-running workflows with thousands of events; use continue-as-new.
- **Hot partitions in Cassandra** — Avoid partition keys that concentrate writes on a single node.
- **Synchronous inter-service calls** — Defeats the purpose of the asynchronous broker architecture; always go through the configured message broker.
