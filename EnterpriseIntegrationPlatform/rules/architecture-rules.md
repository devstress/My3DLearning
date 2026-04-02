# Architecture Rules

> All architecture decisions must satisfy the 11 Quality Pillars in `rules/quality-pillars.md`.

## General Principles

1. **Separation of Concerns** – Each project has a single responsibility
2. **Dependency Inversion** – Depend on abstractions, not implementations
3. **Configurable Message Broker** – The broker layer is abstraction-based: Kafka for broadcast event streams, audit logs, and fan-out analytics; a configurable queue broker (default: NATS JetStream for local dev and cloud; Apache Pulsar with Key_Shared for large-scale production) for task-oriented delivery where messages are distributed by recipient key — recipient A must not block recipient B, even at 1 million recipients. The broker choice is a deployment-time configuration switch.
4. **Workflow Orchestration** – Temporal manages all long-running processes
5. **Distributed by Default** – Design for horizontal scaling from day one
6. **Ack/Nack Notification Loopback** – Every integration implements atomic notification semantics: all-or-nothing. On success, publish Ack. On any failure, publish Nack. Downstream systems subscribe to Ack/Nack queues for rollback or sender notification.
7. **Zero Message Loss** – Every accepted message is either delivered or routed to DLQ. No silent drops, even after restart or full/partial system outage.
8. **Self-Hosted GraphRAG** – The platform includes a self-hosted RAG (Retrieval-Augmented Generation) system via RagFlow + Ollama, both running as Aspire containers. The repository's own docs, rules, and source code are indexed as the knowledge base. Ollama provides embeddings and retrieval within RagFlow. Developers on any client machine use their own preferred AI provider (Copilot, Codex, Claude Code) connecting to this self-hosted RAG system — the platform retrieves relevant context, and the developer's AI provider generates code. No data leaves the infrastructure.
9. **Non-Common Ports** – All Aspire container host ports use the 15xxx range (Ollama 15434, RagFlow 15080/15380, Loki 15100, Temporal 15233/15280, NATS 15222, Cassandra 15042) to avoid conflicts with existing services.

## Project Dependency Rules

- `Contracts` has ZERO project dependencies (pure DTOs and interfaces)
- `ServiceDefaults` has ZERO project dependencies (cross-cutting defaults)
- `Activities` depends only on `Contracts`
- `Workflow.Temporal` depends on `Contracts` and `Activities`
- `Ingestion` depends on `Contracts` (broker abstraction; Kafka, NATS, and Pulsar providers)
- `Ingestion.Kafka` depends on `Ingestion` and `Contracts`
- `Ingestion.Nats` depends on `Ingestion` and `Contracts`
- `Ingestion.Pulsar` depends on `Ingestion` and `Contracts`
- `Storage.Cassandra` depends on `Contracts`
- `Processing.*` projects depend on `Contracts`
- `Connector.*` projects depend on `Contracts`
- `AI.Ollama` depends on `Contracts`
- `AI.RagFlow` has ZERO project dependencies (standalone RAG client)
- `Security` depends on `Contracts`
- `MultiTenancy` depends on `Contracts`
- `Admin.Api` depends on `Contracts`
- `Demo.Pipeline` depends on `Contracts` and `Ingestion`
- `AppHost` references all service projects for orchestration
- `Observability` depends on `Contracts` and `ServiceDefaults`
- Test projects may reference any src project

## Communication Patterns

- **Synchronous**: REST via Admin.Api for platform management; connector HTTP delivery for outbound
- **Asynchronous (streaming)**: Kafka for broadcast event streams, audit logs, fan-out analytics, and decoupled integration
- **Asynchronous (queuing)**: Configurable queue broker (default: NATS JetStream; Pulsar Key_Shared for large-scale production) for task-oriented delivery — messages keyed by recipientId are distributed across consumers so recipient A never blocks recipient B, even at 1 million recipients
- **Orchestration**: Temporal for complex workflows and sagas
- **Storage**: Cassandra for all persistent state

## Data Rules

- All messages use the canonical `IntegrationEnvelope<T>` format
- Messages are immutable once created
- Every message has a correlation ID
- Every message has a timestamp (UTC)
- Message schemas are versioned

## Resilience Rules

- All external calls must have retry policies
- Circuit breakers on all connector outbound calls
- Dead letter queues for unprocessable messages
- Idempotent message processing required
- Compensation logic for saga rollbacks

## Observability Rules

- All services emit OpenTelemetry traces
- All services emit structured logs
- Health checks on every service
- Metrics for throughput, latency, error rates

## Security Rules

- No secrets in source code
- Use Aspire configuration for secret management
- Validate all input at service boundaries
- Authorize all Admin API endpoints
