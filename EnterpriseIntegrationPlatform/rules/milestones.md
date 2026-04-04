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

> **ENFORCEMENT RULE — Completed chunks must be removed from this file.**
>
> When a chunk is marked `done`:
> 1. Log full details (date, goal, architecture, files created/modified, test counts) in `rules/completion-log.md`.
> 2. **Remove the done row from this file.** Milestones.md contains only `not-started` chunks.
> 3. If an entire phase has no remaining rows, replace the table with: `✅ Phase N complete — see completion-log.md`.
> 4. Update the `Next Chunk` section to point to the next `not-started` chunk.
>
> This rule is mandatory for every AI agent session. Never leave done rows in milestones.md.

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

## Completed Phases

✅ Phase 1 (Foundations, chunks 001-011) complete — see completion-log.md  
✅ Phase 2 (Integration Patterns, chunks 012-018) complete — see completion-log.md  
✅ Phase 3 (Connectors, chunks 019-022) complete — see completion-log.md  
✅ Phase 4 (Hardening, chunks 023-028) complete — see completion-log.md  
✅ Phase 5 (Production Readiness, chunks 029-034) complete — see completion-log.md  
✅ Phase 6 (Advanced Patterns & Scale, chunks 035-040) complete — see completion-log.md

## Next Chunk

**Chunk 052** (Content Enricher + Content Filter) is next.

---

### Phase 6 – Advanced Patterns & Scale

✅ Phase 6 complete — see completion-log.md

### Phase 7 – Missing EIP Patterns (Messaging Channels & Construction)

✅ Phase 7 complete — see completion-log.md

### Phase 8 – Missing EIP Patterns (Routing & Transformation)

| Chunk | Name | Goal | Tests Required |
|-------|------|------|----------------|
| 052 | Content Enricher + Content Filter | (a) Add `IContentEnricher` and `ContentEnricher` in Processing.Transform/ that augments an envelope's payload with data fetched from an external source (e.g., HTTP lookup, Cassandra query). (b) Add `IContentFilter` and `ContentFilter` that strips fields from a payload, keeping only specified paths — the inverse of enrichment. Both are Temporal activities. | UnitTests: ≥12 (enrich with HTTP mock, enrich with missing data fallback, filter keep-fields, filter nested paths) |
| 053 | Normalizer + Canonical Data Model | (a) Add `INormalizer` and `MessageNormalizer` in Processing.Transform/ that detects incoming format (JSON, XML, CSV, flat-file) and converts to a canonical JSON representation using existing Transform pipeline steps. (b) Document the Canonical Data Model pattern as `IntegrationEnvelope<T>` itself — the envelope IS the canonical model. | UnitTests: ≥10 (normalize XML→JSON, CSV→JSON, already-JSON passthrough, unknown format error) |

### Phase 9 – Missing EIP Patterns (Endpoints & System Management)

| Chunk | Name | Goal | Tests Required |
|-------|------|------|----------------|
| 054 | Messaging Gateway + Messaging Mapper | (a) Formalize `Gateway.Api` as the Messaging Gateway pattern — verify it encapsulates all broker access behind a clean HTTP API. (b) Add `IMessagingMapper<TDomain, TMessage>` interface in Contracts/ for mapping domain objects to/from `IntegrationEnvelope`. Provide a `JsonMessagingMapper` implementation. | UnitTests: ≥8 (domain→envelope mapping, envelope→domain mapping, null handling, metadata preservation) |
| 055 | Transactional Client | Add `ITransactionalClient` in Ingestion/ that wraps publish+consume in a transactional scope — for brokers that support transactions (Kafka). For NATS/Pulsar, implement via Temporal workflow (publish-then-confirm pattern). Ensures produce-and-consume atomicity. | UnitTests: ≥8 (commit success, rollback on failure, timeout, non-transactional broker fallback) |
| 056 | Polling Consumer + Event-Driven Consumer + Selective Consumer + Durable Subscriber | (a) Formalize `PollingConsumer` and `EventDrivenConsumer` as named wrappers in Ingestion/ — Kafka consumer = Polling, NATS push = Event-Driven. (b) Add `ISelectiveConsumer` that wraps `IMessageBrokerConsumer` with a predicate filter (consume only messages matching criteria). (c) Add `DurableSubscriber` wrapper ensuring subscription state survives restarts (already inherent in Kafka/NATS/Pulsar — formalize with interface + tests). | UnitTests: ≥12 (polling consume, event-driven consume, selective filter, durable reconnect) |
| 057 | Message Dispatcher + Service Activator | (a) Add `IMessageDispatcher` in Processing/ that receives messages from a single channel and distributes to specific handlers based on message type (like a multiplexer). (b) Add `IServiceActivator` that invokes a service operation (sync or async) from a message and optionally publishes the reply. Key pattern for request-reply orchestration. | UnitTests: ≥10 (dispatch by type, unknown type handling, activator invoke+reply, activator invoke-only) |
| 058 | System Management — Control Bus, Detour, Message History, Message Store, Smart Proxy, Test Message, Channel Purger | (a) Formalize `Admin.Api` as the **Control Bus** pattern — admin endpoints already exist, add explicit control-message publish/subscribe for runtime config changes. (b) Add `IDetour` in Processing.Routing/ — conditional routing through validation/debug/test pipeline before normal processing. (c) Add `MessageHistory` record type in Contracts/ tracking processing step chain (activity name + timestamp + status) — attach to envelope metadata. (d) Formalize `Storage.Cassandra` message tables as the **Message Store** pattern. (e) Add `ISmartProxy` that tracks outstanding request-reply and correlates Return Address responses. (f) Add `ITestMessageGenerator` that publishes synthetic test messages through the pipeline for health verification. (g) Add `IChannelPurger` in Ingestion/ that drains all messages from a specified topic/subject. | UnitTests: ≥20 (detour routing, message history chain, test message generation, channel purge, smart proxy correlation) |

