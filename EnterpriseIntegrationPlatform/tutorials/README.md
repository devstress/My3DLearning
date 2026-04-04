# Enterprise Integration Platform — Complete Tutorial Course

> **From Beginner to Expert: Learn Enterprise Integration Patterns with a Modern .NET 10 Platform**

This tutorial series takes you from zero to expert with the Enterprise Integration Platform (EIP). Every tutorial builds on the previous one, introducing one concept at a time with clear explanations, architecture diagrams, and real code examples from the platform.

The course is based on the patterns defined in [*Enterprise Integration Patterns*](https://www.enterpriseintegrationpatterns.com/) by Gregor Hohpe and Bobby Woolf — the definitive reference for messaging-based integration.

---

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for infrastructure containers)
- Basic C# knowledge
- A code editor (Visual Studio, VS Code, or Rider)

---

## Course Structure

### Part 1 — Getting Started

| # | Tutorial | Description |
|---|----------|-------------|
| 01 | [Introduction to Enterprise Integration](01-introduction.md) | What is enterprise integration? Why messaging? The EIP book and this platform. |
| 02 | [Setting Up Your Environment](02-environment-setup.md) | Install prerequisites, clone the repo, build, run tests, launch Aspire. |
| 03 | [Your First Message](03-first-message.md) | Create and publish an `IntegrationEnvelope`, consume it, see it flow through the system. |

### Part 2 — Core Concepts

| # | Tutorial | Description |
|---|----------|-------------|
| 04 | [The Integration Envelope](04-integration-envelope.md) | Deep dive into `IntegrationEnvelope<T>` — the canonical message format. |
| 05 | [Message Brokers](05-message-brokers.md) | Kafka, NATS JetStream, and Apache Pulsar — when to use each and how to configure them. |
| 06 | [Messaging Channels](06-messaging-channels.md) | Point-to-Point, Publish-Subscribe, Datatype, Invalid Message, and Messaging Bridge channels. |
| 07 | [Temporal Workflows](07-temporal-workflows.md) | How Temporal.io orchestrates message processing with durable execution. |
| 08 | [Activities and the Pipeline](08-activities-pipeline.md) | Stateless activities, the integration pipeline, and how messages flow end-to-end. |

### Part 3 — Message Routing

| # | Tutorial | Description |
|---|----------|-------------|
| 09 | [Content-Based Router](09-content-based-router.md) | Route messages to different destinations based on payload content. |
| 10 | [Message Filter](10-message-filter.md) | Accept or discard messages using predicate rules. |
| 11 | [Dynamic Router](11-dynamic-router.md) | Runtime-learned routing tables with control channel registration. |
| 12 | [Recipient List](12-recipient-list.md) | Fan-out messages to multiple resolved destinations. |
| 13 | [Routing Slip](13-routing-slip.md) | Per-message processing pipelines with ordered step execution. |
| 14 | [Process Manager](14-process-manager.md) | Complex multi-step orchestration with Temporal workflows. |

### Part 4 — Message Transformation

| # | Tutorial | Description |
|---|----------|-------------|
| 15 | [Message Translator](15-message-translator.md) | Transform payloads between formats (JSON↔XML, CSV→JSON). |
| 16 | [Transform Pipeline](16-transform-pipeline.md) | Chain multiple transform steps for sequential processing. |
| 17 | [Normalizer](17-normalizer.md) | Detect incoming format and convert to canonical JSON. |
| 18 | [Content Enricher](18-content-enricher.md) | Augment messages with data from external sources. |
| 19 | [Content Filter](19-content-filter.md) | Remove unwanted fields from payloads using JSONPath. |

### Part 5 — Message Construction & Decomposition

| # | Tutorial | Description |
|---|----------|-------------|
| 20 | [Splitter](20-splitter.md) | Break batch messages into individual items. |
| 21 | [Aggregator](21-aggregator.md) | Collect related messages and combine into a single output. |
| 22 | [Scatter-Gather](22-scatter-gather.md) | Send requests to multiple recipients, collect and merge responses. |
| 23 | [Request-Reply](23-request-reply.md) | Synchronous request-response over asynchronous messaging. |

### Part 6 — Reliability & Error Handling

