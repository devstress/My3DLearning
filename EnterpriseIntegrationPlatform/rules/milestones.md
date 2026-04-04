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

All phases complete. See `rules/completion-log.md` for full history.

---

### Phase 10 – Connectors & Test Coverage Hardening

✅ Phase 10 complete — see completion-log.md

### Phase 11 – Admin Dashboard & RAG

✅ Phase 11 complete — see completion-log.md

### Phase 12 – Documentation

✅ Phase 12 complete — see completion-log.md

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

## Tutorial Audit (2026-04-04)

> Full audit of all 50 tutorials against the actual codebase.  
> Build succeeds. All 1,538 .NET tests pass (1,400 Unit + 58 Contract + 29 Workflow + 24 Playwright + 17 Integration + 10 Load).  
> Tutorials 49/50 claim "1,181+ unit tests" — actual count is **1,400**.

### README Discrepancy

| Issue | Severity |
|-------|----------|
| **tutorials/README.md** lists Tutorial 48 as "[Migrating from BizTalk](48-migrating-from-biztalk.md)" but the actual file is `48-notification-use-cases.md` (about notification use cases). `48-migrating-from-biztalk.md` does not exist. | 🔴 ERROR |

### Tutorial 03 — Your First Message

| Issue | Severity |
|-------|----------|
| `PublishAsync` parameter order shown as `(topic, envelope)` but actual signature is `(envelope, topic)` in `IMessageBrokerProducer`. Code will not compile. | 🔴 ERROR |

### Tutorial 04 — The Integration Envelope

| Issue | Severity |
|-------|----------|
| `SchemaVersion` field exists in `IntegrationEnvelope` but is not documented in the tutorial. | 🟡 WARNING |
| `Intent` property shown as non-nullable but is actually `MessageIntent?` (nullable). | 🟡 WARNING |

### Tutorial 06 — Messaging Channels

| Issue | Severity |
|-------|----------|
| `IPointToPointChannel.ReceiveAsync` missing required `channel` and `consumerGroup` parameters. Code will not compile. | 🔴 ERROR |
| `IDatatypeChannel` methods differ: tutorial shows `RouteAsync()`/`RegisterHandlerAsync()`, actual has `PublishAsync<T>()`/`ResolveChannel()`. | 🟡 WARNING |
| `IMessagingBridge.StartAsync` missing `sourceChannel` and `targetChannel` parameters. | 🟡 WARNING |

### Tutorial 08 — Activities and the Pipeline

| Issue | Severity |
|-------|----------|
| `IPersistenceActivityService.SaveMessageAsync` — completely wrong signature. Actual takes `IntegrationPipelineInput`, not `IntegrationEnvelope<T>` + `DeliveryStatus`. | 🔴 ERROR |
| `IMessageValidationService.ValidateAsync` — completely wrong. Returns `MessageValidationResult` (not `ValidationResult`), takes `(string messageType, string payloadJson)` not `IntegrationEnvelope<T>`. | 🔴 ERROR |
| `INotificationActivityService.PublishAckAsync` — takes `(Guid messageId, Guid correlationId, string topic)`, not `IntegrationEnvelope<T>`. | 🔴 ERROR |
| `INotificationActivityService.PublishNackAsync` — takes `(Guid messageId, Guid correlationId, string reason, string topic)`, not `(IntegrationEnvelope<T>, IReadOnlyList<string>)`. | 🔴 ERROR |
| `ICompensationActivityService` — method is `CompensateAsync(Guid, string)` returning `Task<bool>`, not `ExecuteCompensationAsync(string, IntegrationPipelineInput)`. | 🔴 ERROR |

### Tutorial 10 — Message Filter

| Issue | Severity |
|-------|----------|
| `RuleCondition` referenced at `src/Processing.Routing/RuleCondition.cs` but actually located at `src/RuleEngine/RuleCondition.cs`. | 🟡 WARNING |

### Tutorial 13 — Routing Slip

| Issue | Severity |
|-------|----------|
| `RoutingSlip.Advance()` shown as one-liner lambda but actual code includes `InvalidOperationException` guard for completed slips. | 🟡 WARNING |

### Tutorial 14 — Process Manager

| Issue | Severity |
|-------|----------|
| `SagaCompensationActivities` code snippet omits post-compensation success/failure logging that exists in actual code. | 🟡 WARNING |

### Tutorial 26 — Message Replay

