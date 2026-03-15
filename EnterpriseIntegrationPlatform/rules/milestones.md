# EnterpriseIntegrationPlatform – Milestones

> **To continue development, tell the AI agent:**
>
> ```
> continue next chunk
> ```
>
> The agent will read this file, find the next chunk with status `not-started`,
> implement it, update the status to `done`, update `Next Chunk`, and log details
> in `rules/completion-log.md`.

## Vision

Build a modern AI-driven Enterprise Integration Platform to replace Microsoft BizTalk Server.  
The platform uses .NET 10, .NET Aspire, a configurable message broker layer, Temporal.io, CassandraDB, OpenTelemetry, and a self-hosted RAG system (RagFlow + Ollama).  
It implements Enterprise Integration Patterns in a cloud-native, horizontally scalable architecture.

**AI-Driven Integration Generation** — The framework focuses on few lines of code. An operator writes a minimal specification and asks AI to auto-generate a complete, production-ready integration. Example prompt: "Generate an integration that maps a message (XML/JSON/flat file) to another format, obtains an auth token from a web API (cached with expiry), and submits the message to another web API with the token."

**Ack/Nack Notification Loopback** — Every integration implements atomic notification semantics: all-or-nothing. On success, publish an Ack. On any failure, publish a Nack. Downstream systems subscribe to Ack/Nack queues to trigger rollback or send notifications back to the sender.

**Zero Message Loss** — Even after restart or outage of full or partial system offline. Every accepted message is either delivered or routed to DLQ. No silent drops.

**11 Quality Pillars** — All design and implementation decisions are guided by the 11 architectural quality pillars defined in `rules/quality-pillars.md`: Reliability, Security, Scalability, Maintainability, Availability, Resilience, Supportability, Observability, Operational Excellence, Testability, Performance.

**Self-Hosted GraphRAG** — The platform includes a self-hosted RAG system (RagFlow + Ollama) running as Aspire containers. The repository's docs, rules, and source code are indexed as the knowledge base. Ollama provides embeddings and retrieval within RagFlow. Developers on any client machine use their own preferred AI provider (Copilot, Codex, Claude Code) connecting to this self-hosted RAG system — the platform retrieves relevant context, and the developer's AI provider generates production-ready code. All data stays on-premises; no data leaves the infrastructure.

## Architecture Decisions

- Replace BizTalk orchestration with Temporal workflows
- **Configurable message broker layer** — The platform uses the right messaging tool for each job:
  - **Kafka** for broadcast event streams, audit logs, fan-out analytics, and decoupled integration — where its partitioned, ordered, high-throughput model excels. Kafka is partitioned and ordered per partition; within a consumer group each partition is consumed by exactly one consumer at a time. This gives strong scalability but creates per-partition serialization — a slow or poison message blocks progress behind it on that partition (Head-of-Line blocking). Kafka is a strong backbone for high-throughput event streaming, but it is not a universal middleware replacement.
  - **Configurable queue broker (default: NATS JetStream; Apache Pulsar with Key_Shared for large-scale production)** for task-oriented message delivery where queue semantics, lower HOL risk, or different consumption guarantees are needed. NATS JetStream is a lightweight, cloud-native single binary with per-subject filtering and queue groups that avoids HOL blocking between subjects — ideal for local development, testing, and cloud deployments. For large-scale production on-prem, Apache Pulsar with Key_Shared subscription distributes messages by key (e.g., recipientId) across consumers — all messages for recipient A stay ordered, while recipient B is processed by another consumer. **Recipient A must not block Recipient B, even at 1 million recipients.** Both brokers support built-in multi-tenancy with lightweight topic creation that scales to millions of tenants without the cost overhead of Kafka topics.
  - **Temporal** for orchestrated business workflows and sagas — Temporal manages long-running, stateful workflow execution with compensation logic.
  - The broker choice between Kafka and the queue broker is a deployment-time configuration switch per message flow category.
- Use Cassandra for scalable distributed persistence
- Use Aspire AppHost to orchestrate the platform locally
- Integrate Ollama for RAG retrieval within RagFlow; self-hosted knowledge API for developers
- Self-hosted GraphRAG via RagFlow + Ollama — index docs, rules, and source code; developers connect their own AI provider to retrieve context from any client machine
- OpenTelemetry for end-to-end observability
- Saga-based distributed transactions via Temporal
- Target .NET 10 (C# 14) with .NET Aspire 13.1.2
- Non-common Aspire host ports (15xxx range) to avoid conflicts with existing services

## Phases

### Phase 1 – Foundations

| Chunk | Name | Goal | Status |
|-------|------|------|--------|
| 001 | Repository scaffold | Create solution structure, projects, directory layout | done |
| 002 | GitHub Actions CI pipeline | Automated build and test on every push/PR | done |
| 003 | Aspire AppHost infrastructure | Configure Aspire AppHost with service defaults | done |
| 004 | Contracts and canonical message envelope | Define shared message contracts | done |
| 005 | Configurable message broker ingestion | Implement broker abstraction with Kafka, NATS JetStream (default), and Pulsar (Key_Shared) providers for message ingestion | done |
| 006 | Temporal workflow host | Set up Temporal worker and workflow definitions with validation activities | done |
| 007 | Cassandra storage module | Implement Cassandra repository and data access | done |
| 008 | Ollama AI integration | Integrate Ollama for RAG retrieval within RagFlow and trace analysis | done |
| 009 | OpenTelemetry observability | Configure distributed tracing, metrics (Prometheus), isolated observability storage (Loki), OpenClaw web UI with Playwright tests, RagFlow + Ollama in Aspire, AI.RagFlow client, OpenClaw generation endpoints (POST /api/generate/integration, POST /api/generate/chat), non-common Aspire ports (15xxx range) | done |
| 010 | Admin API | Build administration API for platform management | done |
| 011 | End-to-end demo pipeline | Wire all components into a working demo pipeline | done |

### Phase 2 – Integration Patterns

| Chunk | Name | Status |
|-------|------|--------|
| 012 | Content-Based Router | not-started |
| 013 | Message Translator | not-started |
| 014 | Splitter | not-started |
| 015 | Aggregator | not-started |
| 016 | Dead Letter Queue | not-started |
| 017 | Retry framework | not-started |
| 018 | Replay framework | not-started |

### Phase 3 – Connectors

| Chunk | Name | Status |
|-------|------|--------|
| 019 | HTTP connector | not-started |
| 020 | SFTP connector | not-started |
| 021 | Email connector | not-started |
| 022 | File connector | not-started |

### Phase 4 – Hardening

| Chunk | Name | Status |
|-------|------|--------|
| 023 | Security | not-started |
| 024 | Multi-tenancy | not-started |
| 025 | Saga compensation | not-started |
| 026 | Load testing | not-started |
| 027 | Operational tooling | not-started |
| 028 | AI-assisted code generation | not-started |

## Next Chunk

Chunk 012 – Content-Based Router

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