### Phase 10 – Connectors & Test Coverage Hardening

| Chunk | Name | Goal | Tests Required |
|-------|------|------|----------------|
| 059 | Connectors unification | Register Connector.Http/Sftp/Email/File adapters into unified `IConnectorRegistry` + `IConnectorFactory` from src/Connectors/. Runtime connector resolution by name and type. Health check aggregation across all registered connectors. | UnitTests: ≥12 (register, resolve by name, resolve by type, factory create, health aggregation) |
| 060 | Test coverage hardening | Close all test coverage gaps: (a) AI.Ollama — ≥8 unit tests (client wrapper, embedding generation, health check). (b) Admin.Api — ≥10 endpoint tests (DLQ list/replay/discard, throttle CRUD, rate-limit status, DR endpoints). (c) Configuration — ≥8 tests (feature flag evaluation, environment override, change notification). (d) InMemoryRuleStore — expand to ≥5 more edge-case tests. (e) Connectors unified project — ≥10 tests (registry CRUD, factory, descriptor). (f) AI.RagFlow — expand to ≥5 more tests. | UnitTests: ≥46 new tests total |

### Phase 11 – Admin Dashboard & RAG

| Chunk | Name | Goal | Tests Required |
|-------|------|------|----------------|
| 061 | Admin.Web (Vue 3) | Vue 3 admin dashboard frontend for Admin.Api — tenant/queue/endpoint throttle control, rate limit status, DLQ management, message inspection, policy CRUD, DR drill execution, profiling snapshots | PlaywrightTests: ≥8 new (dashboard load, DLQ list, throttle CRUD, DR drill trigger) |
| 062 | RAG Knowledge Base | XML-based RAG knowledge store under docs/rag/ with platform documentation indexed for RagFlow retrieval. Deployable with Aspire or standalone. Covers all EIP patterns, usage guides, and implementation reference | UnitTests: ≥5 (RAG document parsing, index generation, query matching) |

### Phase 12 – Documentation

| Chunk | Name | Goal | Tests Required |
|-------|------|------|----------------|
| 063 | EIP Pattern Documentation | docs/ folder covering ALL Enterprise Integration Patterns implemented — full mapping from book TOC to platform components with usage examples from actual implementation. Must cover all 65 patterns from the EIP book. | N/A (documentation only) |
| 064 | Platform Usage Guide | End-to-end usage documentation: getting started, configuration, deployment (K8s/Docker), connector setup, throttle/rate-limit tuning, multi-tenancy, security, observability — focused on EnterpriseIntegrationPlatform features only | N/A (documentation only) |
| 065 | API Reference | Complete API reference for Admin.Api, Gateway.Api, OpenClaw.Web endpoints with request/response examples, authentication, and rate limit/throttle admin operations | N/A (documentation only) |

---

### EIP Book Pattern Checklist

> Cross-reference against https://www.enterpriseintegrationpatterns.com/patterns/messaging/toc.html
> ✅ = implemented and tested, 🔲 = chunk planned, — = architectural (no dedicated code needed)

