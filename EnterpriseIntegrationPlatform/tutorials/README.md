# Enterprise Integration Platform — Tutorial Course

50 tutorials. Each one: key types, code exercises with assertions, runnable labs and exams. No theory walls.

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Basic C# knowledge

## Running Labs & Exams

```bash
# All 522 exercises
dotnet test tests/TutorialLabs/TutorialLabs.csproj

# Single tutorial (e.g. Tutorial 09)
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial09"

# Only exams
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial09.Exam"
```

## Tutorial Format

Every tutorial follows the same structure:

1. **One-line description** — what the pattern does
2. **Key Types** — actual interfaces/records/enums from `src/`
3. **Exercises** — 3–7 working C# code blocks with `Assert` statements
4. **Lab** — link to `Lab.cs` + `dotnet test` command (5–10 tests per tutorial)
5. **Exam** — link to `Exam.cs` + `dotnet test` command (3 coding challenges per tutorial)

## Course Map

### Part 1 — Getting Started

| # | Tutorial |
|---|----------|
| 01 | [Introduction to Enterprise Integration](01-introduction.md) |
| 02 | [Setting Up Your Environment](02-environment-setup.md) |
| 03 | [Your First Message](03-first-message.md) |

### Part 2 — Core Concepts

| # | Tutorial |
|---|----------|
| 04 | [The Integration Envelope](04-integration-envelope.md) |
| 05 | [Message Brokers](05-message-brokers.md) |
| 06 | [Messaging Channels](06-messaging-channels.md) |
| 07 | [Temporal Workflows](07-temporal-workflows.md) |
| 08 | [Activities and the Pipeline](08-activities-pipeline.md) |

### Part 3 — Message Routing

| # | Tutorial |
|---|----------|
| 09 | [Content-Based Router](09-content-based-router.md) |
| 10 | [Message Filter](10-message-filter.md) |
| 11 | [Dynamic Router](11-dynamic-router.md) |
| 12 | [Recipient List](12-recipient-list.md) |
| 13 | [Routing Slip](13-routing-slip.md) |
| 14 | [Process Manager](14-process-manager.md) |

### Part 4 — Message Transformation

| # | Tutorial |
|---|----------|
| 15 | [Message Translator](15-message-translator.md) |
| 16 | [Transform Pipeline](16-transform-pipeline.md) |
| 17 | [Normalizer](17-normalizer.md) |
| 18 | [Content Enricher](18-content-enricher.md) |
| 19 | [Content Filter](19-content-filter.md) |

### Part 5 — Message Construction & Decomposition

| # | Tutorial |
|---|----------|
| 20 | [Splitter](20-splitter.md) |
| 21 | [Aggregator](21-aggregator.md) |
| 22 | [Scatter-Gather](22-scatter-gather.md) |
| 23 | [Request-Reply](23-request-reply.md) |

### Part 6 — Reliability & Error Handling

| # | Tutorial |
|---|----------|
| 24 | [Retry Framework](24-retry-framework.md) |
| 25 | [Dead Letter Queue](25-dead-letter-queue.md) |
| 26 | [Message Replay](26-message-replay.md) |
| 27 | [Resequencer](27-resequencer.md) |
| 28 | [Competing Consumers](28-competing-consumers.md) |

### Part 7 — Advanced Patterns

| # | Tutorial |
|---|----------|
| 29 | [Throttle and Rate Limiting](29-throttle-rate-limiting.md) |
| 30 | [Business Rule Engine](30-rule-engine.md) |
| 31 | [Event Sourcing](31-event-sourcing.md) |
| 32 | [Multi-Tenancy](32-multi-tenancy.md) |
| 33 | [Security](33-security.md) |

### Part 8 — Connectors

| # | Tutorial |
|---|----------|
| 34 | [HTTP Connector](34-connector-http.md) |
| 35 | [SFTP Connector](35-connector-sftp.md) |
| 36 | [Email Connector](36-connector-email.md) |
| 37 | [File Connector](37-connector-file.md) |

### Part 9 — Observability & AI

| # | Tutorial |
|---|----------|
| 38 | [OpenTelemetry Observability](38-opentelemetry.md) |
| 39 | [Message Lifecycle Tracking](39-message-lifecycle.md) |
| 40 | [Self-Hosted RAG with Ollama](40-rag-ollama.md) |
| 41 | [OpenClaw Web UI](41-openclaw-web.md) |

### Part 10 — Production Deployment

| # | Tutorial |
|---|----------|
| 42 | [Dynamic Configuration](42-configuration.md) |
| 43 | [Kubernetes Deployment](43-kubernetes-deployment.md) |
| 44 | [Disaster Recovery](44-disaster-recovery.md) |
| 45 | [Performance Profiling](45-performance-profiling.md) |

### Part 11 — Real-World Scenarios

| # | Tutorial |
|---|----------|
| 46 | [Building a Complete Integration](46-complete-integration.md) |
| 47 | [Saga Compensation Pattern](47-saga-compensation.md) |
| 48 | [Notification Use Cases](48-notification-use-cases.md) |
| 49 | [Testing Your Integrations](49-testing-integrations.md) |
| 50 | [Best Practices and Patterns](50-best-practices.md) |

## Project Structure

```
tests/TutorialLabs/
├── TutorialLabs.csproj
├── Tutorial01/
│   ├── Lab.cs       # Runnable tests demonstrating the pattern
│   └── Exam.cs      # Coding challenges — make the tests pass
├── Tutorial02/
│   ├── Lab.cs
│   └── Exam.cs
├── ...
└── Tutorial50/
    ├── Lab.cs
    └── Exam.cs
```

**522 total exercises** across 50 tutorials.

## Quick Reference

- [EIP Pattern Mapping](../docs/eip-mapping.md)
- [Architecture Overview](../docs/architecture-overview.md)
- [Developer Setup](../docs/developer-setup.md)
- [Coding Standards](../rules/coding-standards.md)