| Issue | Severity |
|-------|----------|
| `ReplayFilter` uses `From`/`To` properties but actual has `FromTimestamp`/`ToTimestamp`. | 🔴 ERROR |
| `ReplayFilter.CorrelationId` shown as `string?` but actual is `Guid?`. | 🔴 ERROR |
| `IMessageReplayStore.QueryAsync` does not exist. Actual method is `GetMessagesForReplayAsync(topic, filter, maxMessages, ct)` returning `IAsyncEnumerable`. | 🔴 ERROR |
| `IMessageReplayStore.StoreAsync` signature wrong. Actual is `StoreForReplayAsync<T>(envelope, topic, ct)`. | 🔴 ERROR |
| `ReplayResult` missing `SkippedCount` and `FailedCount` properties. | 🟡 WARNING |
| `InMemoryMessageReplayStore` class mentioned but does not exist in codebase. | 🟡 WARNING |

### Tutorial 27 — Resequencer

| Issue | Severity |
|-------|----------|
| `IResequencer.SubmitAsync` does not exist. Actual method is `Accept<T>(envelope)` — synchronous, not async. | 🔴 ERROR |
| `GapPolicy` enum (`WaitForTimeout`, `ReleasePartial`, `DeadLetter`) does not exist. | 🔴 ERROR |
| `ResequencerOptions` properties wrong: `MaxBufferSize`→`MaxConcurrentSequences`, `SequenceTimeout`→`ReleaseTimeout`, no `GapPolicy`. | 🔴 ERROR |
| Default timeout shown as 5 minutes but actual is 30 seconds. Default buffer shown as 1,000 but actual is 10,000. | 🔴 ERROR |

### Tutorial 28 — Competing Consumers

| Issue | Severity |
|-------|----------|
| `IConsumerLagMonitor.GetCurrentLagAsync` — actual is `GetLagAsync(topic, consumerGroup, ct)` returning `ConsumerLagInfo`. | 🔴 ERROR |
| `IConsumerLagMonitor.GetLagByPartitionAsync` does not exist. | 🔴 ERROR |
| `IConsumerScaler` shows `ScaleUpAsync`/`ScaleDownAsync` but actual has single `ScaleAsync(desiredCount, ct)`. | 🔴 ERROR |
| `IBackpressureSignal.IsActive` → actual is `IsBackpressured`. `Activate`/`Deactivate` → actual is `Signal`/`Release`. | 🔴 ERROR |
| `CompetingConsumerOptions.EvaluationInterval` does not exist. `CooldownPeriod` is actually `CooldownMs` (int milliseconds, default 30s not 2min). | 🔴 ERROR |

### Tutorial 29 — Throttle & Rate Limiting

| Issue | Severity |
|-------|----------|
| `IMessageThrottle.AcquireAsync` takes `IntegrationEnvelope<T>` not `string partitionKey`. Returns `ThrottleResult` not `ThrottleDecision`. | 🔴 ERROR |
| `ThrottleDecision` class does not exist — actual is `ThrottleResult`. | 🔴 ERROR |
| `IMessageThrottle.GetMetrics` takes no parameters, not `string partitionKey`. | 🔴 ERROR |
| `ThrottlePartitionStrategy` enum does not exist. Partitioning uses `ThrottlePartitionKey` record with `TenantId`, `Queue`, `Endpoint`. | 🔴 ERROR |
| `IThrottleRegistry` methods differ significantly from tutorial. | 🔴 ERROR |
| `ThrottleMetrics` properties differ: no `PartitionKey`, `TotalThrottled`, `AverageWaitTime`; actual has `TotalAcquired`, `TotalRejected`, `BurstCapacity`, `RefillRate`, `TotalWaitTime`. | 🔴 ERROR |

### Tutorial 30 — Rule Engine

| Issue | Severity |
|-------|----------|
| `ConditionGroup` class does not exist. `BusinessRule` directly has `LogicOperator` and `Conditions`. | 🔴 ERROR |
| Enum is `RuleLogicOperator`, not `LogicalOperator`. | 🔴 ERROR |
| `BusinessRule` properties wrong: no `Id`, uses `Enabled` not `IsEnabled`, has `StopOnMatch`, uses `LogicOperator` not separate `ConditionGroup`. | 🔴 ERROR |
| `RuleCondition` uses `FieldName` not `Field`. | 🟡 WARNING |
| `RuleConditionOperator` enum missing values: no `NotEquals`, `StartsWith`, `EndsWith`, `LessThan`, `Exists`. Has `In` instead. | 🔴 ERROR |
| `RuleAction` uses named properties (`ActionType`, `TargetTopic`, `TransformName`, `Reason`), not generic `Parameters` dict. | 🔴 ERROR |
| `RuleActionType` enum: missing `Enrich`, `Notify`, `Store`; has `DeadLetter` instead. | 🔴 ERROR |
| `IRuleStore` methods differ: no `GetActiveRulesAsync`, has `GetAllAsync`, `GetByNameAsync`, `AddOrUpdateAsync`, `CountAsync`. | 🔴 ERROR |
| `RuleEvaluationResult` returns collections (`MatchedRules`, `Actions`) not singles (`MatchedRule`, `SelectedAction`). | 🔴 ERROR |