**Integration Styles:**
- — File Transfer (Connector.File)
- — Shared Database (Storage.Cassandra)
- — Remote Procedure Invocation (Connector.Http)
- — Messaging (core architecture)

**Messaging Systems:**
- ✅ Message Channel (Ingestion broker abstraction)
- ✅ Message (IntegrationEnvelope)
- ✅ Pipes and Filters (Temporal activity chains + Processing.Transform)
- ✅ Message Router (Processing.Routing)
- ✅ Message Translator (Processing.Translator + Processing.Transform)
- 🔲 Message Endpoint (chunk 056 — formalize consumer patterns)

**Messaging Channels:**
- ✅ Point-to-Point Channel (Ingestion.Channels.PointToPointChannel)
- ✅ Publish-Subscribe Channel (Ingestion.Channels.PublishSubscribeChannel)
- ✅ Datatype Channel (Ingestion.Channels.DatatypeChannel)
- ✅ Invalid Message Channel (Ingestion.Channels.InvalidMessageChannel)
- ✅ Dead Letter Channel (Processing.DeadLetter)
- ✅ Guaranteed Delivery (Kafka + Temporal)
- ✅ Channel Adapter (Connector.Http/Sftp/Email/File)
- ✅ Messaging Bridge (Ingestion.Channels.MessagingBridge)
- ✅ Message Bus (the platform IS the message bus — documented)

**Message Construction:**
- ✅ Command Message (IntegrationEnvelope.Intent = Command)
- ✅ Document Message (IntegrationEnvelope.Intent = Document)
- ✅ Event Message (IntegrationEnvelope.Intent = Event)
- ✅ Request-Reply (Processing.RequestReply.RequestReplyCorrelator)
- ✅ Return Address (IntegrationEnvelope.ReplyTo)
- ✅ Correlation Identifier (IntegrationEnvelope.CorrelationId)
- ✅ Message Sequence (IntegrationEnvelope.SequenceNumber/TotalCount)
- ✅ Message Expiration (IntegrationEnvelope.ExpiresAt + MessageExpirationChecker)
- ✅ Format Indicator (MessageHeaders.ContentType — formalized)

**Message Routing:**
- ✅ Content-Based Router (Processing.Routing)
- ✅ Message Filter (Processing.Routing.MessageFilter)
- ✅ Dynamic Router (Processing.Routing.DynamicRouter)
- ✅ Recipient List (Processing.Routing.RecipientListRouter)
- ✅ Splitter (Processing.Splitter)
- ✅ Aggregator (Processing.Aggregator)
- 🔲 Resequencer (chunk 051)
- ✅ Composed Message Processor (Splitter + Transform + Aggregator pipeline)
- ✅ Scatter-Gather (Processing.ScatterGather)
- ✅ Routing Slip (Processing.Routing.RoutingSlipRouter)
- ✅ Process Manager (Temporal Workflows)
- — Message Broker (the platform IS the broker)

**Message Transformation:**
- ✅ Envelope Wrapper (IntegrationEnvelope)
- 🔲 Content Enricher (chunk 052)
- 🔲 Content Filter (chunk 052)
- ✅ Claim Check (Storage.Cassandra)
- 🔲 Normalizer (chunk 053)
- 🔲 Canonical Data Model (chunk 053 — document)

**Messaging Endpoints:**
- 🔲 Messaging Gateway (chunk 054)
- 🔲 Messaging Mapper (chunk 054)
- 🔲 Transactional Client (chunk 055)
- 🔲 Polling Consumer (chunk 056)
- 🔲 Event-Driven Consumer (chunk 056)
- ✅ Competing Consumers (Processing.CompetingConsumers)
- 🔲 Message Dispatcher (chunk 057)
- 🔲 Selective Consumer (chunk 056)
- 🔲 Durable Subscriber (chunk 056)
- ✅ Idempotent Receiver (Storage.Cassandra dedup)
- 🔲 Service Activator (chunk 057)

**System Management:**
- 🔲 Control Bus (chunk 058)
- 🔲 Detour (chunk 058)
- ✅ Wire Tap (OpenTelemetry / Observability)
- 🔲 Message History (chunk 058)
- 🔲 Message Store (chunk 058 — formalize)
- 🔲 Smart Proxy (chunk 058)
- 🔲 Test Message (chunk 058)
- 🔲 Channel Purger (chunk 058)

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
