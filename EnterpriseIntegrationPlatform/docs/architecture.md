# Architecture Document

## Overview

The Enterprise Integration Platform (EIP) is a cloud-native integration system designed to replace legacy middleware such as BizTalk Server. It provides reliable, scalable message processing with workflow orchestration, multi-protocol connectivity, and AI-assisted development capabilities.

The platform follows a layered, loosely coupled architecture where each layer communicates through well-defined contracts. Messages flow through an immutable pipeline from ingestion to delivery, with full observability at every stage. The message broker layer is configurable — Kafka handles broadcast event streams and audit logs while a configurable queue broker (default: NATS JetStream; Apache Pulsar with Key_Shared for large-scale production) handles task-oriented delivery where recipient A must not block recipient B, even at 1 million recipients.

## Design Principles

1. **Right Tool for Each Job** — The platform uses Kafka for broadcast event streams, audit logs, fan-out analytics, and decoupled integration where its partitioned, ordered, high-throughput model excels. A configurable queue broker (e.g., RabbitMQ) handles task-oriented message delivery where independent per-recipient queues avoid Head-of-Line (HOL) blocking — critical for saga patterns where processing of recipient B must continue even when recipient A is down. Kafka is partitioned and ordered per partition; within a consumer group each partition is consumed by exactly one consumer at a time, creating per-partition serialization that can block progress when one slow or poison message stalls a partition. The broker choice is a deployment-time configuration switch per message flow category.
2. **Durability Over Speed** — Every message is persisted before acknowledgment. At-least-once delivery with idempotent processing guarantees no data loss.
3. **Separation of Concerns** — Ingestion, routing, transformation, delivery, and orchestration are independent, deployable units.
4. **Observable by Default** — OpenTelemetry is integrated at every layer. Every message carries a correlation ID for end-to-end tracing.
5. **AI-Augmented Development** — Ollama provides local AI capabilities for code generation, workflow scaffolding, and documentation summarization.
6. **Multi-Tenant Ready** — Tenant isolation is enforced at the data and processing layers through partition strategies and namespace separation.
7. **Cloud-Native Orchestration** — .NET Aspire manages service composition, health checks, and local development orchestration.

## Technology Stack

| Layer              | Technology         | Purpose                                      |
|--------------------|--------------------|----------------------------------------------|
| Runtime            | .NET 10            | High-performance, cross-platform runtime     |
| Orchestration      | .NET Aspire        | Service composition and local orchestration  |
| Event Streaming     | Apache Kafka       | Broadcast streams, audit logs, fan-out analytics |
| Queue Broker        | NATS JetStream (default)     | Lightweight task-oriented delivery, per-subject queue groups, no HOL blocking |
| Queue Broker (prod) | Apache Pulsar (switchable)   | Key_Shared subscription for large-scale production                            |
| Workflow Engine    | Temporal.io        | Durable workflow orchestration               |
| Primary Storage    | Apache Cassandra   | Distributed, highly-available data store     |
| Observability      | OpenTelemetry      | Distributed tracing, metrics, and logging    |
| AI Runtime         | Ollama (configurable) | Local LLM inference for code generation; switchable to other AI providers |
| API Gateway        | ASP.NET Core       | RESTful admin and ingestion APIs             |

## Layered Architecture

The platform is organized into seven distinct layers, each with clear responsibilities:

### Layer 1: Ingress

The ingress layer accepts messages from external systems through multiple protocols:

- **HTTP/REST endpoints** — Receive webhooks, API calls, and file uploads.
- **SFTP watchers** — Poll remote SFTP servers for new files on configurable schedules.
- **Email listeners** — Monitor mailboxes for inbound messages and attachments.
- **File system watchers** — Detect new files in local or network-mounted directories.

Each ingress adapter normalizes incoming data into an `IntegrationEnvelope` — the canonical message format — and publishes it to the appropriate Kafka ingestion topic.

### Layer 2: Configurable Message Broker

The message broker layer connects ingress to processing and delivery. The broker is configurable per message flow category:

**Kafka (broadcast streams, audit, analytics):**

- **Audit topics** — Record all processing events for compliance and debugging.
- **Analytics streams** — Fan-out event streams for downstream analytics consumers.
- **Decoupled integration** — Publish/subscribe patterns where multiple consumers process the same stream independently.

Kafka is a strong backbone for high-throughput event streaming. It is partitioned and ordered per partition; within a consumer group each partition is consumed by exactly one consumer at a time. This gives strong scalability but creates per-partition serialization — a slow or poison message blocks progress behind it on that partition. Per-tenant topics at scale (e.g., 1 million tenants) are prohibitively expensive.

**Queue broker — default: NATS JetStream; Apache Pulsar with Key_Shared for large-scale production (task-oriented delivery):**

- **Ingestion subjects/topics** — Receive normalized envelopes from ingress adapters.
- **Delivery subjects/topics** — Carry messages to specific processing pipelines and connectors.
- **DLQ subjects/topics** — Capture failed messages for manual review or automated retry.

**NATS JetStream** (default for local development and cloud) is a lightweight, cloud-native single binary with per-subject filtering and queue groups. NATS avoids HOL blocking between subjects, has very low operational overhead, and runs as a Docker container in Aspire (`nats:latest`). Messages keyed by recipientId use subject-based routing — each recipient's subject is consumed independently. **Recipient A must not block Recipient B, even at 1 million recipients.**

