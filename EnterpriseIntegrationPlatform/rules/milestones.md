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
The platform uses .NET 10, .NET Aspire, Kafka, Temporal.io, CassandraDB, OpenTelemetry, and Ollama.  
It implements Enterprise Integration Patterns in a cloud-native, horizontally scalable architecture.

## Architecture Decisions

- Replace BizTalk orchestration with Temporal workflows
- Replace ESB-style message brokers with event-driven Kafka backbone
- Use Cassandra for scalable distributed persistence
- Use Aspire AppHost to orchestrate the platform locally
- Integrate Ollama for AI-assisted development and autonomous code generation
- OpenTelemetry for end-to-end observability
- Saga-based distributed transactions via Temporal
- Target .NET 10 (C# 14) with .NET Aspire 13.1.2

## Phases

### Phase 1 – Foundations

| Chunk | Name | Goal | Status |
|-------|------|------|--------|
| 001 | Repository scaffold | Create solution structure, projects, directory layout | done |
| 002 | GitHub Actions CI pipeline | Automated build and test on every push/PR | done |
| 003 | Aspire AppHost infrastructure | Configure Aspire AppHost with service defaults | done |
| 004 | Contracts and canonical message envelope | Define shared message contracts | done |
| 005 | Kafka ingestion service | Implement Kafka consumer/producer for message ingestion | not-started |
| 006 | Temporal workflow host | Set up Temporal worker and basic workflow definitions | not-started |
| 007 | Cassandra storage module | Implement Cassandra repository and data access | not-started |
| 008 | Ollama AI integration | Integrate Ollama for AI-assisted operations | done |
| 009 | OpenTelemetry observability | Configure distributed tracing, metrics (Prometheus), isolated observability storage, OpenClaw web UI with Playwright tests | done |
| 010 | Admin API | Build administration API for platform management | not-started |
| 011 | End-to-end demo pipeline | Wire all components into a working demo pipeline | not-started |

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

Chunk 005 – Kafka ingestion service

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