### Tutorial 31 — Event Sourcing

| Issue | Severity |
|-------|----------|
| `IEventStore.AppendAsync` returns `Task<long>`, not `Task`. | 🔴 ERROR |
| `IEventStore.ReadStreamAsync` missing `count` parameter. | 🔴 ERROR |
| `IEventStore.QueryAsync(TemporalQuery)` does not exist. `TemporalQuery` is a static helper class, not a query object. | 🔴 ERROR |
| `EventEnvelope` property is `Data`, not `Payload`. | 🔴 ERROR |
| `ISnapshotStore` is generic `ISnapshotStore<TState>`, saves typed state not string. | 🔴 ERROR |
| `IEventProjection` is generic `IEventProjection<TState>`, takes and returns state. No `ProjectionName` property. | 🔴 ERROR |
| `TemporalQuery` is a static class with helper methods, not a record. | 🔴 ERROR |

### Tutorial 32 — Multi-Tenancy

| Issue | Severity |
|-------|----------|
| `ITenantResolver` methods are synchronous `Resolve()`, not async. Takes `IReadOnlyDictionary<string, string>` or `string?`, not `IntegrationEnvelope`. | 🔴 ERROR |
| `TenantContext.TenantName` is nullable, not required. Property is `IsResolved` not `IsAnonymous`. | 🔴 ERROR |
| `ITenantIsolationGuard` has single `Enforce<T>(envelope, expectedTenantId)`, not `Validate`/`ValidateEnvelope`. | 🔴 ERROR |
| `TenantIsolationException` properties are `MessageId`, `ActualTenantId`, `ExpectedTenantId` — not `SourceTenantId`, `TargetTenantId`, `Operation`. | 🔴 ERROR |

### Tutorial 33 — Security

| Issue | Severity |
|-------|----------|
| `IInputSanitizer` methods are synchronous `Sanitize(string)`/`IsClean(string)`, not async `SanitizeAsync` returning `SanitizationResult`. | 🔴 ERROR |
| `IPayloadSizeGuard` method is `Enforce(string)`/`Enforce(byte[])`, not `Validate(IntegrationEnvelope)`. | 🔴 ERROR |
| `JwtOptions` uses `set` accessors with empty string defaults, not `init`/`required`. Has `ClockSkew` instead of `TokenLifetime`. | 🔴 ERROR |
| `ISecretProvider` returns `SecretEntry?` objects with version/metadata support, not raw strings. Additional `DeleteSecretAsync`/`ListSecretKeysAsync` methods. | 🔴 ERROR |
| `HashiCorpVaultSecretProvider` is actually named `VaultSecretProvider`. | 🟡 WARNING |

### Tutorial 34 — HTTP Connector

| Issue | Severity |
|-------|----------|
| `IHttpConnector` methods are generic `SendAsync<TPayload, TResponse>` with URL/method params, not `SendAsync(envelope, options)`. | 🔴 ERROR |
| `HttpConnectorOptions` completely different: uses `BaseUrl` (string), `TimeoutSeconds` (int), retry/cache settings. No `AuthenticationMode` enum. | 🔴 ERROR |
| `ConnectorResult` has no `HttpStatusCode` or `Duration`. Has `ConnectorName`, `StatusMessage`, `CompletedAt` instead. | 🔴 ERROR |

### Tutorial 35 — SFTP Connector

| Issue | Severity |
|-------|----------|
| `ISftpConnector` methods completely different: `UploadAsync` returns path string, `DownloadAsync` returns bytes, `ListFilesAsync` returns strings. | 🔴 ERROR |
| `RemoteFileInfo` record does not exist in codebase. | 🔴 ERROR |
| `SftpConnectorOptions` much simpler: no SSH key auth, no atomic rename option, no sidecar metadata, no file template. Uses `RootPath` not `RemoteDirectory`. | 🔴 ERROR |