**Apache Pulsar with Key_Shared** (switchable for large-scale production) distributes messages by key (e.g., recipientId) across consumers within a single subscription. All messages for recipient A stay ordered and go to the same consumer, while recipient B is processed by another consumer independently. Pulsar's built-in multi-tenancy, tiered storage (hot/warm/cold), and geo-replication make it suitable for large-scale on-prem production deployments.

For cloud deployments, **NATS JetStream** is also the recommended option — available as a managed service (Synadia Cloud) with minimal operational overhead. The broker choice is a deployment-time configuration switch.

Partitioning is based on tenant ID and message type, ensuring ordered processing within a tenant while enabling horizontal scalability across tenants.

### Layer 3: Temporal Workflow Orchestration

Temporal.io provides durable, long-running workflow execution:

- **Integration workflows** — Orchestrate the full lifecycle of a message: validation, transformation, routing, delivery, and acknowledgment.
- **Saga patterns** — Coordinate multi-step processes with compensation logic for rollback.
- **Retry policies** — Configurable per-activity retry with exponential backoff and maximum attempts.
- **Versioning** — Workflow definitions support deterministic versioning for safe deployments.

### Layer 4: Activity Processing

Activities are the units of work executed within Temporal workflows:

- **Validation activities** — Schema validation, business rule evaluation, and data quality checks.
- **Transformation activities** — Message mapping, format conversion, enrichment, and splitting/aggregation.
- **Routing activities** — Content-based routing, recipient list resolution, and dynamic endpoint selection.

### Layer 5: Connectors

Connectors provide outbound delivery to target systems:

- **HTTP connector** — REST API calls with configurable authentication, headers, and retry policies.
- **SFTP connector** — File delivery to remote SFTP servers with resume and verification.
- **Email connector** — SMTP delivery with template support and attachment handling.
- **File connector** — Local or network file system writes with atomic rename patterns.

### Layer 6: Storage

Apache Cassandra provides the distributed data layer:

- **Message store** — Persistent storage for message payloads and metadata.
- **Workflow state** — Queryable workflow execution history and current state.
- **Deduplication store** — Idempotency keys for at-least-once processing guarantees.
- **Audit log** — Immutable record of all processing events and state transitions.

### Layer 7: Observability

OpenTelemetry provides comprehensive observability:

- **Distributed tracing** — End-to-end trace propagation across HTTP, Kafka, and Temporal boundaries.
- **Structured logging** — Contextual, machine-parseable logs correlated with trace and span IDs.
- **Metrics** — Throughput, latency, error rates, and queue depth metrics per tenant and message type.
- **Health checks** — Liveness and readiness probes for all services and dependencies.

## Deployment Architecture

The platform is designed for containerized deployment:

- **Local development** — .NET Aspire orchestrates all services, including the configured message brokers (Kafka and NATS via Docker images), Temporal, and Cassandra containers.
- **Staging/Production** — Kubernetes with Helm charts, managed Kafka for streaming (Confluent Cloud or MSK), NATS JetStream or Pulsar for queuing (Synadia Cloud, StreamNative Cloud, or self-hosted), managed Cassandra (Astra or self-hosted), and self-hosted Temporal cluster.
- **CI/CD** — GitHub Actions pipelines with automated testing, security scanning, and container image publishing.

### Service Topology

```
┌─────────────────────────────────────────────────────────┐
│                    .NET Aspire Host                      │
├─────────────┬──────────────┬───────────────┬────────────┤
│ Ingress API │ Workflow Svc │ Connector Svc │ Admin API  │
├─────────────┴──────────────┴───────────────┴────────────┤
│                   Message Broker Layer                    │
│           (Kafka streams / RabbitMQ queues)               │
├─────────────────────────────────────────────────────────┤
│              Temporal Server Cluster                    │
├─────────────────────────────────────────────────────────┤
│              Cassandra Cluster (3+ nodes)               │
├─────────────────────────────────────────────────────────┤
│           OpenTelemetry Collector → Backend             │
└─────────────────────────────────────────────────────────┘
```

## Cross-Cutting Concerns

### Configuration Management

All services use the .NET Options pattern with configuration sourced from:
- `appsettings.json` for defaults
- Environment variables for deployment-specific overrides
- Aspire resource configuration for service discovery

### Error Handling

Errors are categorized as transient (retryable) or permanent (non-retryable). Transient errors trigger automatic retry with exponential backoff. Permanent errors route messages to DLQ topics with full diagnostic context.

### Idempotency

Every message carries a unique `MessageId` and `CorrelationId`. The deduplication store in Cassandra ensures that reprocessed messages (due to at-least-once delivery) produce identical outcomes.

### Multi-Tenancy

Tenant isolation is enforced through:
- Kafka topic partitioning by tenant ID for streaming workloads; NATS subjects or Pulsar namespaces with per-recipient key distribution for delivery workloads (built-in multi-tenancy scales to millions of tenants)
- Cassandra partition keys including tenant ID
- Temporal namespace or task queue separation per tenant
- RBAC policies scoped to tenant resources

### Security

- mTLS for all inter-service communication
- OAuth 2.0 / JWT for API authentication
- Secret management through environment variables and secure vaults
- Input validation and sanitization at all ingress points
