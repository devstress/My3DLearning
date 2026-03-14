# EnterpriseIntegrationPlatform – Milestones

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
| 002 | Aspire AppHost infrastructure | Configure Aspire AppHost with service defaults | not-started |
| 003 | Contracts and canonical message envelope | Define shared message contracts | not-started |
| 004 | Kafka ingestion service | Implement Kafka consumer/producer for message ingestion | not-started |
| 005 | Temporal workflow host | Set up Temporal worker and basic workflow definitions | not-started |
| 006 | Cassandra storage module | Implement Cassandra repository and data access | not-started |
| 007 | Ollama AI integration | Integrate Ollama for AI-assisted operations | not-started |
| 008 | OpenTelemetry observability | Configure distributed tracing, metrics, and logging | not-started |
| 009 | Admin API | Build administration API for platform management | not-started |
| 010 | End-to-end demo pipeline | Wire all components into a working demo pipeline | not-started |

### Phase 2 – Integration Patterns

| Chunk | Name | Status |
|-------|------|--------|
| 011 | Content-Based Router | not-started |
| 012 | Message Translator | not-started |
| 013 | Splitter | not-started |
| 014 | Aggregator | not-started |
| 015 | Dead Letter Queue | not-started |
| 016 | Retry framework | not-started |
| 017 | Replay framework | not-started |

### Phase 3 – Connectors

| Chunk | Name | Status |
|-------|------|--------|
| 018 | HTTP connector | not-started |
| 019 | SFTP connector | not-started |
| 020 | Email connector | not-started |
| 021 | File connector | not-started |

### Phase 4 – Hardening

| Chunk | Name | Status |
|-------|------|--------|
| 022 | Security | not-started |
| 023 | Multi-tenancy | not-started |
| 024 | Saga compensation | not-started |
| 025 | Load testing | not-started |
| 026 | Operational tooling | not-started |
| 027 | AI-assisted code generation | not-started |

## Next Chunk

Chunk 002 – Aspire AppHost infrastructure

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