### Tutorial 36 — Email Connector

| Issue | Severity |
|-------|----------|
| `IEmailConnector.SendAsync` is generic, returns `Task` (not `ConnectorResult`), takes individual params not options object. | 🔴 ERROR |
| `EmailConnectorOptions` only has basic SMTP config. No To/Cc/Bcc, no body template, no HTML flag, no attachments. | 🔴 ERROR |
| `EmailAttachment` record does not exist. | 🔴 ERROR |
| Liquid template rendering described but not implemented — uses `Func<T, string>` body builders instead. | 🔴 ERROR |

### Tutorial 37 — File Connector

| Issue | Severity |
|-------|----------|
| `IFileConnector` methods differ: `WriteAsync` returns path, `ReadAsync` returns bytes, `ListFilesAsync` returns strings. | 🔴 ERROR |
| `LocalFileInfo` record does not exist. | 🔴 ERROR |
| `FileConnectorOptions` uses string encoding, no atomic write flag, no sidecar metadata. Namespace is `Connector.FileSystem`, not `Connector.File`. | 🔴 ERROR |

### Tutorial 38 — OpenTelemetry

| Issue | Severity |
|-------|----------|
| `PlatformActivitySource` doesn't directly expose `ActivitySource`. It's in `DiagnosticsConfig`. Has generic overload for envelopes. | 🔴 ERROR |
| `PlatformMeters` missing `MessagesDeadLettered`, `MessagesRetried`, `MessagesInFlight` counters. Has `MessagesProcessed` not `MessagesDelivered`. | 🔴 ERROR |
| `CorrelationPropagator` methods differ: `InjectTraceContext<T>` returns envelope, `ExtractAndStart<T>` returns Activity. | 🔴 ERROR |

### Tutorial 39 — Message Lifecycle

| Issue | Severity |
|-------|----------|
| `MessageEvent` IDs are `Guid` not `string`. Uses `Stage`/`Status`/`Source` not `State`/`Component`. Has additional `EventId`, `TraceId`, `SpanId`. | 🔴 ERROR |
| `DeliveryStatus` enum values are `Pending, InFlight, Delivered, Failed, Retrying, DeadLettered` — not `Received, Routed, Transformed, Acked, Nacked`. | 🔴 ERROR |
| `IMessageStateStore` methods take `Guid` not `string` for message/correlation IDs. Has additional `GetLatestByCorrelationIdAsync`. | 🔴 ERROR |
| `ITraceAnalyzer` methods differ: returns AI-generated strings, not structured `TraceAnalysis` record. | 🔴 ERROR |
| `IObservabilityEventLog` method is `RecordAsync` not `WriteAsync`. No generic `QueryAsync` with date range. | 🔴 ERROR |

### Tutorial 40 — RAG with Ollama

| Issue | Severity |
|-------|----------|
| `IOllamaEmbeddingProvider` does not exist. Actual is `IOllamaService` with `GenerateAsync`/`AnalyseAsync`/`IsHealthyAsync`. | 🔴 ERROR |
| `OllamaOptions` → actual class is `OllamaSettings` with only a `Model` property. | 🔴 ERROR |
| `IRagPipeline` does not exist. Actual is `IRagFlowService` with `RetrieveAsync`/`ChatAsync`/`ListDatasetsAsync`/`IsHealthyAsync`. | 🔴 ERROR |
| `RagResponse` → actual is `RagFlowChatResponse(Answer, ConversationId, References)`. No `Confidence` field. | 🔴 ERROR |
| `RagOptions` → actual is `RagFlowOptions` for connection config (BaseAddress, ApiKey, AssistantId), not query options. | 🔴 ERROR |
| `IGenerationProvider` interface does not exist in codebase. | 🔴 ERROR |

### Tutorial 41 — OpenClaw Web UI

| Issue | Severity |
|-------|----------|
| `IMessageSearchService`, `IMessageInspector`, `IRagChatService` do not exist. OpenClaw.Web only has `DemoDataSeeder.cs` and `Program.cs`. | 🔴 ERROR |

### Tutorial 42 — Configuration

