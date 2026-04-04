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
✅ Phase 7 (Missing EIP Patterns – Messaging Channels & Construction, chunks 044-051) complete — see completion-log.md
✅ Phase 8 (Missing EIP Patterns – Routing & Transformation, chunks 052-053) complete — see completion-log.md
✅ Phase 9 (Missing EIP Patterns – Endpoints & System Management, chunks 054-058) complete — see completion-log.md
✅ Phase 10 (Connectors & Test Coverage Hardening, chunks 059-060) complete — see completion-log.md

## Next Chunk

**Chunk 064** (Platform Usage Guide) is next.

---

### Phase 10 – Connectors & Test Coverage Hardening

✅ Phase 10 complete — see completion-log.md

### Phase 11 – Admin Dashboard & RAG

✅ Phase 11 complete — see completion-log.md

### Phase 12 – Documentation

| Chunk | Name | Goal | Tests Required |
|-------|------|------|----------------|
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
- ✅ Message Endpoint (Ingestion — formalized as PollingConsumer, EventDrivenConsumer, SelectiveConsumer, DurableSubscriber)

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
- ✅ Resequencer (Processing.Resequencer.MessageResequencer)
- ✅ Composed Message Processor (Splitter + Transform + Aggregator pipeline)
- ✅ Scatter-Gather (Processing.ScatterGather)
- ✅ Routing Slip (Processing.Routing.RoutingSlipRouter)
- ✅ Process Manager (Temporal Workflows)
- — Message Broker (the platform IS the broker)

**Message Transformation:**
- ✅ Envelope Wrapper (IntegrationEnvelope)
- ✅ Content Enricher (Processing.Transform.ContentEnricher)
- ✅ Content Filter (Processing.Transform.ContentFilter)
- ✅ Claim Check (Storage.Cassandra)
- ✅ Normalizer (Processing.Transform.MessageNormalizer)
- ✅ Canonical Data Model (IntegrationEnvelope<T> — documented)

**Messaging Endpoints:**
- ✅ Messaging Gateway (Gateway.Api — IMessagingGateway + HttpMessagingGateway)
- ✅ Messaging Mapper (Contracts — IMessagingMapper + JsonMessagingMapper)
- ✅ Transactional Client (Ingestion — ITransactionalClient + BrokerTransactionalClient)
- ✅ Polling Consumer (Ingestion — IPollingConsumer + PollingConsumer)
- ✅ Event-Driven Consumer (Ingestion — IEventDrivenConsumer + EventDrivenConsumer)
- ✅ Competing Consumers (Processing.CompetingConsumers)
- ✅ Message Dispatcher (Processing.Dispatcher.MessageDispatcher)
- ✅ Selective Consumer (Ingestion — ISelectiveConsumer + SelectiveConsumer)
- ✅ Durable Subscriber (Ingestion — IDurableSubscriber + DurableSubscriber)
- ✅ Idempotent Receiver (Storage.Cassandra dedup)
- ✅ Service Activator (Processing.Dispatcher.ServiceActivator)

**System Management:**
- ✅ Control Bus (SystemManagement.ControlBusPublisher)
- ✅ Detour (Processing.Routing.Detour)
- ✅ Wire Tap (OpenTelemetry / Observability)
- ✅ Message History (Contracts.MessageHistoryHelper)
- ✅ Message Store (SystemManagement.MessageStore)
- ✅ Smart Proxy (SystemManagement.SmartProxy)
- ✅ Test Message (SystemManagement.TestMessageGenerator)
- ✅ Channel Purger (Ingestion.ChannelPurger)

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
