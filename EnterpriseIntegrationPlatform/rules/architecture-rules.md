# Architecture Rules

## General Principles

1. **Separation of Concerns** – Each project has a single responsibility
2. **Dependency Inversion** – Depend on abstractions, not implementations
3. **Event-Driven Architecture** – Kafka is the primary communication backbone
4. **Workflow Orchestration** – Temporal manages all long-running processes
5. **Distributed by Default** – Design for horizontal scaling from day one

## Project Dependency Rules

- `Contracts` has ZERO project dependencies (pure DTOs and interfaces)
- `ServiceDefaults` has ZERO project dependencies (cross-cutting defaults)
- `Activities` depends only on `Contracts`
- `Workflow.Temporal` depends on `Contracts` and `Activities`
- `Gateway.Api` depends on `Contracts` and `Ingestion.Kafka`
- `Ingestion.Kafka` depends on `Contracts`
- `Storage.Cassandra` depends on `Contracts`
- `Processing.*` projects depend on `Contracts`
- `Connector.*` projects depend on `Contracts`
- `AI.Ollama` depends on `Contracts`
- `RuleEngine` depends on `Contracts`
- `Admin.Api` depends on `Contracts`
- `AppHost` references all service projects for orchestration
- `Observability` depends on `Contracts` and `ServiceDefaults`
- Test projects may reference any src project

## Communication Patterns

- **Synchronous**: REST/gRPC via Gateway.Api only for external consumers
- **Asynchronous**: Kafka for all inter-service communication
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