| Issue | Severity |
|-------|----------|
| `IConfigurationStore` methods differ: `GetAsync` requires `environment`, `SetAsync` takes `ConfigurationEntry` not key+value, no `GetAllAsync`/`GetHistoryAsync`. Has `DeleteAsync`/`WatchAsync`. | 🔴 ERROR |
| `IFeatureFlagService` methods differ: param names, `GetAllFlagsAsync` → `ListAsync`, missing `GetAsync`/`SetAsync`/`DeleteAsync`. | 🔴 ERROR |
| `ConfigurationChangeNotifier` uses `IObservable<T>` pattern with `Publish()`, not events with `NotifyAsync()`. `ConfigurationChange` record has `Environment`, `ChangeType`, `Timestamp`. | 🔴 ERROR |
| `FeatureFlag` is a record not class, `Variants` is `Dictionary<string,string>` not `IReadOnlyList<string>`, `RolloutPercentage` is `int` not `double`. | 🟡 WARNING |
| `NotificationFeatureFlags` has only `NotificationsEnabled` constant, not separate `AckNotifications`/`NackNotifications`. | 🟡 WARNING |

### Tutorial 45 — Performance Profiling

| Issue | Severity |
|-------|----------|
| `CpuProfiler` does not exist. Actual is `ContinuousProfiler` with synchronous `CaptureSnapshot()`. | 🔴 ERROR |
| `MemoryProfiler` does not exist. Memory profiling is in `GcMonitor` returning `GcSnapshot`. | 🔴 ERROR |

### Tutorial 48 — Notification Use Cases

| Issue | Severity |
|-------|----------|
| `INotificationMapper` takes `(Guid messageId, Guid correlationId)`, not `IntegrationEnvelope`. `MapNack` has 3 params. Uses `SecurityElement.Escape()`. | 🔴 ERROR |

### Tutorial 49 — Testing Integrations

| Issue | Severity |
|-------|----------|
| Claims "1,181+ unit tests" but actual count is **1,400** unit tests (1,538 total .NET tests). | 🟡 WARNING |

### Tutorial 50 — Best Practices

| Issue | Severity |
|-------|----------|
| Inherits incorrect "1,181+ unit tests" claim from Tutorial 49. | 🟡 WARNING |

### Tutorials Passing Audit (No Issues Found)

✅ Tutorial 01 — Introduction  
✅ Tutorial 02 — Environment Setup  
✅ Tutorial 05 — Message Brokers  
✅ Tutorial 07 — Temporal Workflows (minor simplification)  
✅ Tutorial 09 — Content-Based Router  
✅ Tutorial 11 — Dynamic Router  
✅ Tutorial 12 — Recipient List  
✅ Tutorial 15 — Message Translator  
✅ Tutorial 16 — Transform Pipeline  
✅ Tutorial 17 — Normalizer  
✅ Tutorial 18 — Content Enricher  
✅ Tutorial 19 — Content Filter  
✅ Tutorial 20 — Splitter  
✅ Tutorial 21 — Aggregator  
✅ Tutorial 22 — Scatter-Gather  
✅ Tutorial 23 — Request-Reply  
✅ Tutorial 24 — Retry Framework  
✅ Tutorial 25 — Dead Letter Queue  
✅ Tutorial 43 — Kubernetes Deployment  
✅ Tutorial 44 — Disaster Recovery  
✅ Tutorial 46 — Complete Integration  
✅ Tutorial 47 — Saga Compensation  

### Summary

| Category | Count |
|----------|-------|
| 🔴 ERROR (code won't compile or API mismatch) | ~90 |
| 🟡 WARNING (incomplete or misleading) | ~15 |
| Tutorials with errors | 28 of 50 |
| Tutorials passing | 22 of 50 |

**Most affected tutorials (by error count):**
1. Tutorial 30 (Rule Engine) — 9 errors
2. Tutorial 31 (Event Sourcing) — 7 errors
3. Tutorial 40 (RAG with Ollama) — 6 errors
4. Tutorial 28 (Competing Consumers) — 5 errors
5. Tutorial 08 (Activities and the Pipeline) — 5 errors
6. Tutorial 29 (Throttle & Rate Limiting) — 6 errors
7. Tutorial 39 (Message Lifecycle) — 5 errors

**Root cause pattern:** Tutorials show idealized/designed API signatures that differ from actual implementations. Method names, parameter types, return types, enum values, and class structures frequently don't match the real code.

**Recommended next steps:**
1. Fix tutorials/README.md — correct Tutorial 48 link to `48-notification-use-cases.md` and update title
2. Update all tutorial code snippets to match actual API signatures
3. Correct test count claims to reflect actual 1,400 unit tests / 1,538 total
4. Prioritize fixing Tutorials 03, 06, 08 (beginner path) to avoid confusing new developers

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