| # | Tutorial | Description |
|---|----------|-------------|
| 24 | [Retry Framework](24-retry-framework.md) | Exponential backoff, max attempts, and non-retryable error classification. |
| 25 | [Dead Letter Queue](25-dead-letter-queue.md) | Route failed messages to DLQ with diagnostic metadata. |
| 26 | [Message Replay](26-message-replay.md) | Replay failed or DLQ messages back into the pipeline. |
| 27 | [Resequencer](27-resequencer.md) | Reorder out-of-sequence messages by sequence number. |
| 28 | [Competing Consumers](28-competing-consumers.md) | Horizontal scaling with auto-scaling and backpressure. |

### Part 7 — Advanced Patterns

| # | Tutorial | Description |
|---|----------|-------------|
| 29 | [Throttle and Rate Limiting](29-throttle-rate-limiting.md) | Token bucket throttling with per-tenant partitioning. |
| 30 | [Business Rule Engine](30-rule-engine.md) | Conditions, operators, actions, and AND/OR logic for message processing. |
| 31 | [Event Sourcing](31-event-sourcing.md) | Event store, snapshots, projections, and temporal queries. |
| 32 | [Multi-Tenancy](32-multi-tenancy.md) | Tenant resolution, isolation guards, and per-tenant configuration. |
| 33 | [Security](33-security.md) | Input sanitization, payload guards, encryption, and secret management. |

### Part 8 — Connectors

| # | Tutorial | Description |
|---|----------|-------------|
| 34 | [HTTP Connector](34-connector-http.md) | REST API delivery with OAuth 2.0, Bearer, API Key authentication. |
| 35 | [SFTP Connector](35-connector-sftp.md) | SFTP file delivery with SSH key auth and atomic rename. |
| 36 | [Email Connector](36-connector-email.md) | SMTP/SMTPS email delivery with templates and attachments. |
| 37 | [File Connector](37-connector-file.md) | Local/NFS/SMB file writing with atomic operations. |

### Part 9 — Observability & AI

| # | Tutorial | Description |
|---|----------|-------------|
| 38 | [OpenTelemetry Observability](38-opentelemetry.md) | Distributed tracing, metrics, and structured logging. |
| 39 | [Message Lifecycle Tracking](39-message-lifecycle.md) | "Where is my message?" — tracking state across the pipeline. |
| 40 | [Self-Hosted RAG with Ollama](40-rag-ollama.md) | RagFlow + Ollama for AI-powered knowledge retrieval. |
| 41 | [OpenClaw Web UI](41-openclaw-web.md) | Natural language message search and the OpenClaw interface. |

### Part 10 — Production Deployment

| # | Tutorial | Description |
|---|----------|-------------|
| 42 | [Dynamic Configuration](42-configuration.md) | Feature flags, environment overrides, and change notification. |
| 43 | [Kubernetes Deployment](43-kubernetes-deployment.md) | Helm charts, Kustomize overlays, and production manifests. |
| 44 | [Disaster Recovery](44-disaster-recovery.md) | Failover, replication, RPO/RTO targets, and DR drills. |
| 45 | [Performance Profiling](45-performance-profiling.md) | CPU/memory profiling, GC tuning, and benchmarks. |

### Part 11 — Real-World Scenarios

| # | Tutorial | Description |
|---|----------|-------------|
| 46 | [Building a Complete Integration](46-complete-integration.md) | End-to-end: receive HTTP → transform → route → deliver SFTP. |
| 47 | [Saga Compensation Pattern](47-saga-compensation.md) | Distributed transactions with automatic rollback. |
| 48 | [Migrating from BizTalk](48-migrating-from-biztalk.md) | Step-by-step migration strategy for BizTalk Server users. |
| 49 | [Testing Your Integrations](49-testing-integrations.md) | Unit tests, contract tests, integration tests, and load tests. |
| 50 | [Best Practices and Patterns](50-best-practices.md) | Design guidelines, anti-patterns, and production checklist. |

---

## How to Use This Course

1. **Start from Tutorial 01** if you are new to enterprise integration
2. **Jump to a specific tutorial** if you already know the basics and want to learn a specific pattern
3. **Each tutorial is self-contained** with context, but builds on earlier concepts
4. **Code examples reference actual platform source files** — open them side-by-side
5. **Practice exercises** at the end of each tutorial reinforce learning

## Quick Reference

- [EIP Pattern Mapping](../docs/eip-mapping.md) — Complete pattern-to-component mapping
- [Architecture Overview](../docs/architecture-overview.md) — System architecture diagrams
- [Developer Setup](../docs/developer-setup.md) — Detailed setup instructions
- [Coding Standards](../rules/coding-standards.md) — Code style and conventions
