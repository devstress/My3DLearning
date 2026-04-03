# Completion Log

Detailed record of completed chunks, files created/modified, and notes.

See `milestones.md` for current phase status and next chunk.

## Chunk 046 – Message Construction — Request-Reply

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: Add RequestReplyCorrelator in Processing.RequestReply/ that publishes a request envelope with ReplyTo set, subscribes to the reply topic, and correlates the response by CorrelationId with configurable timeout. Async messaging equivalent of HTTP request-response — replaces BizTalk solicit-response port.

### Architecture

- **IRequestReplyCorrelator<TRequest, TResponse>** — Interface for sending a request and awaiting a correlated reply.
- **RequestReplyCorrelator<TRequest, TResponse>** — Production implementation using ConcurrentDictionary for pending requests, TaskCompletionSource for async correlation, and linked CancellationTokenSource for timeout.
- **RequestReplyMessage<TRequest>** — Record describing the request (Payload, RequestTopic, ReplyTopic, Source, MessageType, CorrelationId?).
- **RequestReplyResult<TResponse>** — Record with CorrelationId, Reply envelope (null on timeout), TimedOut flag, Duration.
- **RequestReplyOptions** — TimeoutMs (default 30s) and ConsumerGroup configuration.
- **RequestReplyServiceExtensions** — DI registration.

### Files created

- `src/Processing.RequestReply/Processing.RequestReply.csproj`
- `src/Processing.RequestReply/IRequestReplyCorrelator.cs`
- `src/Processing.RequestReply/RequestReplyCorrelator.cs`
- `src/Processing.RequestReply/RequestReplyMessage.cs`
- `src/Processing.RequestReply/RequestReplyResult.cs`
- `src/Processing.RequestReply/RequestReplyOptions.cs`
- `src/Processing.RequestReply/RequestReplyServiceExtensions.cs`
- `tests/UnitTests/RequestReplyCorrelatorTests.cs`

### Files modified

- `tests/UnitTests/UnitTests.csproj` — Added Processing.RequestReply project reference
- `rules/milestones.md` — Removed chunk 046 row, updated Next Chunk to 047, marked Phase 7 complete, updated EIP checklist
- `rules/completion-log.md` — Added chunk 046 entry

### Test counts after chunk

| Suite | Count |
|-------|-------|
| UnitTests | 1047 |
| ContractTests | 58 |
| WorkflowTests | 24 |
| IntegrationTests | 17 |
| PlaywrightTests | 13 |
| LoadTests | 10 |
| **Total** | **1169** |

---

## Chunk 045 – Message Construction (Return Address, Message Expiration, Format Indicator, Message Sequence, Command/Document/Event Messages)

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: Add EIP Message Construction patterns — Return Address (ReplyTo field), Message Expiration (ExpiresAt field + MessageExpirationChecker routing to DLQ), Format Indicator (formalized ContentType header), Message Sequence (SequenceNumber/TotalCount first-class fields on envelope + Splitter integration), Command/Document/Event Messages (MessageIntent enum).

### Architecture

- **MessageIntent enum** — Three values: Command (0), Document (1), Event (2). Distinguishes the three fundamental EIP message types.
- **IntegrationEnvelope<T> new fields** — `ReplyTo` (string?), `ExpiresAt` (DateTimeOffset?), `SequenceNumber` (int?), `TotalCount` (int?), `Intent` (MessageIntent?). All nullable with defaults of null so existing code is unaffected. Added `IsExpired` computed property.
- **MessageHeaders new constants** — `ReplyTo` ("reply-to"), `ExpiresAt` ("expires-at"), `SequenceNumber` ("sequence-number"), `TotalCount` ("total-count"), `Intent` ("intent").
- **DeadLetterReason.MessageExpired** — New enum value for expired message routing.
- **IMessageExpirationChecker<T> / MessageExpirationChecker<T>** — Checks ExpiresAt against TimeProvider, routes expired messages to DLQ via IDeadLetterPublisher with reason MessageExpired. Uses TimeProvider for testability.
- **DeadLetterServiceExtensions.AddMessageExpirationChecker<T>()** — DI registration for the expiration checker.
- **MessageSplitter<T>** — Updated to set SequenceNumber (0-based index) and TotalCount on each split envelope. Also preserves ReplyTo, ExpiresAt, and Intent from source envelope.

### Files created

- `src/Contracts/MessageIntent.cs`
- `src/Processing.DeadLetter/IMessageExpirationChecker.cs`
- `src/Processing.DeadLetter/MessageExpirationChecker.cs`
- `tests/ContractTests/IntegrationEnvelopeMessageConstructionTests.cs`
- `tests/ContractTests/MessageHeadersNewFieldsTests.cs`
- `tests/ContractTests/MessageIntentTests.cs`
- `tests/UnitTests/MessageExpirationCheckerTests.cs`
- `tests/UnitTests/MessageSplitterSequenceTests.cs`

### Files modified

- `src/Contracts/IntegrationEnvelope.cs` — Added ReplyTo, ExpiresAt, SequenceNumber, TotalCount, Intent fields + IsExpired property
- `src/Contracts/MessageHeaders.cs` — Added ReplyTo, ExpiresAt, SequenceNumber, TotalCount, Intent constants
- `src/Processing.DeadLetter/DeadLetterReason.cs` — Added MessageExpired value
- `src/Processing.DeadLetter/DeadLetterServiceExtensions.cs` — Added AddMessageExpirationChecker<T>()
- `src/Processing.Splitter/MessageSplitter.cs` — Set SequenceNumber, TotalCount, ReplyTo, ExpiresAt, Intent on split envelopes
- `tests/ContractTests/MessageHeadersTests.cs` — Updated AllConstantsAreNonEmpty to include new constants
- `rules/milestones.md` — Removed chunk 045 row, updated Next Chunk to 046, updated EIP checklist
- `rules/completion-log.md` — Added chunk 045 entry

### Test counts after chunk

| Suite | Count |
|-------|-------|
| UnitTests | 1035 |
| ContractTests | 58 |
| WorkflowTests | 24 |
| IntegrationTests | 17 |
| PlaywrightTests | 13 |
| LoadTests | 10 |
| **Total** | **1157** |

---

## Chunk 044 – Messaging Channels (Point-to-Point, Pub-Sub, Datatype, Invalid Message, Bridge, Message Bus)

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: Add EIP Messaging Channel patterns — PointToPointChannel (queue-group semantics), PublishSubscribeChannel (fan-out), DatatypeChannel (auto-resolve topic from MessageType), InvalidMessageChannel (malformed input routing distinct from DLQ), MessagingBridge (cross-broker forwarding with dedup), Message Bus (documented as platform architecture).

### Architecture

- **IPointToPointChannel / PointToPointChannel** — Wraps IMessageBrokerProducer/Consumer to enforce queue-group semantics: each message delivered to exactly one consumer in the group.
- **IPublishSubscribeChannel / PublishSubscribeChannel** — Wraps broker with fan-out delivery: each subscriber gets a unique consumer group so every subscriber receives every message.
- **IDatatypeChannel / DatatypeChannel** — Auto-resolves topic from IntegrationEnvelope.MessageType using configurable prefix and separator. Each message type flows on its own dedicated channel.
- **DatatypeChannelOptions** — TopicPrefix and Separator configuration.
- **IInvalidMessageChannel / InvalidMessageChannel** — Routes unparseable/invalid-schema messages to a dedicated invalid-message topic. Distinct from DLQ (processing failures). Supports both envelope-based and raw-data routing.
- **InvalidMessageEnvelope** — Record carrying OriginalMessageId, RawData, SourceTopic, Reason, RejectedAt.
- **InvalidMessageChannelOptions** — InvalidMessageTopic and Source configuration.
- **IMessagingBridge / MessagingBridge** — Forwards messages between two broker instances with envelope preservation and sliding-window deduplication by MessageId. Thread-safe via ConcurrentDictionary + ConcurrentQueue.
- **MessagingBridgeOptions** — ConsumerGroup and DeduplicationWindowSize configuration.
- **ChannelServiceExtensions** — DI registration for all channel services.
- **Message Bus** — Documented as the architectural pattern the platform itself implements (the entire EIP platform IS the message bus).

### Files created

- `src/Ingestion/Channels/IPointToPointChannel.cs`
- `src/Ingestion/Channels/PointToPointChannel.cs`
- `src/Ingestion/Channels/IPublishSubscribeChannel.cs`
- `src/Ingestion/Channels/PublishSubscribeChannel.cs`
- `src/Ingestion/Channels/IDatatypeChannel.cs`
- `src/Ingestion/Channels/DatatypeChannel.cs`
- `src/Ingestion/Channels/DatatypeChannelOptions.cs`
- `src/Ingestion/Channels/IInvalidMessageChannel.cs`
- `src/Ingestion/Channels/InvalidMessageChannel.cs`
- `src/Ingestion/Channels/InvalidMessageEnvelope.cs`
- `src/Ingestion/Channels/InvalidMessageChannelOptions.cs`
- `src/Ingestion/Channels/IMessagingBridge.cs`
- `src/Ingestion/Channels/MessagingBridge.cs`
- `src/Ingestion/Channels/MessagingBridgeOptions.cs`
- `src/Ingestion/Channels/ChannelServiceExtensions.cs`
- `tests/UnitTests/PointToPointChannelTests.cs`
- `tests/UnitTests/PublishSubscribeChannelTests.cs`
- `tests/UnitTests/DatatypeChannelTests.cs`
- `tests/UnitTests/InvalidMessageChannelTests.cs`
- `tests/UnitTests/MessagingBridgeTests.cs`

### Files modified

- `rules/milestones.md` — Removed chunk 044 row, updated Next Chunk to 045, updated EIP checklist
- `rules/completion-log.md` — Added chunk 044 entry

### Test counts after chunk

| Suite | Count |
|-------|-------|
| UnitTests | 1018 |
| ContractTests | 29 |
| WorkflowTests | 24 |
| IntegrationTests | 17 |
| PlaywrightTests | 13 |
| LoadTests | 10 |
| **Total** | **1111** |

---

## Chunk 043 – Stateful Pipeline Workflow (Temporal All-or-Nothing)

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: Move ALL pipeline orchestration logic inside Temporal workflows for true BizTalk-replacement atomicity. Previously PipelineOrchestrator did persist/validate/ack/nack OUTSIDE Temporal — not atomic, not recoverable. Fix: new IntegrationPipelineWorkflow with Temporal activities for every side-effect. Demo.Pipeline becomes a thin NATS→Temporal dispatcher. All-or-nothing: if any step fails, Temporal retries or compensates — no partial state.

### Architecture

- **IntegrationPipelineInput** — Record carrying full message data (MessageId, CorrelationId, CausationId, Timestamp, Source, MessageType, SchemaVersion, Priority, PayloadJson, MetadataJson, AckSubject, NackSubject) so that every step executes as a Temporal activity.
- **IntegrationPipelineResult** — Record (MessageId, IsSuccess, FailureReason?) returned by the workflow.
- **IPersistenceActivityService** — Interface for persist message, update delivery status, save fault envelope.
- **INotificationActivityService** — Interface for publish Ack/Nack to message broker.
- **CassandraPersistenceActivityService** — Implementation backed by IMessageRepository (Cassandra). Maps IntegrationPipelineInput to MessageRecord, creates FaultEnvelope, parses DeliveryStatus enum.
- **NatsNotificationActivityService** — Implementation backed by IMessageBrokerProducer (NATS JetStream). Creates IntegrationEnvelope<AckPayload/NackPayload> with correct correlation/causation IDs.
- **PipelineActivities** — Temporal activity class wrapping IPersistenceActivityService + INotificationActivityService + IMessageLoggingService. Activities: PersistMessageAsync, UpdateDeliveryStatusAsync, SaveFaultAsync, PublishAckAsync, PublishNackAsync, LogStageAsync.
- **IntegrationPipelineWorkflow** — Temporal workflow orchestrating the full pipeline atomically: (1) Persist as Pending → (2) Log Received → (3) Validate → (4a) Success: Log Validated → Update Delivered → Publish Ack → (4b) Failure: Log ValidationFailed → Save Fault → Update Failed → Publish Nack. Retry policies: 5 attempts with exponential backoff for infrastructure activities, 3 attempts for validation.
- **PipelineOrchestrator** — Simplified to thin dispatcher: converts IntegrationEnvelope<JsonElement> → IntegrationPipelineInput → dispatches to Temporal. No side-effects outside Temporal.
- **TemporalWorkflowDispatcher** — Updated to dispatch IntegrationPipelineWorkflow instead of ProcessIntegrationMessageWorkflow.
- **TemporalServiceExtensions** — Registers IntegrationPipelineWorkflow, PipelineActivities, CassandraPersistenceActivityService, NatsNotificationActivityService.
- **Workflow.Temporal/Program.cs** — Registers Cassandra storage, NATS JetStream broker, platform observability.

### Files created

- `src/Activities/IntegrationPipelineInput.cs`
- `src/Activities/IntegrationPipelineResult.cs`
- `src/Activities/IPersistenceActivityService.cs`
- `src/Activities/INotificationActivityService.cs`
- `src/Workflow.Temporal/Activities/PipelineActivities.cs`
- `src/Workflow.Temporal/Services/CassandraPersistenceActivityService.cs`
- `src/Workflow.Temporal/Services/NatsNotificationActivityService.cs`
- `src/Workflow.Temporal/Workflows/IntegrationPipelineWorkflow.cs`
- `tests/UnitTests/PipelineActivitiesTests.cs`
- `tests/UnitTests/CassandraPersistenceActivityServiceTests.cs`
- `tests/UnitTests/NatsNotificationActivityServiceTests.cs`
- `tests/UnitTests/IntegrationPipelineInputResultTests.cs`

### Files modified

- `src/Workflow.Temporal/Workflow.Temporal.csproj` — Added Storage.Cassandra, Ingestion, Ingestion.Nats, Observability refs
- `src/Workflow.Temporal/TemporalServiceExtensions.cs` — Registered new workflow, activities, services
- `src/Workflow.Temporal/Program.cs` — Added infrastructure registrations
- `src/Demo.Pipeline/PipelineOrchestrator.cs` — Simplified to thin Temporal dispatcher
- `src/Demo.Pipeline/ITemporalWorkflowDispatcher.cs` — Changed to use IntegrationPipelineInput/Result
- `src/Demo.Pipeline/TemporalWorkflowDispatcher.cs` — Dispatches IntegrationPipelineWorkflow
- `src/Demo.Pipeline/PipelineServiceExtensions.cs` — Removed Cassandra/Observability registrations
- `tests/UnitTests/PipelineOrchestratorTests.cs` — Rewritten for new thin constructor
- `tests/UnitTests/UnitTests.csproj` — Added Activities and Workflow.Temporal refs
- `rules/architecture-rules.md` — Updated Workflow.Temporal dependency rules
- `rules/milestones.md` — Chunk 043 redefined, downstream chunks renumbered

### Test counts after chunk

| Suite | Count |
|-------|-------|
| UnitTests | 969 |
| ContractTests | 29 |
| WorkflowTests | 24 |
| IntegrationTests | 17 |
| PlaywrightTests | 13 |
| LoadTests | 10 |
| **Total** | **1062** |

---

## Chunk 042 – RuleEngine

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: Business rule evaluation engine — conditions (Equals, Contains, Regex, In, GreaterThan) with AND/OR logic, priority-sorted, per-message actions (Route, Transform, Reject, DeadLetter).

### Architecture

- **RuleConditionOperator** — Enum: Equals, Contains, Regex, In, GreaterThan.
- **RuleCondition** — Record: FieldName + Operator + Value. Supports MessageType, Source, Priority, Metadata.{key}, Payload.{path} fields.
- **RuleLogicOperator** — Enum: And (all conditions must match), Or (any condition must match).
- **RuleActionType** — Enum: Route, Transform, Reject, DeadLetter.
- **RuleAction** — Record: ActionType + TargetTopic + TransformName + Reason.
- **BusinessRule** — Record: Name + Priority + LogicOperator + Conditions + Action + StopOnMatch + Enabled.
- **RuleEvaluationResult** — Record: MatchedRules + Actions + HasMatch + RulesEvaluated.
- **IRuleEngine** — Interface for evaluating rules against IntegrationEnvelope<T>.
- **BusinessRuleEngine** — Production implementation. Priority-sorted evaluation. AND/OR logic. Disabled rule skip. MaxRulesPerEvaluation guard. Regex timeout protection against catastrophic backtracking. Structured logging.
- **RuleEngineOptions** — Configuration (Enabled, MaxRulesPerEvaluation, Rules seed list, RegexTimeout). Bind from RuleEngine section.
- **IRuleStore** — Interface for CRUD on business rules. GetAll (sorted), GetByName, AddOrUpdate, Remove, Count.
- **InMemoryRuleStore** — Thread-safe ConcurrentDictionary implementation. Case-insensitive name lookup.
- **RuleEngineServiceExtensions** — DI registration: AddRuleEngine (store + engine + config seed), AddRuleStore<T> (custom store replacement).

### Files created

- `src/RuleEngine/RuleEngine.csproj`
- `src/RuleEngine/RuleConditionOperator.cs`
- `src/RuleEngine/RuleCondition.cs`
- `src/RuleEngine/RuleLogicOperator.cs`
- `src/RuleEngine/RuleActionType.cs`
- `src/RuleEngine/RuleAction.cs`
- `src/RuleEngine/BusinessRule.cs`
- `src/RuleEngine/RuleEvaluationResult.cs`
- `src/RuleEngine/IRuleEngine.cs`
- `src/RuleEngine/IRuleStore.cs`
- `src/RuleEngine/InMemoryRuleStore.cs`
- `src/RuleEngine/BusinessRuleEngine.cs`
- `src/RuleEngine/RuleEngineOptions.cs`
- `src/RuleEngine/RuleEngineServiceExtensions.cs`
- `tests/UnitTests/BusinessRuleEngineTests.cs`
- `tests/UnitTests/InMemoryRuleStoreTests.cs`
- `tests/UnitTests/RuleEngineOptionsTests.cs`

### Test counts after chunk

| Suite | Count |
|-------|-------|
| UnitTests | 944 |
| ContractTests | 29 |
| WorkflowTests | 24 |
| IntegrationTests | 17 |
| PlaywrightTests | 13 |
| LoadTests | 10 |
| **Total** | **1037** |

---

## Chunk 041 – Processing.Transform

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: General payload transformation pipeline with pluggable steps (JSON↔XML, regex replace, JSONPath filter), complementing Processing.Translator field mapping.

### Architecture

- **ITransformStep** — Interface for a single pipeline step. Each step has a Name and ExecuteAsync method that transforms a TransformContext.
- **TransformContext** — Carries payload string + content type + mutable metadata through the pipeline. Immutable payload/contentType with WithPayload factory methods.
- **TransformResult** — Record containing transformed payload, content type, steps applied count, and accumulated metadata.
- **ITransformPipeline** — Interface for executing ordered transform steps against a payload.
- **TransformPipeline** — Production implementation. Executes steps in registration order. Supports Enabled toggle, MaxPayloadSizeBytes guard, StopOnStepFailure (halt vs skip), cancellation propagation, and structured logging.
- **TransformOptions** — Configuration (Enabled, MaxPayloadSizeBytes, StopOnStepFailure). Bind from TransformPipeline section.
- **JsonToXmlStep** — Converts JSON to XML with configurable root element name. Handles objects, arrays, nested structures, booleans, nulls. Sanitizes invalid XML element names.
- **XmlToJsonStep** — Converts XML to JSON. Repeated sibling elements become arrays. Attributes use @prefix. Mixed content uses #text.
- **RegexReplaceStep** — Applies compiled regex replacement with configurable options and timeout (default 5s) to protect against catastrophic backtracking.
- **JsonPathFilterStep** — Extracts subset of JSON using dot-notation paths. Creates intermediate objects. Missing paths silently skipped. Preserves value types (arrays, numbers, strings).
- **TransformServiceExtensions** — DI registration: AddTransformPipeline, AddJsonToXmlStep, AddXmlToJsonStep, AddRegexReplaceStep, AddJsonPathFilterStep.

### Files created

- `src/Processing.Transform/Processing.Transform.csproj`
- `src/Processing.Transform/ITransformStep.cs`
- `src/Processing.Transform/ITransformPipeline.cs`
- `src/Processing.Transform/TransformContext.cs`
- `src/Processing.Transform/TransformResult.cs`
- `src/Processing.Transform/TransformOptions.cs`
- `src/Processing.Transform/TransformPipeline.cs`
- `src/Processing.Transform/JsonToXmlStep.cs`
- `src/Processing.Transform/XmlToJsonStep.cs`
- `src/Processing.Transform/RegexReplaceStep.cs`
- `src/Processing.Transform/JsonPathFilterStep.cs`
- `src/Processing.Transform/TransformServiceExtensions.cs`
- `tests/UnitTests/TransformPipelineTests.cs`
- `tests/UnitTests/JsonToXmlStepTests.cs`
- `tests/UnitTests/XmlToJsonStepTests.cs`
- `tests/UnitTests/RegexReplaceStepTests.cs`
- `tests/UnitTests/JsonPathFilterStepTests.cs`
- `tests/UnitTests/TransformOptionsTests.cs`
- `tests/UnitTests/TransformContextTests.cs`

### Files modified

- `EnterpriseIntegrationPlatform.sln` — added Processing.Transform project
- `tests/UnitTests/UnitTests.csproj` — added Processing.Transform reference
- `rules/milestones.md` — removed chunk 041 row, updated Next Chunk to 042
- `rules/completion-log.md` — added chunk 041 entry

### Test counts

- **New tests**: 64 (TransformPipeline 18, JsonToXmlStep 9, XmlToJsonStep 8, RegexReplaceStep 10, JsonPathFilterStep 11, TransformOptions 6, TransformContext 6)
- **Total UnitTests**: 890 (826 + 64)
- **All test projects**: UnitTests 890, ContractTests 29, WorkflowTests 24, IntegrationTests 17, PlaywrightTests 13, LoadTests 10 = **983 total**

## Chunk 040 – Performance Profiling

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: Continuous profiling integration, memory/CPU hotspot detection, GC tuning, and benchmark regression tests.

### Architecture

- **IContinuousProfiler** — Interface for capturing periodic CPU, memory, and GC snapshots. Computes delta metrics (CPU%, allocation rate) from previous snapshot.
- **ContinuousProfiler** — Production implementation using System.Diagnostics.Process, GC.GetGCMemoryInfo(), and GC.GetTotalAllocatedBytes(). Thread-safe with bounded snapshot retention and lock-based capture serialization.
- **IHotspotDetector** — Interface for registering operation metrics and detecting CPU/memory hotspots against configurable thresholds.
- **AllocationHotspotDetector** — Lock-free concurrent implementation using ConcurrentDictionary with Interlocked-based OperationAccumulator. Supports configurable max tracked operations, minimum invocation thresholds, and warning/critical severity levels.
- **IGcMonitor** — Interface for monitoring GC behavior and providing tuning recommendations.
- **GcMonitor** — Captures GC generation sizes, collection counts, fragmentation ratio, pause time, and LOH metrics. Generates tuning recommendations for ServerGC, fragmentation, Gen2 pressure, pause time, and LOH size.
- **IBenchmarkRegistry** — Interface for storing benchmark baselines and detecting regressions.
- **InMemoryBenchmarkRegistry** — Thread-safe ConcurrentDictionary-based registry with case-insensitive lookup. Compares duration and allocation metrics against configurable regression thresholds.
- **ProfilingOptions** — Configuration (SnapshotInterval, MaxRetainedSnapshots, MaxTrackedOperations, Enabled, HotspotThresholds).

### Files created

- `src/Performance.Profiling/Performance.Profiling.csproj`
- `src/Performance.Profiling/CpuSnapshot.cs`
- `src/Performance.Profiling/MemorySnapshot.cs`
- `src/Performance.Profiling/GcSnapshot.cs`
- `src/Performance.Profiling/ProfileSnapshot.cs`
- `src/Performance.Profiling/HotspotSeverity.cs`
- `src/Performance.Profiling/HotspotInfo.cs`
- `src/Performance.Profiling/HotspotThresholds.cs`
- `src/Performance.Profiling/OperationStats.cs`
- `src/Performance.Profiling/BenchmarkBaseline.cs`
- `src/Performance.Profiling/BenchmarkResult.cs`
- `src/Performance.Profiling/BenchmarkRegression.cs`
- `src/Performance.Profiling/GcTuningRecommendation.cs`
- `src/Performance.Profiling/ProfilingOptions.cs`
- `src/Performance.Profiling/IContinuousProfiler.cs`
- `src/Performance.Profiling/IHotspotDetector.cs`
- `src/Performance.Profiling/IGcMonitor.cs`
- `src/Performance.Profiling/IBenchmarkRegistry.cs`
- `src/Performance.Profiling/ContinuousProfiler.cs`
- `src/Performance.Profiling/AllocationHotspotDetector.cs`
- `src/Performance.Profiling/GcMonitor.cs`
- `src/Performance.Profiling/InMemoryBenchmarkRegistry.cs`
- `src/Performance.Profiling/ProfilingServiceExtensions.cs`
- `tests/UnitTests/ProfilingTests/ContinuousProfilerTests.cs` (20 tests)
- `tests/UnitTests/ProfilingTests/AllocationHotspotDetectorTests.cs` (24 tests)
- `tests/UnitTests/ProfilingTests/GcMonitorTests.cs` (15 tests)
- `tests/UnitTests/ProfilingTests/InMemoryBenchmarkRegistryTests.cs` (22 tests)
- `tests/LoadTests/ProfilingLoadTests.cs` (5 load tests)

### Files modified

- `src/Admin.Api/Admin.Api.csproj` — added ProjectReference to Performance.Profiling
- `src/Admin.Api/Program.cs` — added Performance.Profiling DI registration and 8 profiling admin endpoints
- `tests/UnitTests/UnitTests.csproj` — added ProjectReference to Performance.Profiling
- `tests/LoadTests/LoadTests.csproj` — added ProjectReference to Performance.Profiling
- `EnterpriseIntegrationPlatform.sln` — added Performance.Profiling project
- `rules/milestones.md` — removed chunk 040, updated Next Chunk to 041, completed Phase 6
- `rules/completion-log.md` — this entry

### Admin API endpoints added

- `POST /api/admin/profiling/snapshot` — capture a new profile snapshot (optional ?label=)
- `GET /api/admin/profiling/snapshot/latest` — get the most recent snapshot
- `GET /api/admin/profiling/snapshots` — get snapshots in time range (?from=&to=)
- `GET /api/admin/profiling/hotspots` — detect hotspots with optional thresholds
- `GET /api/admin/profiling/operations` — get all tracked operation stats
- `GET /api/admin/profiling/gc` — capture and return GC snapshot
- `GET /api/admin/profiling/gc/recommendations` — get GC tuning recommendations
- `GET /api/admin/profiling/benchmarks` — list all benchmark baselines

### Test counts

- UnitTests: 826 (745 + 81 new)
- LoadTests: 10 (5 + 5 new)
- Total across all projects: 919 (UnitTests 826, ContractTests 29, WorkflowTests 24, IntegrationTests 17, PlaywrightTests 13, LoadTests 10)

---

## Chunk 039 – Disaster Recovery Automation

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: Automated failover, cross-region replication, recovery point validation, and DR drill framework.

### Architecture

- **IFailoverManager** — Interface for managing automated failover between primary and standby regions (register, failover, failback, health check).
- **InMemoryFailoverManager** — Thread-safe in-memory implementation with full state tracking, lock-based failover serialization.
- **IReplicationManager** — Interface for cross-region data replication monitoring (report progress, get lag/status).
- **InMemoryReplicationManager** — In-memory replication tracking with lag calculation based on pending items and configurable per-item replication time.
- **IRecoveryPointValidator** — Interface for validating RPO/RTO targets against current system state.
- **RecoveryPointValidator** — Validates registered recovery objectives against current replication lag and failover duration.
- **IDrDrillRunner** — Interface for running DR drill scenarios and tracking drill history.
- **DrDrillRunner** — Full drill orchestrator: detection → replication check → failover → objective validation → failback, with history retention.
- **DisasterRecoveryOptions** — Configuration (MaxReplicationLag, HealthCheckInterval, MaxDrillHistorySize, OfflineThreshold, PerItemReplicationTime).

### Files created

- `src/DisasterRecovery/DisasterRecovery.csproj`
- `src/DisasterRecovery/FailoverState.cs`
- `src/DisasterRecovery/RegionInfo.cs`
- `src/DisasterRecovery/FailoverResult.cs`
- `src/DisasterRecovery/ReplicationStatus.cs`
- `src/DisasterRecovery/RecoveryObjective.cs`
- `src/DisasterRecovery/RecoveryPointValidationResult.cs`
- `src/DisasterRecovery/DrDrillScenario.cs`
- `src/DisasterRecovery/DrDrillType.cs`
- `src/DisasterRecovery/DrDrillResult.cs`
- `src/DisasterRecovery/IFailoverManager.cs`
- `src/DisasterRecovery/IReplicationManager.cs`
- `src/DisasterRecovery/IRecoveryPointValidator.cs`
- `src/DisasterRecovery/IDrDrillRunner.cs`
- `src/DisasterRecovery/DisasterRecoveryOptions.cs`
- `src/DisasterRecovery/InMemoryFailoverManager.cs`
- `src/DisasterRecovery/InMemoryReplicationManager.cs`
- `src/DisasterRecovery/RecoveryPointValidator.cs`
- `src/DisasterRecovery/DrDrillRunner.cs`
- `src/DisasterRecovery/DisasterRecoveryServiceExtensions.cs`
- `tests/UnitTests/DisasterRecoveryTests/InMemoryFailoverManagerTests.cs` (14 tests)
- `tests/UnitTests/DisasterRecoveryTests/InMemoryReplicationManagerTests.cs` (13 tests)
- `tests/UnitTests/DisasterRecoveryTests/RecoveryPointValidatorTests.cs` (12 tests)
- `tests/UnitTests/DisasterRecoveryTests/DrDrillRunnerTests.cs` (13 tests)

### Files modified

- `src/Admin.Api/Admin.Api.csproj` — added ProjectReference to DisasterRecovery
- `src/Admin.Api/Program.cs` — added DisasterRecovery DI registration and 9 DR admin endpoints
- `tests/UnitTests/UnitTests.csproj` — added ProjectReference to DisasterRecovery
- `EnterpriseIntegrationPlatform.sln` — added DisasterRecovery project
- `rules/milestones.md` — removed chunks 038/039, updated Next Chunk to 040
- `rules/completion-log.md` — this entry

### Admin API endpoints added

- `GET /api/admin/dr/regions` — list all registered regions
- `POST /api/admin/dr/regions` — register a DR region
- `POST /api/admin/dr/failover/{targetRegionId}` — trigger failover
- `POST /api/admin/dr/failback/{regionId}` — trigger failback
- `GET /api/admin/dr/replication` — get all replication statuses
- `GET /api/admin/dr/objectives` — list recovery objectives
- `POST /api/admin/dr/objectives` — register a recovery objective
- `POST /api/admin/dr/drills` — run a DR drill
- `GET /api/admin/dr/drills/history` — get drill history

### Test count

- 52 new unit tests (total: 745 across UnitTests, 833 across all test projects)

---

## Chunk 038 – Tenant Onboarding Automation

- **Date**: 2026-04-03
- **Status**: done
- **Goal**: Self-service tenant provisioning, quota management, isolated broker namespaces, and onboarding workflow.

### Architecture

- **ITenantOnboardingService** — Interface for tenant provisioning and deprovisioning.
- **InMemoryTenantOnboardingService** — Thread-safe in-memory implementation with status tracking.
- **ITenantQuotaManager** — Interface for per-tenant quota management.
- **InMemoryTenantQuotaManager** — In-memory quota tracking.
- **IBrokerNamespaceProvisioner** — Interface for isolated broker namespace creation.
- **InMemoryBrokerNamespaceProvisioner** — In-memory broker namespace tracking.
- **TenantOnboardingServiceExtensions** — DI extension method `AddTenantOnboarding()`.

### Files created

- `src/MultiTenancy.Onboarding/MultiTenancy.Onboarding.csproj`
- `src/MultiTenancy.Onboarding/ITenantOnboardingService.cs`
- `src/MultiTenancy.Onboarding/InMemoryTenantOnboardingService.cs`
- `src/MultiTenancy.Onboarding/ITenantQuotaManager.cs`
- `src/MultiTenancy.Onboarding/InMemoryTenantQuotaManager.cs`
- `src/MultiTenancy.Onboarding/IBrokerNamespaceProvisioner.cs`
- `src/MultiTenancy.Onboarding/InMemoryBrokerNamespaceProvisioner.cs`
- `src/MultiTenancy.Onboarding/TenantOnboardingServiceExtensions.cs`
- `src/MultiTenancy.Onboarding/TenantOnboardingRequest.cs`
- `src/MultiTenancy.Onboarding/TenantOnboardingResult.cs`
- `src/MultiTenancy.Onboarding/OnboardingStatus.cs`
- `src/MultiTenancy.Onboarding/IsolationLevel.cs`
- `src/MultiTenancy.Onboarding/TenantPlan.cs`
- `src/MultiTenancy.Onboarding/TenantQuota.cs`
- `src/MultiTenancy.Onboarding/BrokerNamespaceConfig.cs`
- `tests/UnitTests/TenantOnboardingTests/InMemoryTenantOnboardingServiceTests.cs`
- `tests/UnitTests/TenantOnboardingTests/InMemoryTenantQuotaManagerTests.cs`
- `tests/UnitTests/TenantOnboardingTests/InMemoryBrokerNamespaceProvisionerTests.cs`

### Files modified

- `src/Admin.Api/Admin.Api.csproj` — added ProjectReference to MultiTenancy.Onboarding
- `src/Admin.Api/Program.cs` — added tenant onboarding DI registration and 5 admin endpoints
- `tests/UnitTests/UnitTests.csproj` — added ProjectReference to MultiTenancy.Onboarding
- `EnterpriseIntegrationPlatform.sln` — added MultiTenancy.Onboarding project

### Test count

- 27 new unit tests (total: 693 across UnitTests)

---

## Chunk 037 – Competing Consumers

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Dynamic consumer scaling, partition rebalancing, consumer lag monitoring, and backpressure signaling.

### Architecture

- **IConsumerScaler** — Interface for dynamic consumer instance management (ScaleUpAsync, ScaleDownAsync, GetActiveCountAsync).
- **IConsumerLagMonitor** — Interface for consumer lag tracking per consumer group/partition.
- **IBackpressureSignal** — Interface for signaling backpressure state (IsActive, Activate, Deactivate).
- **InMemoryConsumerScaler** — Thread-safe consumer scaling with configurable min/max instance bounds.
- **InMemoryConsumerLagMonitor** — In-memory lag tracking using ConcurrentDictionary with lag history.
- **BackpressureSignal** — Thread-safe backpressure signal with activation threshold and cooldown.
- **CompetingConsumerOrchestrator** — Coordinates scaling decisions based on lag metrics and backpressure signals.
- **CompetingConsumerOptions** — Configuration for min/max consumers, lag threshold, scale interval, and cooldown.

### Files created

- `src/Processing.CompetingConsumers/Processing.CompetingConsumers.csproj`
- `src/Processing.CompetingConsumers/IConsumerScaler.cs`
- `src/Processing.CompetingConsumers/IConsumerLagMonitor.cs`
- `src/Processing.CompetingConsumers/IBackpressureSignal.cs`
- `src/Processing.CompetingConsumers/InMemoryConsumerScaler.cs`
- `src/Processing.CompetingConsumers/InMemoryConsumerLagMonitor.cs`
- `src/Processing.CompetingConsumers/BackpressureSignal.cs`
- `src/Processing.CompetingConsumers/CompetingConsumerOrchestrator.cs`
- `src/Processing.CompetingConsumers/CompetingConsumerOptions.cs`
- `src/Processing.CompetingConsumers/ConsumerLagInfo.cs`
- `src/Processing.CompetingConsumers/CompetingConsumerServiceExtensions.cs`
- `tests/UnitTests/CompetingConsumersTests/BackpressureSignalTests.cs` (7 tests)
- `tests/UnitTests/CompetingConsumersTests/CompetingConsumerOrchestratorTests.cs` (11 tests)
- `tests/UnitTests/CompetingConsumersTests/InMemoryConsumerLagMonitorTests.cs` (10 tests)
- `tests/UnitTests/CompetingConsumersTests/InMemoryConsumerScalerTests.cs` (11 tests)

### Test count

- 39 new unit tests (total: 666 across UnitTests)

---

## Chunk 036 – Scatter-Gather Pattern

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Broadcast a request to multiple recipients, collect responses within a timeout window, and aggregate results.

### Architecture

- **IScatterGatherer<TRequest, TResponse>** — Generic interface for scatter-gather operations.
- **ScatterGatherer<TRequest, TResponse>** — Full implementation with Channel-based response collection, timeout handling, and concurrent operation tracking.
- **ScatterRequest<TRequest>** — Request record with correlation ID, payload, recipient list, and optional timeout.
- **GatherResponse<TResponse>** — Response record with recipient identifier, payload, success/error status.
- **ScatterGatherResult<TResponse>** — Aggregated result with all responses, timeout indicator, and duration.
- **ScatterGatherOptions** — Configuration for default timeout, max recipients, max concurrent operations.

### Files created

- `src/Processing.ScatterGather/Processing.ScatterGather.csproj`
- `src/Processing.ScatterGather/IScatterGatherer.cs`
- `src/Processing.ScatterGather/ScatterGatherer.cs`
- `src/Processing.ScatterGather/ScatterRequest.cs`
- `src/Processing.ScatterGather/GatherResponse.cs`
- `src/Processing.ScatterGather/ScatterGatherResult.cs`
- `src/Processing.ScatterGather/ScatterGatherOptions.cs`
- `src/Processing.ScatterGather/ScatterGatherServiceExtensions.cs`
- `tests/UnitTests/ScatterGatherTests/ScatterGathererTests.cs` (21 tests)

### Test count

- 21 new unit tests

---

## Chunk 035 – Event Sourcing

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Event store implementation, event projections, snapshot strategy, and temporal queries for full audit trail reconstruction.

### Architecture

- **IEventStore** — Interface for event persistence: AppendAsync, GetEventsAsync, GetEventsAfterAsync.
- **ISnapshotStore** — Interface for snapshot persistence: SaveAsync, GetLatestAsync.
- **IEventProjection** — Interface for event projections with type filtering and Apply method.
- **InMemoryEventStore** — Thread-safe event store with optimistic concurrency via expected version checks.
- **InMemorySnapshotStore** — Thread-safe snapshot store using ConcurrentDictionary.
- **EventProjectionEngine** — Processes events through registered projections with checkpoint tracking.
- **EventEnvelope** — Immutable record wrapping domain events with metadata (stream, version, timestamp, correlation).
- **TemporalQuery** — Query model for time-range event retrieval.
- **OptimisticConcurrencyException** — Thrown when event stream version conflicts are detected.
- **EventSourcingOptions** — Configuration for snapshot interval, max events per query, projection batch size.

### Files created

- `src/EventSourcing/EventSourcing.csproj`
- `src/EventSourcing/IEventStore.cs`
- `src/EventSourcing/ISnapshotStore.cs`
- `src/EventSourcing/IEventProjection.cs`
- `src/EventSourcing/InMemoryEventStore.cs`
- `src/EventSourcing/InMemorySnapshotStore.cs`
- `src/EventSourcing/EventProjectionEngine.cs`
- `src/EventSourcing/EventEnvelope.cs`
- `src/EventSourcing/TemporalQuery.cs`
- `src/EventSourcing/OptimisticConcurrencyException.cs`
- `src/EventSourcing/EventSourcingOptions.cs`
- `src/EventSourcing/EventSourcingServiceExtensions.cs`
- `tests/UnitTests/EventSourcingTests/InMemoryEventStoreTests.cs` (16 tests)
- `tests/UnitTests/EventSourcingTests/InMemorySnapshotStoreTests.cs` (7 tests)
- `tests/UnitTests/EventSourcingTests/EventProjectionEngineTests.cs` (11 tests)

### Test count

- 34 new unit tests

---

## Chunk 034 – Secrets Management

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: HashiCorp Vault and Azure Key Vault integration for runtime secret injection, automatic rotation, and audit logging.

### Architecture

- **ISecretProvider** — Interface for secret CRUD: GetSecretAsync, SetSecretAsync, DeleteSecretAsync, ListSecretsAsync.
- **ISecretRotationService** — Interface for automatic secret rotation with policy-based scheduling.
- **InMemorySecretProvider** — Thread-safe in-memory secret store for development and testing.
- **VaultSecretProvider** — HashiCorp Vault integration via HTTP API with token-based auth.
- **AzureKeyVaultSecretProvider** — Azure Key Vault integration via Azure.Security.KeyVault.Secrets SDK.
- **CachedSecretProvider** — Decorator that caches secrets with configurable TTL to reduce provider calls.
- **SecretRotationService** — Background service that monitors rotation policies and triggers rotation when due.
- **SecretAuditLogger** — Structured audit logging for all secret access, modification, and rotation events.
- **SecretEntry** — Record for secret data with version, expiry, and metadata.
- **SecretRotationPolicy** — Configuration record for rotation interval, notification window, and target secret.
- **SecretAuditEvent** — Audit event record with action, principal, timestamp, and outcome.
- **SecretAccessAction** — Enum of auditable actions (Get, Set, Delete, Rotate, List).
- **SecretsOptions** — Configuration for provider type, cache TTL, rotation check interval.

### Files created

- `src/Security.Secrets/Security.Secrets.csproj`
- `src/Security.Secrets/ISecretProvider.cs`
- `src/Security.Secrets/ISecretRotationService.cs`
- `src/Security.Secrets/InMemorySecretProvider.cs`
- `src/Security.Secrets/VaultSecretProvider.cs`
- `src/Security.Secrets/AzureKeyVaultSecretProvider.cs`
- `src/Security.Secrets/CachedSecretProvider.cs`
- `src/Security.Secrets/SecretRotationService.cs`
- `src/Security.Secrets/SecretAuditLogger.cs`
- `src/Security.Secrets/SecretEntry.cs`
- `src/Security.Secrets/SecretRotationPolicy.cs`
- `src/Security.Secrets/SecretAuditEvent.cs`
- `src/Security.Secrets/SecretAccessAction.cs`
- `src/Security.Secrets/SecretsOptions.cs`
- `src/Security.Secrets/SecretsServiceExtensions.cs`
- `tests/UnitTests/SecretsTests/CachedSecretProviderTests.cs` (11 tests)
- `tests/UnitTests/SecretsTests/InMemorySecretProviderTests.cs` (15 tests)
- `tests/UnitTests/SecretsTests/SecretAuditLoggerTests.cs` (11 tests)
- `tests/UnitTests/SecretsTests/SecretRotationServiceTests.cs` (9 tests)

### Test count

- 46 new unit tests

---

## Chunk 033 – Configuration Management

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Centralized configuration service with environment-specific overrides, feature flags, and dynamic reconfiguration without restart.

### Architecture

- **IConfigurationStore** — Interface for config CRUD: GetAsync, SetAsync, DeleteAsync, ListAsync, WatchAsync (change notifications).
- **IFeatureFlagService** — Interface for feature flags: IsEnabledAsync, GetVariantAsync.
- **InMemoryConfigurationStore** — Thread-safe ConcurrentDictionary-based implementation with change notifications via Channel<ConfigurationChange>.
- **InMemoryFeatureFlagService** — Thread-safe in-memory feature flag service with rollout percentage and tenant targeting.
- **EnvironmentOverrideProvider** — Resolves config values with environment cascade: specific env → default.
- **ConfigurationChangeNotifier** — Pub/sub for config changes using System.Threading.Channels.
- **Admin Endpoints** — GET/PUT/DELETE `/api/admin/config/{key}` + GET/PUT/DELETE `/api/admin/features/{name}`.

### Files created

- `src/Configuration/Configuration.csproj`
- `src/Configuration/IConfigurationStore.cs`
- `src/Configuration/IFeatureFlagService.cs`
- `src/Configuration/ConfigurationEntry.cs`
- `src/Configuration/FeatureFlag.cs`
- `src/Configuration/ConfigurationChange.cs`
- `src/Configuration/InMemoryConfigurationStore.cs`
- `src/Configuration/InMemoryFeatureFlagService.cs`
- `src/Configuration/EnvironmentOverrideProvider.cs`
- `src/Configuration/ConfigurationChangeNotifier.cs`
- `src/Configuration/ConfigurationServiceExtensions.cs`

### Files modified

- `src/Admin.Api/Program.cs` — Added 8 config/feature-flag endpoints
- `src/Admin.Api/Admin.Api.csproj` — Added Configuration project reference
- `tests/UnitTests/UnitTests.csproj` — Added Configuration project reference
- `EnterpriseIntegrationPlatform.sln` — Added Configuration project

### Tests added

- `tests/UnitTests/InMemoryConfigurationStoreTests.cs`
- `tests/UnitTests/InMemoryFeatureFlagServiceTests.cs`
- `tests/UnitTests/EnvironmentOverrideProviderTests.cs`
- `tests/UnitTests/ConfigurationChangeNotifierTests.cs`

---

## Chunk 032 – Grafana Dashboards

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Pre-built Grafana dashboards for platform health, message throughput, connector status, Temporal workflow metrics, and alerting rules.

### Architecture

- **5 Grafana dashboard JSON files** — platform-health, message-throughput, connector-status, temporal-workflows, dlq-overview. Schema version 39+, unique UIDs, real PromQL queries against PlatformMeters.cs metrics.
- **Provisioning configs** — Prometheus and Loki datasource YAML, dashboard auto-provisioning YAML, alerting rules YAML.
- **Alerting rules** — High error rate, DLQ depth threshold, service down, high latency, workflow failures.
- **Helm ConfigMap** — Mounts dashboard JSONs into Grafana pods.
- **Aspire integration** — Grafana container on port 15300 with provisioning volume mounts.

### Files created

- `deploy/grafana/dashboards/platform-health.json`
- `deploy/grafana/dashboards/message-throughput.json`
- `deploy/grafana/dashboards/connector-status.json`
- `deploy/grafana/dashboards/temporal-workflows.json`
- `deploy/grafana/dashboards/dlq-overview.json`
- `deploy/grafana/provisioning/datasources/prometheus.yaml`
- `deploy/grafana/provisioning/datasources/loki.yaml`
- `deploy/grafana/provisioning/dashboards/dashboards.yaml`
- `deploy/grafana/provisioning/alerting/alerts.yaml`
- `deploy/helm/eip/templates/grafana-dashboards-configmap.yaml`

### Files modified

- `deploy/helm/eip/values.yaml` — Added Grafana settings
- `src/AppHost/Program.cs` — Added Grafana container on port 15300

### Tests added

- `tests/UnitTests/GrafanaDashboardTests.cs` — 33 tests validating JSON structure, UIDs, datasources, provisioning

---

## Chunk 031b – Processing Throttle (Admin-Controlled, Per-Tenant/Queue/Endpoint)

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Implement token-bucket message processing throttle (distinct from HTTP rate limiting) with per-tenant, per-queue, and per-endpoint partitioning — controllable from Admin API at runtime. Like BizTalk host throttling and Apache Camel per-route throttle EIP. Advances Quality Pillar 11 (Performance — throughput control) and Pillar 6 (Resilience — backpressure signaling).

### Architecture

- **Rate Limiting vs Throttling** — Rate limiting (Gateway.Api/Admin.Api) rejects excess HTTP requests with 429 Too Many Requests. Throttling (Processing.Throttle) controls message processing speed by delaying consumers — smoothing throughput and preventing downstream overload. They are independent mechanisms.
- **TokenBucketThrottle** — SemaphoreSlim-based token bucket with configurable refill rate, burst capacity, max wait, and backpressure rejection mode.
- **ThrottleRegistry** — ConcurrentDictionary-based registry of partitioned throttles. Resolves in specificity order: exact (tenant+queue+endpoint) → tenant+queue → tenant → queue → global.
- **ThrottlePolicy** — Admin-configurable settings per partition. CRUD via Admin API endpoints.
- **Admin Endpoints** — GET/PUT/DELETE `/api/admin/throttle/policies` + GET `/api/admin/ratelimit/status` for visibility into both mechanisms.

### Files created

- `src/Processing.Throttle/Processing.Throttle.csproj`
- `src/Processing.Throttle/ThrottleOptions.cs`
- `src/Processing.Throttle/ThrottleResult.cs`
- `src/Processing.Throttle/ThrottleMetrics.cs`
- `src/Processing.Throttle/IMessageThrottle.cs`
- `src/Processing.Throttle/TokenBucketThrottle.cs`
- `src/Processing.Throttle/ThrottlePartitionKey.cs`
- `src/Processing.Throttle/ThrottlePolicy.cs`
- `src/Processing.Throttle/ThrottlePolicyStatus.cs`
- `src/Processing.Throttle/IThrottleRegistry.cs`
- `src/Processing.Throttle/ThrottleRegistry.cs`
- `src/Processing.Throttle/ThrottleServiceExtensions.cs`
- `tests/UnitTests/Throttle/TokenBucketThrottleTests.cs` — 8 tests
- `tests/UnitTests/Throttle/ThrottleRegistryTests.cs` — 12 tests
- `tests/UnitTests/Throttle/ThrottlePartitionKeyTests.cs` — 5 tests

### Files modified

- `src/Admin.Api/Admin.Api.csproj` — added Processing.Throttle reference
- `src/Admin.Api/Program.cs` — added throttle registry DI, 4 throttle admin endpoints, 1 rate limit status endpoint, SetThrottlePolicyRequest record
- `tests/UnitTests/UnitTests.csproj` — added Processing.Throttle reference
- `EnterpriseIntegrationPlatform.sln` — added Processing.Throttle project

### Test count

- UnitTests: 443 (was 418, +25 throttle tests)
- Total: 531 (was 506)

---

## Chunk 031 – API Gateway (Gateway.Api)

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Create Gateway.Api .NET project — single entry point for external integration traffic with reverse proxy routing, per-client + global rate limiting (429 rejection), JWT passthrough, correlation ID injection, request logging, downstream health aggregation, and API versioning. Advances Quality Pillar 3 (Scalability — single entry point) and Pillar 2 (Security — edge rate limiting).

### Files created

- `src/Gateway.Api/` — full project (Program.cs, GatewayServiceExtensions.cs, Middleware/, Routing/, Health/, Configuration/, Properties/, appsettings)
- `tests/UnitTests/Gateway/` — 16 tests (CorrelationId, RouteResolver, RequestLogging, DownstreamHealth)

### Files modified

- `EnterpriseIntegrationPlatform.sln` — added Gateway.Api
- `src/AppHost/AppHost.csproj` — added Gateway.Api reference
- `src/AppHost/Program.cs` — added gateway service on port 15000
- `tests/UnitTests/UnitTests.csproj` — added Gateway.Api reference

---

## Chunk 030 – CI/CD Pipeline Hardening

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Multi-environment CI/CD pipelines (dev/staging/prod), blue-green deployment, canary release strategy, automated rollback triggers. Advances Quality Pillar 5 (Availability — zero-downtime deployments) and Pillar 9 (Operational Excellence — automated deployment).

### Files created

- `.github/workflows/deploy.yml` — multi-environment deploy pipeline (7 jobs)
- `deploy/scripts/blue-green-deploy.sh`
- `deploy/scripts/canary-deploy.sh`
- `deploy/scripts/rollback.sh`
- `deploy/docker/Dockerfile` — multi-stage .NET build
- `deploy/docker/docker-compose.yml` — local dev compose
- `deploy/environments/dev.env`
- `deploy/environments/staging.env`
- `deploy/environments/prod.env`

---

## Chunk 029 – Kubernetes Deployment

- **Date**: 2026-04-02
- **Status**: done
- **Goal**: Helm charts, Kustomize overlays, namespace isolation, resource limits, liveness/readiness probes. Advances Quality Pillar 5 (Availability — K8s self-healing) and Pillar 3 (Scalability — HPA autoscaling).

### Files created

- `deploy/helm/eip/` — Chart.yaml, values.yaml, _helpers.tpl, 10 templates (namespace, services, HPA, NetworkPolicy, ServiceAccount, ConfigMap)
- `deploy/kustomize/base/` — namespace, OpenClaw.Web/Admin.Api deployments and services
- `deploy/kustomize/overlays/dev/` — 1 replica, small resources
- `deploy/kustomize/overlays/staging/` — 2 replicas, medium resources
- `deploy/kustomize/overlays/prod/` — 3 replicas, PodDisruptionBudgets
- `deploy/validate.sh` — YAML validation script

---

## Chunk 028 – AI-Assisted Code Generation

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Extend OpenClaw.Web with two new AI context-retrieval endpoints (`/api/generate/connector` and `/api/generate/schema`) that give developers' own AI providers (Copilot, Codex, Claude Code) structured RAG context for generating connectors and message schemas, advancing Quality Pillar 9 (Operational Excellence — reduce time to generate new integrations) and Pillar 4 (Maintainability — standardized prompt patterns).

### Architecture

- `/api/generate/connector` (POST) — accepts `GenerateConnectorRequest` (connector type, target description, auth type, related patterns), builds a structured query, retrieves context from RagFlow, returns `GenerateConnectorResponse`.
- `/api/generate/schema` (POST) — accepts `GenerateSchemaRequest` (message type, format, optional example payload), retrieves schema-related context from RagFlow, returns `GenerateSchemaResponse`.
- Both endpoints follow the same RAG-retrieval-only pattern established in chunks 009 and earlier: the platform retrieves context; the developer's AI provider generates the code.

### Files modified

- `src/OpenClaw.Web/Program.cs` — added `generate.MapPost("/connector")`, `generate.MapPost("/schema")`, and the four supporting record types.

---

## Chunk 027 – Operational Tooling

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Add DLQ resubmission endpoint to the Admin API, wiring `DlqManagementService` → `IMessageReplayer`, advancing Quality Pillar 7 (Supportability — operators can resubmit failed messages via a single API call) and Pillar 9 (Operational Excellence — reduce MTTR for poison message incidents).

### Architecture

- `DlqManagementService` — thin orchestration service in Admin.Api that delegates to `IMessageReplayer.ReplayAsync`, logs start/completion, and returns `ReplayResult`.
- `POST /api/admin/dlq/resubmit` — accepts `DlqResubmitRequest` (optional CorrelationId, MessageType, FromTimestamp, ToTimestamp filters), calls `DlqManagementService.ResubmitAsync`, returns `ReplayResult`. Protected by `X-Api-Key` authentication with Admin role.
- `AddMessageReplay` from `Processing.Replay` registered in Admin.Api DI.

### Files created

- `src/Admin.Api/Services/DlqManagementService.cs`

### Files modified

- `src/Admin.Api/Admin.Api.csproj` — added `Processing.DeadLetter` and `Processing.Replay` project references
- `src/Admin.Api/Program.cs` — added `AddMessageReplay`, `DlqManagementService` DI registration, `POST /api/admin/dlq/resubmit` endpoint, and `DlqResubmitRequest` record

---

## Chunk 026 – Load Testing

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Add in-process throughput load tests measuring platform component performance under concurrent load, advancing Quality Pillar 10 (Testability — performance regressions are caught by automated tests) and Pillar 11 (Performance — measured throughput baselines).

### Architecture

`ThroughputLoadTests` in the existing `LoadTests` project uses `Task.WhenAll` and `Parallel.For` to exercise real implementations with in-memory state (no external infrastructure). Tests measure elapsed time against generous thresholds (5 s for 1000 concurrent messages, 2 s for 10,000 payload validations) to catch catastrophic regressions in CI without flakiness.

Four tests:
1. `DeadLetterPublisher_1000ConcurrentPublishes_CompletesWithin5Seconds`
2. `InMemoryReplayStore_500ConcurrentStores_CompletesWithin5Seconds`
3. `ExponentialBackoffRetryPolicy_200ConcurrentSucceedingOperations_CompletesWithin5Seconds`
4. `PayloadSizeGuard_10000ConcurrentValidations_CompletesWithin2Seconds`

### Files created

- `tests/LoadTests/ThroughputLoadTests.cs` — 4 throughput load tests

### Files modified

- `tests/LoadTests/LoadTests.csproj` — added project references for Contracts, DeadLetter, Retry, Replay, Ingestion, Security, MultiTenancy

---

## Chunk 025 – Saga Compensation

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the Saga compensation workflow in Temporal, advancing Quality Pillar 1 (Reliability — all-or-nothing saga semantics with explicit compensation) and Pillar 6 (Resilience — partial compensation continues on failure rather than aborting).

### Architecture

**New types in `Activities`:**
- `SagaCompensationInput` — record: `CorrelationId`, `OriginalMessageId`, `MessageType`, `CompensationSteps` (forward-ordered), `FailureReason`.
- `SagaCompensationResult` — record: `CorrelationId`, `CompensatedSteps`, `FailedSteps`, `IsFullyCompensated`.
- `ICompensationActivityService` / `DefaultCompensationActivityService` — interface + logging default for executing named compensation steps. Production deployments replace with real rollback logic per step name.

**New types in `Workflow.Temporal`:**
- `SagaCompensationActivities` — Temporal activities that call `ICompensationActivityService.CompensateAsync` and log each step's start/success/failure stage.
- `SagaCompensationWorkflow` — Temporal workflow that reverses `CompensationSteps` (last-to-first) and executes each via `SagaCompensationActivities.CompensateStepAsync`. Continues past failed steps (records in `FailedSteps`) to maximise partial compensation.

**Updated `TemporalServiceExtensions`:** registers `ICompensationActivityService`, `SagaCompensationWorkflow`, and `SagaCompensationActivities`.

### Files created

- `src/Activities/SagaCompensationInput.cs`
- `src/Activities/SagaCompensationResult.cs`
- `src/Activities/ICompensationActivityService.cs`
- `src/Activities/DefaultCompensationActivityService.cs`
- `src/Workflow.Temporal/Activities/SagaCompensationActivities.cs`
- `src/Workflow.Temporal/Workflows/SagaCompensationWorkflow.cs`
- `tests/WorkflowTests/SagaCompensationActivitiesTests.cs` — 4 tests

### Files modified

- `src/Workflow.Temporal/TemporalServiceExtensions.cs` — registered saga types

---

## Chunk 024 – Multi-Tenancy

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement tenant resolution and tenant isolation guard in a new `MultiTenancy` project, advancing Quality Pillar 2 (Security — cross-tenant data access prevented at envelope boundaries) and Pillar 3 (Scalability — tenant-keyed metadata enables per-tenant routing and partitioning).

### Architecture

- `TenantContext` — record: `TenantId`, `TenantName?`, `IsResolved`. Static `Anonymous` sentinel.
- `ITenantResolver` / `TenantResolver` — resolves tenant from `IReadOnlyDictionary<string, string>` metadata (key `tenantId`) or from a raw string. Returns `TenantContext.Anonymous` when absent.
- `ITenantIsolationGuard` / `TenantIsolationGuard` — validates that an `IntegrationEnvelope<T>` belongs to the expected tenant; throws `TenantIsolationException` on mismatch or missing tenant ID.
- `TenantIsolationException` — exposes `MessageId`, `ActualTenantId`, `ExpectedTenantId`.
- `MultiTenancyServiceExtensions` — `AddMultiTenancy(IServiceCollection)`.

### Files created

- `src/MultiTenancy/MultiTenancy.csproj`
- `src/MultiTenancy/TenantContext.cs`
- `src/MultiTenancy/ITenantResolver.cs`
- `src/MultiTenancy/TenantResolver.cs`
- `src/MultiTenancy/ITenantIsolationGuard.cs`
- `src/MultiTenancy/TenantIsolationGuard.cs`
- `src/MultiTenancy/TenantIsolationException.cs`
- `src/MultiTenancy/MultiTenancyServiceExtensions.cs`
- `tests/UnitTests/TenantResolverTests.cs` — 7 tests
- `tests/UnitTests/TenantIsolationGuardTests.cs` — 7 tests

### Files modified

- `EnterpriseIntegrationPlatform.sln` — added `MultiTenancy` project
- `tests/UnitTests/UnitTests.csproj` — added `MultiTenancy` project reference

---

## Chunk 023 – Security

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the Security library with JWT bearer authentication, payload size enforcement, and input sanitization, advancing Quality Pillar 2 (Security — JWT authentication, CRLF injection prevention, oversized payload rejection) and Pillar 4 (Maintainability — reusable security services across all API projects).

### Architecture

- `JwtOptions` — bound from `Jwt` config section: `Issuer`, `Audience`, `SigningKey`, `ValidateLifetime`, `ClockSkew`.
- `SecurityServiceExtensions.AddPlatformJwtAuthentication` — registers `JwtBearerDefaults.AuthenticationScheme` with `TokenValidationParameters` built from `JwtOptions`. Guards: throws `InvalidOperationException` if `SigningKey` is empty.
- `PayloadSizeOptions` — `MaxPayloadBytes` (default 1 MB). `IPayloadSizeGuard` / `PayloadSizeGuard` — checks byte count of string (UTF-8) or byte array; throws `PayloadTooLargeException` on excess.
- `PayloadTooLargeException` — exposes `ActualBytes` and `MaxBytes`.
- `IInputSanitizer` / `InputSanitizer` — `Sanitize` removes CRLF and null bytes; `IsClean` validates input is free of dangerous characters.
- `SecurityServiceExtensions.AddPayloadSizeGuard` / `AddInputSanitizer` — DI registration helpers.

### Files created

- `src/Security/Security.csproj`
- `src/Security/JwtOptions.cs`
- `src/Security/SecurityServiceExtensions.cs`
- `src/Security/PayloadSizeOptions.cs`
- `src/Security/IPayloadSizeGuard.cs`
- `src/Security/PayloadSizeGuard.cs`
- `src/Security/PayloadTooLargeException.cs`
- `src/Security/IInputSanitizer.cs`
- `src/Security/InputSanitizer.cs`
- `tests/UnitTests/PayloadSizeGuardTests.cs` — 8 tests
- `tests/UnitTests/InputSanitizerTests.cs` — 9 tests

### Files modified

- `Directory.Packages.props` — added `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.5
- `EnterpriseIntegrationPlatform.sln` — added `Security` project
- `tests/UnitTests/UnitTests.csproj` — added `Security` and `MultiTenancy` project references

---

## Chunk 022 – File Connector

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the File connector for reading and writing files on the local or network file system, advancing Quality Pillar 6 (Resilience — metadata sidecar preserves correlation context for recovery) and Pillar 11 (Performance — async I/O via IFileSystem abstraction).

### Architecture

`Connector.File` is a standalone class library in namespace `EnterpriseIntegrationPlatform.Connector.FileSystem` (to avoid conflict with `System.IO.File`).

- `FileConnectorOptions` — `RootDirectory` (required), `Encoding` (default `"utf-8"`), `CreateDirectoryIfNotExists` (default `true`), `OverwriteExisting` (default `false`), `FilenamePattern` (default `"{MessageId}-{MessageType}.json"`).
- `IFileSystem` / `PhysicalFileSystem` — abstraction over `System.IO.File` and `Directory` for testability.
- `IFileConnector` / `FileConnector` — `WriteAsync<T>` expands the filename pattern (`{MessageId}`, `{MessageType}`, `{CorrelationId}`, `{Timestamp:yyyyMMddHHmmss}`), writes payload bytes and a `.meta.json` sidecar with envelope metadata, honours `OverwriteExisting`. `ReadAsync` returns raw bytes. `ListFilesAsync` lists files matching a search pattern.
- `FileConnectorServiceExtensions` — `AddFileConnector(IServiceCollection, IConfiguration)`.

### Files created

- `src/Connector.File/Connector.File.csproj`
- `src/Connector.File/FileConnectorOptions.cs`
- `src/Connector.File/IFileSystem.cs`
- `src/Connector.File/PhysicalFileSystem.cs`
- `src/Connector.File/IFileConnector.cs`
- `src/Connector.File/FileConnector.cs`
- `src/Connector.File/FileConnectorServiceExtensions.cs`
- `tests/UnitTests/FileConnectorTests.cs` — 10 tests

### Files modified

- `tests/UnitTests/UnitTests.csproj` — added `Connector.File` project reference
- `EnterpriseIntegrationPlatform.sln` — added `Connector.File` project

---

## Chunk 021 – Email Connector

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the Email connector using MailKit/MimeKit, advancing Quality Pillar 6 (Resilience — always-disconnect SMTP finally block) and Pillar 2 (Security — MimeKit 4.15.1 overrides vulnerable 4.12.0 transitive dep for GHSA-g7hc-96xr-gvvx).

### Architecture

- `EmailConnectorOptions` — `SmtpHost`, `SmtpPort` (587), `UseTls` (true), `Username`, `Password`, `DefaultFrom`, `DefaultSubjectTemplate` (`"{MessageType} notification"`).
- `ISmtpClientWrapper` / `MailKitSmtpClientWrapper` — thin wrapper around `MailKit.Net.Smtp.SmtpClient` for testability.
- `IEmailConnector` / `EmailConnector` — builds `MimeMessage`, adds `X-Correlation-Id` and `X-Message-Id` headers, connects/authenticates/sends/disconnects (always in finally). Two overloads: single address and list of addresses.
- `EmailConnectorServiceExtensions` — `AddEmailConnector(IServiceCollection, IConfiguration)`.
- `Directory.Packages.props` updated: `MailKit` → 4.15.1, explicit `MimeKit` 4.15.1 override for GHSA-g7hc-96xr-gvvx.

### Files created

- `src/Connector.Email/Connector.Email.csproj`
- `src/Connector.Email/EmailConnectorOptions.cs`
- `src/Connector.Email/ISmtpClientWrapper.cs`
- `src/Connector.Email/MailKitSmtpClientWrapper.cs`
- `src/Connector.Email/IEmailConnector.cs`
- `src/Connector.Email/EmailConnector.cs`
- `src/Connector.Email/EmailConnectorServiceExtensions.cs`
- `tests/UnitTests/EmailConnectorTests.cs` — 10 tests

### Files modified

- `Directory.Packages.props` — upgraded MailKit to 4.15.1, added MimeKit 4.15.1 override
- `tests/UnitTests/UnitTests.csproj` — added `Connector.Email` project reference
- `EnterpriseIntegrationPlatform.sln` — added `Connector.Email` project

---

## Chunk 020 – SFTP Connector

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the SFTP connector using SSH.NET, advancing Quality Pillar 6 (Resilience — always-disconnect finally block) and Pillar 7 (Supportability — correlation metadata sidecar for every upload).

### Architecture

- `SftpConnectorOptions` — `Host`, `Port` (22), `Username`, `Password`, `RootPath` ("/"), `TimeoutMs` (10000).
- `ISftpClient` / `SshNetSftpClient` — thin wrapper around `Renci.SshNet.SftpClient` for testability.
- `ISftpConnector` / `SftpConnector` — `UploadAsync` writes data file + `.meta` sidecar with correlation JSON, always disconnects. `DownloadAsync` returns raw bytes. `ListFilesAsync` returns file list.
- `SftpConnectorServiceExtensions` — `AddSftpConnector(IServiceCollection, IConfiguration)`.

### Files created

- `src/Connector.Sftp/Connector.Sftp.csproj`
- `src/Connector.Sftp/SftpConnectorOptions.cs`
- `src/Connector.Sftp/ISftpClient.cs`
- `src/Connector.Sftp/SshNetSftpClient.cs`
- `src/Connector.Sftp/ISftpConnector.cs`
- `src/Connector.Sftp/SftpConnector.cs`
- `src/Connector.Sftp/SftpConnectorServiceExtensions.cs`
- `tests/UnitTests/SftpConnectorTests.cs` — 10 tests

### Files modified

- `Directory.Packages.props` — added `SSH.NET` 2024.2.0
- `tests/UnitTests/UnitTests.csproj` — added `Connector.Sftp` project reference
- `EnterpriseIntegrationPlatform.sln` — added `Connector.Sftp` project

---

## Chunk 019 – HTTP Connector

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the HTTP connector with bearer-token caching, advancing Quality Pillar 6 (Resilience — retry via `Microsoft.Extensions.Http.Resilience`), Pillar 11 (Performance — token cache avoids repeated round-trips), and Pillar 2 (Security — cached token with configurable expiry).

### Architecture

- `HttpConnectorOptions` — `BaseUrl`, `TimeoutSeconds` (30), `MaxRetryAttempts` (3), `RetryDelayMs` (1000), `CacheTokenExpirySeconds` (300), `DefaultHeaders`.
- `ITokenCache` / `InMemoryTokenCache` — thread-safe `ConcurrentDictionary`-backed cache with per-entry expiry.
- `IHttpConnector` / `HttpConnector` — `SendAsync` adds `X-Correlation-Id`/`X-Message-Id` headers, serializes body for POST/PUT, deserializes response. `SendWithTokenAsync` resolves (or fetches+caches) bearer token before sending.
- `HttpConnectorServiceExtensions` — `AddHttpConnector(IServiceCollection, IConfiguration)`.

### Files created

- `src/Connector.Http/Connector.Http.csproj`
- `src/Connector.Http/HttpConnectorOptions.cs`
- `src/Connector.Http/ITokenCache.cs`
- `src/Connector.Http/InMemoryTokenCache.cs`
- `src/Connector.Http/IHttpConnector.cs`
- `src/Connector.Http/HttpConnector.cs`
- `src/Connector.Http/HttpConnectorServiceExtensions.cs`
- `tests/UnitTests/HttpConnectorTests.cs` — 10 tests
- `tests/UnitTests/InMemoryTokenCacheTests.cs` — 6 tests

### Files modified

- `tests/UnitTests/UnitTests.csproj` — added `Connector.Http` project reference
- `EnterpriseIntegrationPlatform.sln` — added `Connector.Http` project

---

## Chunk 018 – Replay Framework

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the Replay EIP pattern in a new `Processing.Replay` project, advancing Quality Pillar 1 (Reliability — replay lost or failed messages) and Pillar 6 (Resilience — recover from partial failures by replaying to a target topic).

### Architecture

- `ReplayOptions` — `SourceTopic` (required), `TargetTopic` (required), `MaxMessages` (1000), `BatchSize` (100).
- `ReplayFilter` — record: `CorrelationId?`, `MessageType?`, `FromTimestamp?`, `ToTimestamp?`.
- `ReplayResult` — record: `ReplayedCount`, `SkippedCount`, `FailedCount`, `StartedAt`, `CompletedAt`.
- `IMessageReplayStore` / `InMemoryMessageReplayStore` — thread-safe `ConcurrentDictionary<string, ConcurrentQueue<…>>`, supports filter-aware `IAsyncEnumerable` retrieval.
- `IMessageReplayer` / `MessageReplayer` — reads from store using source topic + filter, republishes to target topic with new `MessageId` and `CausationId` = original `MessageId`, counts replayed/failed.
- `ReplayServiceExtensions` — `AddMessageReplay(IServiceCollection, IConfiguration)`.

### Files created

- `src/Processing.Replay/Processing.Replay.csproj`
- `src/Processing.Replay/ReplayOptions.cs`
- `src/Processing.Replay/ReplayFilter.cs`
- `src/Processing.Replay/ReplayResult.cs`
- `src/Processing.Replay/IMessageReplayStore.cs`
- `src/Processing.Replay/InMemoryMessageReplayStore.cs`
- `src/Processing.Replay/IMessageReplayer.cs`
- `src/Processing.Replay/MessageReplayer.cs`
- `src/Processing.Replay/ReplayServiceExtensions.cs`
- `tests/UnitTests/ReplayOptionsTests.cs` — 5 tests
- `tests/UnitTests/InMemoryMessageReplayStoreTests.cs` — 8 tests
- `tests/UnitTests/MessageReplayerTests.cs` — 10 tests

### Files modified

- `tests/UnitTests/UnitTests.csproj` — added `Processing.Replay` project reference
- `EnterpriseIntegrationPlatform.sln` — added `Processing.Replay` project

---

## Chunk 017 – Retry Framework

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the Retry framework in a new `Processing.Retry` project, advancing Quality Pillar 6 (Resilience — exponential backoff with jitter, bounded retries) and Pillar 1 (Reliability — classify transient vs permanent failures).

### Architecture

- `RetryOptions` — `MaxAttempts` (3), `InitialDelayMs` (1000), `MaxDelayMs` (30000), `BackoffMultiplier` (2.0), `UseJitter` (true).
- `RetryResult<T>` — record: `IsSucceeded`, `Attempts`, `LastException?`, `Result?`.
- `IRetryPolicy` — `ExecuteAsync<T>(Func<CancellationToken, Task<T>>, CancellationToken)` and `ExecuteAsync(Func<CancellationToken, Task>, CancellationToken)`.
- `ExponentialBackoffRetryPolicy` — real exponential backoff: `delay = min(InitialDelayMs × BackoffMultiplier^(attempt-1), MaxDelayMs)`. Jitter adds ±20% random variation. `OperationCanceledException` propagates immediately; all other exceptions are retried up to `MaxAttempts`.
- `RetryServiceExtensions` — `AddRetryPolicy(IServiceCollection, IConfiguration)`.

### Files created

- `src/Processing.Retry/Processing.Retry.csproj`
- `src/Processing.Retry/RetryOptions.cs`
- `src/Processing.Retry/RetryResult.cs`
- `src/Processing.Retry/IRetryPolicy.cs`
- `src/Processing.Retry/ExponentialBackoffRetryPolicy.cs`
- `src/Processing.Retry/RetryServiceExtensions.cs`
- `tests/UnitTests/RetryOptionsTests.cs` — 6 tests
- `tests/UnitTests/ExponentialBackoffRetryPolicyTests.cs` — 12 tests (adjusted to avoid real delays: zero-delay config)

### Files modified

- `tests/UnitTests/UnitTests.csproj` — added `Processing.Retry` project reference
- `EnterpriseIntegrationPlatform.sln` — added `Processing.Retry` project

---

## Chunk 016 – Dead Letter Queue

- **Date**: 2026-03-24
- **Status**: done
- **Goal**: Implement the Dead Letter Queue pattern in a new `Processing.DeadLetter` project, advancing Quality Pillar 1 (Reliability — zero message loss: every unprocessable message is routed to DLQ), Pillar 6 (Resilience — DLQ as last resort for poison messages and max-retry exhaustion), and Pillar 7 (Supportability — DLQ envelopes carry full error context for operator inspection).

### Architecture

- `DeadLetterOptions` — `DeadLetterTopic` (required), `MaxRetryAttempts` (3), `Source` (optional override), `MessageType` (optional override, defaults to `"DeadLetter"`).
- `DeadLetterReason` — enum: `MaxRetriesExceeded`, `PoisonMessage`, `ProcessingTimeout`, `ValidationFailed`, `UnroutableMessage`.
- `DeadLetterEnvelope<T>` — record wrapping `OriginalEnvelope`, `Reason`, `ErrorMessage`, `FailedAt`, `AttemptCount`.
- `IDeadLetterPublisher<T>` / `DeadLetterPublisher<T>` — wraps the original envelope in `DeadLetterEnvelope<T>`, creates a new `IntegrationEnvelope<DeadLetterEnvelope<T>>` preserving `CorrelationId` with `CausationId = original.MessageId`, and publishes to `DeadLetterOptions.DeadLetterTopic` via `IMessageBrokerProducer`. Guards: `ArgumentNullException` if envelope null; `InvalidOperationException` if `DeadLetterTopic` is empty.
- `DeadLetterServiceExtensions` — `AddDeadLetterPublisher<T>(IServiceCollection, IConfiguration)`.

### Files created

- `src/Processing.DeadLetter/Processing.DeadLetter.csproj`
- `src/Processing.DeadLetter/DeadLetterOptions.cs`
- `src/Processing.DeadLetter/DeadLetterReason.cs`
- `src/Processing.DeadLetter/DeadLetterEnvelope.cs`
- `src/Processing.DeadLetter/IDeadLetterPublisher.cs`
- `src/Processing.DeadLetter/DeadLetterPublisher.cs`
- `src/Processing.DeadLetter/DeadLetterServiceExtensions.cs`
- `tests/UnitTests/DeadLetterOptionsTests.cs` — 5 tests
- `tests/UnitTests/DeadLetterPublisherTests.cs` — 12 tests

### Files modified

- `tests/UnitTests/UnitTests.csproj` — added `Processing.DeadLetter` project reference
- `EnterpriseIntegrationPlatform.sln` — added `Processing.DeadLetter` project

---

## Chunk 015 – Aggregator

- **Date**: 2026-03-16
- **Status**: done
- **Goal**: Implement the Aggregator EIP pattern in a new `Processing.Aggregator` project, advancing Quality Pillars 1 (Reliability — zero message loss across multi-message correlation groups), 4 (Maintainability — composable aggregation and completion strategy abstractions), and 10 (Testability — pure unit-testable aggregation logic with in-memory store).

### Architecture

The Aggregator is a standalone class library (`Processing.Aggregator`) that collects individual `IntegrationEnvelope<TItem>` messages sharing the same `CorrelationId`, evaluates a pluggable completion condition, and when the group is complete, combines all payloads using a pluggable `IAggregationStrategy<TItem, TAggregate>` and publishes a single `IntegrationEnvelope<TAggregate>` via `IMessageBrokerProducer`.

**Aggregation flow:**
1. The `MessageAggregator<TItem, TAggregate>` receives an individual envelope and validates the `TargetTopic` configuration.
2. The envelope is appended to its correlation group in the injected `IMessageAggregateStore<TItem>`, keyed by `CorrelationId`.
3. The injected `ICompletionStrategy<TItem>` is evaluated against the current group.
4. If not complete: an `AggregateResult<TAggregate>` with `IsComplete = false` and no envelope is returned.
5. If complete: the injected `IAggregationStrategy<TItem, TAggregate>` combines all payloads into an aggregate. A new envelope is created preserving `CorrelationId`, merging all `Metadata` (last-write wins on key conflicts), and adopting the highest `Priority` in the group. `CausationId` is not set because the aggregate has multiple causal messages. The group is removed from the store, the aggregate is published to `AggregatorOptions.TargetTopic`, and a complete `AggregateResult<TAggregate>` is returned.

**Provided `ICompletionStrategy<TItem>` implementations:**
- `CountCompletionStrategy<T>` — Completes when the group reaches a configured `ExpectedCount`. Guard: `expectedCount > 0` enforced in constructor.
- `FuncCompletionStrategy<T>` — Wraps a caller-supplied `Func<IReadOnlyList<IntegrationEnvelope<T>>, bool>` predicate for arbitrary completion logic (e.g., payload-content-based completion).

**Provided `IAggregationStrategy<TItem, TAggregate>` implementations:**
- `FuncAggregationStrategy<TItem, TAggregate>` — Wraps a caller-supplied `Func<IReadOnlyList<TItem>, TAggregate>` delegate for inline or lambda-based aggregation.

**`IMessageAggregateStore<T>` and `InMemoryMessageAggregateStore<T>`:**
- Thread-safe in-memory store using `ConcurrentDictionary<Guid, List<T>>` with `lock` on each list for safe concurrent adds to the same group.
- `AddAsync` appends and returns an immutable snapshot; `RemoveGroupAsync` clears the group.
- Not durable across restarts; intended for development, testing, and Temporal-backed workflows. Replace with Cassandra-backed store for durable production deployments.

**`AggregatorOptions`** (bound from `MessageAggregator` configuration section):
- `TargetTopic` — Required. Topic to publish the aggregate envelope to.
- `TargetMessageType` — Optional. Overrides the aggregate envelope's `MessageType`; when absent, the first received envelope's `MessageType` is used.
- `TargetSource` — Optional. Overrides the aggregate envelope's `Source`; when absent, the first received envelope's `Source` is used.
- `ExpectedCount` — Used by `CountCompletionStrategy<T>` when no custom completion predicate is provided.

**DI registration (`AggregatorServiceExtensions`):**
- `AddMessageAggregator<TItem, TAggregate>(IServiceCollection, IConfiguration, Func<IReadOnlyList<TItem>, TAggregate>, Func<...>?)` — Registers with a delegate aggregation strategy. When `completionPredicate` is `null`, a `CountCompletionStrategy<TItem>` using `AggregatorOptions.ExpectedCount` is used. Always registers `InMemoryMessageAggregateStore<TItem>`.

### Files created

- `src/Processing.Aggregator/Processing.Aggregator.csproj` — Class library; depends on `Contracts` and `Ingestion`
- `src/Processing.Aggregator/IAggregationStrategy.cs` — Interface: `Aggregate(IReadOnlyList<TItem>) → TAggregate`
- `src/Processing.Aggregator/ICompletionStrategy.cs` — Interface: `IsComplete(IReadOnlyList<IntegrationEnvelope<T>>) → bool`
- `src/Processing.Aggregator/IMessageAggregateStore.cs` — Interface: `AddAsync`, `RemoveGroupAsync`
- `src/Processing.Aggregator/InMemoryMessageAggregateStore.cs` — Thread-safe ConcurrentDictionary + lock store
- `src/Processing.Aggregator/IMessageAggregator.cs` — Interface: `AggregateAsync(IntegrationEnvelope<TItem>) → AggregateResult<TAggregate>`
- `src/Processing.Aggregator/AggregateResult.cs` — Result record: `IsComplete`, `AggregateEnvelope`, `CorrelationId`, `ReceivedCount`
- `src/Processing.Aggregator/AggregatorOptions.cs` — Options: `TargetTopic`, `TargetMessageType`, `TargetSource`, `ExpectedCount`
- `src/Processing.Aggregator/MessageAggregator.cs` — Production implementation; highest-priority adoption; metadata merge; group cleanup before publish
- `src/Processing.Aggregator/FuncAggregationStrategy.cs` — Delegate-based aggregation strategy
- `src/Processing.Aggregator/FuncCompletionStrategy.cs` — Delegate-based completion predicate
- `src/Processing.Aggregator/CountCompletionStrategy.cs` — Count-based completion; constructor guard for ≤0
- `src/Processing.Aggregator/AggregatorServiceExtensions.cs` — DI extensions `AddMessageAggregator`
- `tests/UnitTests/AggregatorOptionsTests.cs` — 8 tests for `AggregatorOptions` defaults and values
- `tests/UnitTests/InMemoryMessageAggregateStoreTests.cs` — 8 tests: add/retrieve, multi-add, isolation, remove, fresh-start, non-existent remove, thread safety
- `tests/UnitTests/MessageAggregatorTests.cs` — 23 tests covering incomplete group, complete group, payload aggregation, envelope headers (CorrelationId, CausationId null, new MessageId, highest priority, merged metadata), MessageType/Source override, broker publish, guard clauses, group isolation, group cleared after completion, custom completion predicate, CorrelationId on incomplete result

### Files modified

- `tests/UnitTests/UnitTests.csproj` — Added `<ProjectReference>` to `Processing.Aggregator`
- `EnterpriseIntegrationPlatform.sln` — Added `Processing.Aggregator` project
- `rules/milestones.md` — Marked chunk 015 as done, updated Next Chunk to 016
- `rules/completion-log.md` — This entry

### Notes

- All 260 unit tests pass (39 new + 221 pre-existing). Build: 0 warnings, 0 errors.
- `MessageAggregator<TItem, TAggregate>` uses two type parameters for full type safety — items and aggregate can be different types (e.g. `TItem=string, TAggregate=string` joining, or `TItem=JsonElement, TAggregate=JsonElement` merging into an array).
- The aggregate envelope's `CausationId` is deliberately `null` — unlike the Splitter (one cause → many) or Translator (one cause → one), the Aggregator combines many messages into one; there is no single causal message.
- `InMemoryMessageAggregateStore<T>` uses `ConcurrentDictionary.GetOrAdd` plus a `lock` on the list for safe concurrent adds within the same correlation group. This avoids TOCTOU races.
- After a group is aggregated, `RemoveGroupAsync` is called before `PublishAsync` to prevent re-aggregation if a late-arriving message triggers the store again — the group is clean before the downstream consumer sees the aggregate.
- `CountCompletionStrategy<T>` uses `>=` (not `==`) so that groups receiving more messages than expected still complete — defensive against at-least-once delivery.


## Chunk 014 – Splitter

- **Date**: 2026-03-16
- **Status**: done
- **Goal**: Implement the Splitter EIP pattern in a new `Processing.Splitter` project, advancing Quality Pillars 1 (Reliability — no message loss during split), 4 (Maintainability — composable split strategy abstraction), and 10 (Testability — pure unit-testable split logic).

### Architecture

The Splitter is a standalone class library (`Processing.Splitter`) that decomposes a composite `IntegrationEnvelope<T>` into individual `IntegrationEnvelope<T>` messages using a pluggable `ISplitStrategy<T>` and publishes each to a configured target topic via `IMessageBrokerProducer`.

**Splitting flow:**
1. The `MessageSplitter<T>` receives a source envelope and validates the `TargetTopic` configuration.
2. The injected `ISplitStrategy<T>` decomposes the payload into individual items.
3. For each item, a new envelope is created, preserving `CorrelationId`, `Priority`, `SchemaVersion`, and `Metadata` from the source. `CausationId` is set to `source.MessageId` to maintain the full causation chain.
4. `MessageType` and `Source` on each split envelope are overridden when `SplitterOptions.TargetMessageType` / `TargetSource` are configured; otherwise they are inherited from the source.
5. Each split envelope is published to `SplitterOptions.TargetTopic` via `IMessageBrokerProducer`.
6. A `SplitResult<T>` record is returned containing the split envelopes, source message ID, target topic, and item count for observability and downstream use.
7. When the split produces zero items, a warning is logged and no messages are published — the result contains an empty list with `ItemCount = 0`.

**Provided `ISplitStrategy<T>` implementations:**
- `FuncSplitStrategy<T>` — Wraps a caller-supplied `Func<T, IReadOnlyList<T>>` delegate. Use for inline or lambda-based split logic.
- `JsonArraySplitStrategy` — Splits a `JsonElement` containing a JSON array into individual `JsonElement` items. Supports both top-level arrays and named array properties within JSON objects (via `SplitterOptions.ArrayPropertyName`). Each element is cloned to ensure independence from the source `JsonDocument` lifetime.

**`SplitterOptions`** (bound from `MessageSplitter` configuration section):
- `TargetTopic` — Required. Topic to publish split envelopes to.
- `TargetMessageType` — Optional. Overrides the split envelopes' `MessageType`; when absent, the source `MessageType` is preserved.
- `TargetSource` — Optional. Overrides the split envelopes' `Source`; when absent, the source `Source` is preserved.
- `ArrayPropertyName` — Optional. Used by `JsonArraySplitStrategy` to specify which property in a JSON object contains the array to split. When absent, the payload is expected to be a top-level JSON array.

**DI registration (`SplitterServiceExtensions`):**
- `AddMessageSplitter<T>(IServiceCollection, IConfiguration, Func<T, IReadOnlyList<T>>)` — Registers with a delegate strategy.
- `AddJsonMessageSplitter(IServiceCollection, IConfiguration)` — Registers a `JsonArraySplitStrategy`-backed JSON splitter.

Both overloads require an `IMessageBrokerProducer` to already be registered (e.g. via `AddNatsJetStreamBroker`).

### Files created

- `src/Processing.Splitter/Processing.Splitter.csproj` — Class library; depends on `Contracts` and `Ingestion`
- `src/Processing.Splitter/ISplitStrategy.cs` — Interface: `Split(T) → IReadOnlyList<T>`
- `src/Processing.Splitter/IMessageSplitter.cs` — Interface: `SplitAsync(IntegrationEnvelope<T>) → SplitResult<T>`
- `src/Processing.Splitter/SplitResult.cs` — Result record: `SplitEnvelopes`, `SourceMessageId`, `TargetTopic`, `ItemCount`
- `src/Processing.Splitter/SplitterOptions.cs` — Options: `TargetTopic`, `TargetMessageType`, `TargetSource`, `ArrayPropertyName`
- `src/Processing.Splitter/MessageSplitter.cs` — Production implementation; preserves causation chain; configurable type/source override
- `src/Processing.Splitter/FuncSplitStrategy.cs` — Delegate-based split strategy
- `src/Processing.Splitter/JsonArraySplitStrategy.cs` — JSON array split implementation; top-level and named property; element cloning
- `src/Processing.Splitter/SplitterServiceExtensions.cs` — DI extensions `AddMessageSplitter` and `AddJsonMessageSplitter`
- `tests/UnitTests/SplitterOptionsTests.cs` — 8 tests for `SplitterOptions` defaults and values
- `tests/UnitTests/MessageSplitterTests.cs` — 20 tests covering payload split, envelope header propagation, MessageType/Source override, broker publish, result record, guard clauses, metadata isolation, and zero-item split
- `tests/UnitTests/JsonArraySplitStrategyTests.cs` — 10 tests covering top-level array, named array property, scalar arrays, empty arrays, error cases, and element independence

### Files modified

- `tests/UnitTests/UnitTests.csproj` — Added `<ProjectReference>` to `Processing.Splitter`
- `EnterpriseIntegrationPlatform.sln` — Added `Processing.Splitter` project
- `rules/milestones.md` — Marked chunk 014 as done, updated Next Chunk to 015
- `rules/completion-log.md` — This entry

### Notes

- All 221 unit tests pass (38 new + 183 pre-existing). Build: 0 warnings, 0 errors.
- `MessageSplitter<T>` is fully generic — any payload type is supported. The `JsonArraySplitStrategy` specialises for `JsonElement` scenarios common in HTTP connector integrations.
- Each split envelope gets its own deep copy of `Metadata` to prevent mutation side effects between split items.
- `JsonArraySplitStrategy` clones each array element via `JsonSerializer.SerializeToElement` to ensure elements are independent of the source `JsonDocument` lifetime.
- Zero-item splits produce a warning log and return an empty result — no messages are published. This prevents accidental silent drops while providing diagnostics.
- `SplitterServiceExtensions.AddMessageSplitter` and `AddJsonMessageSplitter` require an `IMessageBrokerProducer` to already be registered — consistent with the dependency inversion pattern used by `AddContentBasedRouter` and `AddMessageTranslator`.


## Chunk 013 – Message Translator

- **Date**: 2026-03-15
- **Status**: done
- **Goal**: Implement the Message Translator EIP pattern in a new `Processing.Translator` project, advancing Quality Pillars 1 (Reliability — causation chain preserved), 4 (Maintainability — composable transform abstraction), and 10 (Testability — pure unit-testable transform logic).

### Architecture

The Message Translator is a standalone class library (`Processing.Translator`) that transforms `IntegrationEnvelope<TIn>` messages into `IntegrationEnvelope<TOut>` using a pluggable `IPayloadTransform<TIn, TOut>` and publishes the translated envelope to a configured target topic via `IMessageBrokerProducer`.

**Translation flow:**
1. The `MessageTranslator<TIn, TOut>` receives a source envelope and validates the `TargetTopic` configuration.
2. The injected `IPayloadTransform<TIn, TOut>` performs the domain-specific payload transformation.
3. A new envelope is created, preserving `CorrelationId`, `Priority`, `SchemaVersion`, and `Metadata` from the source. `CausationId` is set to `source.MessageId` to maintain the full causation chain.
4. `MessageType` and `Source` on the translated envelope are overridden when `TranslatorOptions.TargetMessageType` / `TargetSource` are configured; otherwise they are inherited from the source.
5. The translated envelope is published to `TranslatorOptions.TargetTopic` via `IMessageBrokerProducer`.
6. A `TranslationResult<TOut>` record is returned containing the translated envelope, source message ID, and target topic for observability and downstream use.

**Provided `IPayloadTransform<TIn, TOut>` implementations:**
- `FuncPayloadTransform<TIn, TOut>` — Wraps a caller-supplied `Func<TIn, TOut>` delegate. Use for inline or lambda-based transformations.
- `JsonFieldMappingTransform` — Maps fields from a source `JsonElement` to a new `JsonElement` using a list of `FieldMapping` records from `TranslatorOptions.FieldMappings`. Supports dot-separated source and target paths (nested objects created automatically), static value injection, and graceful handling of missing source fields (key omitted from target).

**`TranslatorOptions`** (bound from `MessageTranslator` configuration section):
- `TargetTopic` — Required. Topic to publish the translated envelope to.
- `TargetMessageType` — Optional. Overrides the translated envelope's `MessageType`; when absent, the source `MessageType` is preserved.
- `TargetSource` — Optional. Overrides the translated envelope's `Source`; when absent, the source `Source` is preserved.
- `FieldMappings` — List of `FieldMapping` records used by `JsonFieldMappingTransform`.

**DI registration (`TranslatorServiceExtensions`):**
- `AddMessageTranslator<TIn, TOut>(IServiceCollection, IConfiguration, Func<TIn, TOut>)` — Registers with a delegate transform.
- `AddJsonMessageTranslator(IServiceCollection, IConfiguration)` — Registers a `JsonFieldMappingTransform`-backed JSON-to-JSON translator.

Both overloads require an `IMessageBrokerProducer` to already be registered (e.g. via `AddNatsJetStreamBroker`).

### Files created

- `src/Processing.Translator/Processing.Translator.csproj` — Class library; depends on `Contracts` and `Ingestion`
- `src/Processing.Translator/IPayloadTransform.cs` — Interface: `Transform(TIn) → TOut`
- `src/Processing.Translator/IMessageTranslator.cs` — Interface: `TranslateAsync(IntegrationEnvelope<TIn>) → TranslationResult<TOut>`
- `src/Processing.Translator/TranslationResult.cs` — Result record: `TranslatedEnvelope`, `SourceMessageId`, `TargetTopic`
- `src/Processing.Translator/FieldMapping.cs` — Record: `SourcePath`, `TargetPath`, `StaticValue`
- `src/Processing.Translator/TranslatorOptions.cs` — Options: `TargetTopic`, `TargetMessageType`, `TargetSource`, `FieldMappings`
- `src/Processing.Translator/MessageTranslator.cs` — Production implementation; preserves causation chain; configurable type/source override
- `src/Processing.Translator/JsonFieldMappingTransform.cs` — JSON field mapping implementation; dot-path navigation; nested target creation; static value injection
- `src/Processing.Translator/FuncPayloadTransform.cs` — Delegate-based payload transform
- `src/Processing.Translator/TranslatorServiceExtensions.cs` — DI extensions `AddMessageTranslator` and `AddJsonMessageTranslator`
- `tests/UnitTests/TranslatorOptionsTests.cs` — 8 tests for `TranslatorOptions` defaults and values
- `tests/UnitTests/MessageTranslatorTests.cs` — 14 tests covering payload transform, envelope header propagation, MessageType/Source override, broker publish, result record, and guard clauses
- `tests/UnitTests/JsonFieldMappingTransformTests.cs` — 10 tests covering flat mapping, nested source/target paths, static value, missing source field, multiple mappings, numeric/boolean values, and empty mapping list

### Files modified

- `tests/UnitTests/UnitTests.csproj` — Added `<ProjectReference>` to `Processing.Translator`
- `EnterpriseIntegrationPlatform.sln` — Added `Processing.Translator` project with GUID `{B1000017-0000-0000-0000-000000000001}`
- `rules/milestones.md` — Marked chunk 013 as done, updated Next Chunk to 014
- `rules/completion-log.md` — This entry

### Notes

- All 183 unit tests pass (32 new + 151 pre-existing). Build: 0 warnings, 0 errors.
- `MessageTranslator<TIn, TOut>` is fully generic — any payload type pair is supported. The `JsonFieldMappingTransform` specialises for `JsonElement → JsonElement` scenarios common in HTTP connector integrations.
- `JsonFieldMappingTransform` creates intermediate JSON objects along multi-segment target paths automatically, so callers do not need to pre-create the target object hierarchy.
- Missing source path segments are silently skipped (key omitted from target), preventing `null` injection. Static values can be used to inject constants (e.g. schema version, tenant ID) into the target document without requiring a source field.
- `TranslatorServiceExtensions.AddMessageTranslator` and `AddJsonMessageTranslator` require an `IMessageBrokerProducer` to already be registered — consistent with the dependency inversion pattern used by `AddContentBasedRouter`.



- **Date**: 2026-03-15
- **Status**: done
- **Goal**: Implement the Content-Based Router EIP pattern in a new `Processing.Routing` project, advancing Quality Pillars 1 (Reliability — deterministic routing), 4 (Maintainability — rule-driven extensibility), and 11 (Performance — pre-sorted rule list evaluated at O(n)).

### Architecture

The Content-Based Router is a standalone class library (`Processing.Routing`) that evaluates `IntegrationEnvelope<T>` messages against a prioritised list of `RoutingRule` objects and publishes the envelope to the selected topic via `IMessageBrokerProducer`.

**Routing flow:**
1. Rules are pre-sorted in ascending `Priority` order at construction time (one allocation, zero per-message sort).
2. For each rule the router extracts the configured field value from the envelope.
3. The field value is tested against the rule's `Operator` and `Value`.
4. The first matching rule determines the `TargetTopic`; the envelope is published there.
5. If no rule matches and `RouterOptions.DefaultTopic` is set, the message is routed to the default topic.
6. If no rule matches and no default is configured, `InvalidOperationException` is thrown (prevents silent message loss).

**Supported field names:**
- `MessageType` — the envelope's `MessageType` header
- `Source` — the envelope's `Source` header
- `Priority` — string representation of the envelope's `Priority` enum value
- `Metadata.{key}` — a value from the envelope's `Metadata` dictionary
- `Payload.{dot.path}` — a value from the JSON payload (dot-separated path; only for `JsonElement` payloads)

**Supported operators:** `Equals`, `Contains`, `StartsWith`, `Regex` — all case-insensitive.

**Result:** `RoutingDecision` record carrying `TargetTopic`, `MatchedRule` (nullable), and `IsDefault` for observability and diagnostics.

### Files created

- `src/Processing.Routing/Processing.Routing.csproj` — Class library; depends on `Contracts` and `Ingestion`
- `src/Processing.Routing/RoutingOperator.cs` — Enum: `Equals`, `Contains`, `StartsWith`, `Regex`
- `src/Processing.Routing/RoutingRule.cs` — Record: `Priority`, `FieldName`, `Operator`, `Value`, `TargetTopic`, `Name`
- `src/Processing.Routing/RouterOptions.cs` — Options: `Rules`, `DefaultTopic`; bound from `ContentBasedRouter` config section
- `src/Processing.Routing/RoutingDecision.cs` — Result record: `TargetTopic`, `MatchedRule`, `IsDefault`
- `src/Processing.Routing/IContentBasedRouter.cs` — Interface: `RouteAsync<T>`
- `src/Processing.Routing/ContentBasedRouter.cs` — Production implementation; pre-sorted rules; JSON path navigation; structured logging of routing decisions
- `src/Processing.Routing/RoutingServiceExtensions.cs` — DI extension `AddContentBasedRouter(IServiceCollection, IConfiguration)`
- `tests/UnitTests/RouterOptionsTests.cs` — 4 tests for `RouterOptions` defaults and values
- `tests/UnitTests/ContentBasedRouterTests.cs` — 15 tests covering all operators, priority ordering, default fallback, metadata routing, payload JSON routing, producer called, and null guard

### Files modified

- `tests/UnitTests/UnitTests.csproj` — Added `<ProjectReference>` to `Processing.Routing`
- `EnterpriseIntegrationPlatform.sln` — Added `Processing.Routing` project with GUID `{B1000016-0000-0000-0000-000000000001}`
- `rules/milestones.md` — Marked chunk 012 as done, updated Next Chunk to 013
- `rules/completion-log.md` — This entry

### Notes

- All 151 unit tests pass (19 new + 132 pre-existing). Build: 0 warnings, 0 errors.
- `ContentBasedRouter` sorts rules once at construction (`_sortedRules`) — no per-message allocation or LINQ sort on the hot path.
- Regex patterns are compiled each invocation via `Regex.IsMatch` with `RegexOptions.IgnoreCase | RegexOptions.CultureInvariant`. For high-throughput deployments, callers may pre-compile patterns by caching `Regex` instances in `RoutingRule.Name` or a future extension.
- `Payload.{path}` field extraction returns `null` (non-matching) for non-`JsonElement` payloads, so rules that target payload fields are safely ignored for non-JSON messages.
- `RoutingServiceExtensions.AddContentBasedRouter` requires an `IMessageBrokerProducer` to already be registered (e.g., via `AddNatsJetStreamBroker`) — this matches the dependency inversion pattern used across the platform.



- **Date**: 2026-03-15
- **Status**: done
- **Goal**: Wire all platform components into a working end-to-end demo pipeline, advancing Quality Pillars 1 (Reliability — zero message loss), 6 (Resilience — Ack/Nack loopback), 8 (Observability — lifecycle recording), and 10 (Testability — unit-testable orchestrator).

### Architecture

The demo pipeline is a standalone .NET Worker Service (`Demo.Pipeline`) that subscribes to an inbound NATS JetStream subject and routes each message through the full platform stack. The pipeline implements the Ack/Nack notification loopback pattern required by the architecture rules: every accepted message is either delivered (Ack) or permanently recorded as a fault (Nack) — no silent drops.

**Pipeline flow (per message):**
1. **Persist** — Save `MessageRecord` to Cassandra as `DeliveryStatus.Pending`.
2. **Record Received** — Emit a lifecycle event to `MessageLifecycleRecorder` (Loki + OTel).
3. **Dispatch** — Start `ProcessIntegrationMessageWorkflow` via the Temporal client using the string-based API; await the result.
4. **On success** — Update Cassandra status to `Delivered`, record `Delivered` event, publish Ack envelope to `integration.ack`.
5. **On validation failure** — Update Cassandra status to `Failed`, persist `FaultEnvelope`, record `Failed` event, publish Nack envelope to `integration.nack`.
6. **On exception** — Same as failure path; internal try/catch ensures the worker stays alive for subsequent messages.

**Workflow input/output records moved to `Activities`:** `ProcessIntegrationMessageInput` and `ProcessIntegrationMessageResult` were moved from `Workflow.Temporal` to `Activities` so that both the Temporal worker and the pipeline client can reference them without a circular project dependency. `Workflow.Temporal` and `Demo.Pipeline` both reference `Activities`.

### Files created

- `src/Activities/ProcessIntegrationMessageInput.cs` — Workflow input record (moved from Workflow.Temporal; now in the shared Activities contract assembly)
- `src/Activities/ProcessIntegrationMessageResult.cs` — Workflow result record (moved from Workflow.Temporal)
- `src/Demo.Pipeline/Demo.Pipeline.csproj` — Worker SDK project; references ServiceDefaults, Contracts, Activities, Ingestion, Ingestion.Nats, Storage.Cassandra, Observability, Temporalio
- `src/Demo.Pipeline/PipelineOptions.cs` — Configuration record: NatsUrl, InboundSubject, AckSubject, NackSubject, ConsumerGroup, TemporalServerAddress, TemporalNamespace, TemporalTaskQueue, WorkflowTimeout
- `src/Demo.Pipeline/IPipelineOrchestrator.cs` — Interface for single-message pipeline processing
- `src/Demo.Pipeline/PipelineOrchestrator.cs` — Production orchestrator: persist → dispatch → Ack/Nack → update status; fault-safe with internal try/catch on every external call
- `src/Demo.Pipeline/ITemporalWorkflowDispatcher.cs` — Interface for Temporal workflow dispatch
- `src/Demo.Pipeline/TemporalWorkflowDispatcher.cs` — Lazy-connected singleton Temporal client; uses string-based workflow dispatch; thread-safe via SemaphoreSlim
- `src/Demo.Pipeline/IntegrationPipelineWorker.cs` — BackgroundService that subscribes to NATS JetStream and delegates to IPipelineOrchestrator; stays alive after orchestrator errors
- `src/Demo.Pipeline/NotificationPayloads.cs` — `AckPayload` and `NackPayload` records for Ack/Nack envelope payloads
- `src/Demo.Pipeline/PipelineServiceExtensions.cs` — DI extension `AddDemoPipeline`: registers NATS, Cassandra, Observability, Temporal dispatcher, orchestrator, and hosted worker
- `src/Demo.Pipeline/Program.cs` — Worker host; calls `AddDemoPipeline`
- `src/Demo.Pipeline/appsettings.json` — Default config (all Pipeline, Cassandra, and Loki settings)
- `src/Demo.Pipeline/appsettings.Development.json` — Debug log level override
- `src/Demo.Pipeline/Properties/launchSettings.json` — Local dev profile
- `tests/UnitTests/PipelineOptionsTests.cs` — 11 tests for all PipelineOptions defaults and custom values
- `tests/UnitTests/PipelineOrchestratorTests.cs` — 9 tests: valid/invalid/exception paths; verifies Cassandra saves, status updates, Ack/Nack publishing, fault persistence, workflow input
- `tests/UnitTests/ProcessIntegrationMessageContractTests.cs` — 5 tests for moved contract types and notification payloads

### Files modified

- `src/Workflow.Temporal/Workflows/ProcessIntegrationMessageWorkflow.cs` — Removed inline record definitions (moved to Activities); uses `EnterpriseIntegrationPlatform.Activities` namespace
- `src/AppHost/AppHost.csproj` — Added `<ProjectReference>` to Demo.Pipeline
- `src/AppHost/Program.cs` — Registered `Projects.Demo_Pipeline` as `demo-pipeline` with NATS, Temporal, Loki, and Cassandra environment injection
- `tests/UnitTests/UnitTests.csproj` — Added `<ProjectReference>` to Demo.Pipeline
- `EnterpriseIntegrationPlatform.sln` — Added Demo.Pipeline project with GUID `{B1000015-0000-0000-0000-000000000001}`
- `rules/milestones.md` — Marked chunk 011 as done, updated Next Chunk to 012
- `rules/completion-log.md` — This entry

### Notes

- All 132 unit tests pass (26 new + 106 pre-existing). All 20 workflow tests pass. Build: 0 warnings, 0 errors.
- The `ProcessIntegrationMessageWorkflow` is dispatched using the Temporal string-based API (`"ProcessIntegrationMessageWorkflow"` as workflow type name) — avoids a project reference from `Demo.Pipeline` to `Workflow.Temporal`.
- `TemporalWorkflowDispatcher` creates a lazy singleton `TemporalClient`; re-uses the same connection for all messages; protected by `SemaphoreSlim` for thread safety.
- All external calls in `PipelineOrchestrator` (Cassandra, Loki, NATS) are wrapped in try/catch so a single-component failure does not prevent Nack publishing or fault recording.
- `IntegrationPipelineWorker` catches non-cancellation exceptions from the orchestrator and logs them without crashing — the worker continues consuming subsequent messages.
- `AckPayload` and `NackPayload` are published as `IntegrationEnvelope<T>` with the correlation and causation IDs set, satisfying the Ack/Nack notification loopback requirement.

## Chunk 010 – Admin API

- **Date**: 2026-03-15
- **Status**: done
- **Goal**: Build a production-ready administration API for platform management, advancing Quality Pillars 2 (Security) and 7 (Supportability).

### Architecture

The Admin API is a standalone ASP.NET Core Web API registered in Aspire AppHost alongside OpenClaw.Web. It is protected by API key authentication and per-key rate limiting, and exposes endpoints for:

1. **Platform status** — runs all registered health checks (including Cassandra) and returns an aggregated `Healthy / Degraded / Unhealthy` summary.
2. **Message queries** — look up `MessageRecord`s from Cassandra by correlation ID or message ID.
3. **Message status update** — change the `DeliveryStatus` of a message in Cassandra (e.g. force-DLQ or re-queue).
4. **Fault queries** — retrieve `FaultEnvelope`s from Cassandra by correlation ID.
5. **Observability event queries** — query the Loki-backed `IObservabilityEventLog` by correlation ID or business key.

All endpoints require the `X-Api-Key` header. API keys are stored in configuration (`AdminApi:ApiKeys`) — never in source code. A development convenience key is set in `appsettings.Development.json`.

**Security measures implemented:**
- API key authentication (`ApiKeyAuthenticationHandler`) with ordinal key comparison.
- API keys are masked (first 4 chars + `****`) in all audit log entries.
- Fixed-window rate limiting (per API key, or per IP as fallback) with HTTP 429 on excess.
- All admin operations are logged to the structured audit trail (flows to Loki for compliance).

### Endpoint Summary

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/admin/status` | Aggregated platform health |
| GET | `/api/admin/messages/correlation/{correlationId}` | Messages by correlation ID |
| GET | `/api/admin/messages/{messageId}` | Single message by ID |
| PATCH | `/api/admin/messages/{messageId}/status` | Update delivery status |
| GET | `/api/admin/faults/correlation/{correlationId}` | Faults by correlation ID |
| GET | `/api/admin/events/correlation/{correlationId}` | Observability events by correlation ID |
| GET | `/api/admin/events/business/{businessKey}` | Observability events by business key |

- **Files created**:
  - `src/Admin.Api/Admin.Api.csproj` — ASP.NET Core Web project; references ServiceDefaults, Observability, Storage.Cassandra
  - `src/Admin.Api/AdminApiOptions.cs` — configuration record (`AdminApi:ApiKeys`, `AdminApi:RateLimitPerMinute`)
  - `src/Admin.Api/Authentication/ApiKeyAuthenticationHandler.cs` — custom `AuthenticationHandler<AuthenticationSchemeOptions>`; validates `X-Api-Key` header; grants `Admin` role
  - `src/Admin.Api/Services/PlatformStatusService.cs` — aggregates `HealthCheckService.CheckHealthAsync`; returns `PlatformStatusResult` + `ComponentStatus` records
  - `src/Admin.Api/Services/AdminAuditLogger.cs` — structured audit log via `ILogger<T>`; masks API key prefix; flows to Loki
  - `src/Admin.Api/Program.cs` — full API host: 7 admin endpoints, authentication, rate limiting, Cassandra + Loki integration
  - `src/Admin.Api/appsettings.json` — default config with empty ApiKeys list and Cassandra/Loki defaults
  - `src/Admin.Api/appsettings.Development.json` — development convenience key + elevated rate limit
  - `src/Admin.Api/Properties/launchSettings.json` — local dev profile on port 5200
  - `tests/UnitTests/AdminApiOptionsTests.cs` — 5 tests for options defaults and key list semantics
  - `tests/UnitTests/AdminAuditLoggerTests.cs` — 4 tests for audit logging with various principal states
  - `tests/UnitTests/PlatformStatusServiceTests.cs` — 5 tests for status aggregation, exception handling, and field population
- **Files modified**:
  - `EnterpriseIntegrationPlatform.sln` — added Admin.Api project with GUID `{B1000014-0000-0000-0000-000000000001}`
  - `src/AppHost/AppHost.csproj` — added `<ProjectReference>` to Admin.Api
  - `src/AppHost/Program.cs` — registered `Projects.Admin_Api` as `admin-api` with `WithExternalHttpEndpoints()`, Loki + Cassandra environment injection
  - `tests/UnitTests/UnitTests.csproj` — added `<ProjectReference>` to Admin.Api
  - `rules/milestones.md` — marked chunk 010 as done, updated Next Chunk to 011
  - `rules/completion-log.md` — this entry
- **Notes**:
  - All 106 unit tests pass (14 new + 92 pre-existing). Build: 0 warnings, 0 errors.
  - Rate limiting uses `System.Threading.RateLimiting` (built-in, no extra NuGet package) with `PartitionedRateLimiter.Create` keyed by API key or remote IP.
  - `HealthCheckService.CheckHealthAsync(null, cancellationToken)` is called directly (abstract overload) to enable NSubstitute mocking in unit tests.
  - Admin.Api is intentionally decoupled from AI.Ollama and AI.RagFlow — those are OpenClaw concerns. Admin focuses on operational management.
  - `/health` and `/alive` endpoints (from `MapDefaultEndpoints`) remain public; only `/api/admin/*` endpoints require authentication.

## Chunk 007 – Cassandra Storage Module

- **Date**: 2026-03-15
- **Status**: done
- **Goal**: Implement Cassandra repository and data access layer for scalable distributed persistence. Provides durable storage for message records, fault envelopes, and delivery status tracking. Satisfies Quality Pillar 1 (Reliability) with RF=3, Pillar 3 (Scalability) with distributed NoSQL storage, and Pillar 11 (Performance) with denormalised tables and TTL-based cleanup.

### Files created

- `src/Storage.Cassandra/Storage.Cassandra.csproj` — Project file (depends on Contracts, CassandraCSharpDriver 3.22.0, OpenTelemetry)
- `src/Storage.Cassandra/CassandraOptions.cs` — Configuration (ContactPoints, Port 15042, Keyspace, RF=3, TTL 30d)
- `src/Storage.Cassandra/ICassandraSessionFactory.cs` — Factory interface for Cassandra session lifecycle
- `src/Storage.Cassandra/CassandraSessionFactory.cs` — Manages Cluster/ISession with lazy thread-safe initialisation
- `src/Storage.Cassandra/SchemaManager.cs` — Idempotent keyspace and table creation (messages_by_correlation_id, messages_by_id, faults_by_correlation_id)
- `src/Storage.Cassandra/MessageRecord.cs` — Denormalised message record for Cassandra storage
- `src/Storage.Cassandra/IMessageRepository.cs` — Repository interface for message/fault persistence and queries
- `src/Storage.Cassandra/CassandraMessageRepository.cs` — Production Cassandra implementation with batch writes and OpenTelemetry traces
- `src/Storage.Cassandra/CassandraDiagnostics.cs` — Dedicated ActivitySource and Meter for storage telemetry
- `src/Storage.Cassandra/CassandraHealthCheck.cs` — Health check verifying Cassandra connectivity
- `src/Storage.Cassandra/CassandraServiceExtensions.cs` — DI registration (session factory, repository, health check, OTel)
- `tests/UnitTests/CassandraOptionsTests.cs` — 8 tests for configuration defaults and binding
- `tests/UnitTests/MessageRecordTests.cs` — 6 tests for record defaults and property assignment
- `tests/UnitTests/CassandraDiagnosticsTests.cs` — 6 tests for OpenTelemetry source/meter configuration
- `tests/UnitTests/CassandraHealthCheckTests.cs` — 3 tests for healthy/unhealthy scenarios
- `tests/UnitTests/CassandraServiceExtensionsTests.cs` — 4 tests for DI registration and options binding
- `tests/UnitTests/CassandraMessageRepositoryTests.cs` — 7 tests for repository operations with mocked session

### Files modified

- `Directory.Packages.props` — Added CassandraCSharpDriver 3.22.0 and Newtonsoft.Json 13.0.4 (override for GHSA-5crp-9r3c-p9vr)
- `EnterpriseIntegrationPlatform.sln` — Added Storage.Cassandra project
- `src/AppHost/Program.cs` — Added Cassandra container (cassandra:5.0, host port 15042, target 9042)
- `tests/UnitTests/UnitTests.csproj` — Added Storage.Cassandra project reference
- `rules/milestones.md` — Chunk 007 → done, Next Chunk → 010
- `rules/completion-log.md` — This entry

### Port mapping (updated)

| Service | Host Port | Container Port |
|---------|-----------|----------------|
| Cassandra CQL | 15042 | 9042 |

### Cassandra table design

- `messages_by_correlation_id` — Partition: correlation_id, Clustering: recorded_at ASC, message_id ASC
- `messages_by_id` — Partition: message_id (single-row lookup)
- `faults_by_correlation_id` — Partition: correlation_id, Clustering: faulted_at DESC, fault_id ASC

### Test results

- ContractTests: 29 passed
- UnitTests: 92 passed (58 existing + 34 new Cassandra tests)
- WorkflowTests: 20 passed
- Total: 141 passed, 0 failed

## Self-Hosted GraphRAG + Non-Common Aspire Ports

- **Date**: 2026-03-15
- **Status**: done
- **Goal**: Add self-hosted GraphRAG system (RagFlow + Ollama) to the Aspire project so developers can retrieve platform knowledge from any client machine and use their own AI provider for code generation. Change all Aspire container host ports to non-common 15xxx range to avoid conflicts with existing services.

### Files created

- `src/AI.RagFlow/AI.RagFlow.csproj` — Project file
- `src/AI.RagFlow/IRagFlowService.cs` — Interface for RAG retrieval, chat, dataset listing, health
- `src/AI.RagFlow/RagFlowService.cs` — Production HTTP client for RagFlow REST API
- `src/AI.RagFlow/RagFlowOptions.cs` — Configuration (BaseAddress, ApiKey, AssistantId)
- `src/AI.RagFlow/RagFlowServiceExtensions.cs` — DI registration + health check
- `src/AI.RagFlow/RagFlowHealthCheck.cs` — Health check for RagFlow availability
- `tests/UnitTests/RagFlowServiceTests.cs` — 11 unit tests for RagFlow service

### Files modified

- `src/AppHost/Program.cs` — All containers use non-common host ports (15xxx range); RagFlow endpoint passed to OpenClaw
- `src/OpenClaw.Web/Program.cs` — Register RagFlow service; add generation endpoints (POST /api/generate/integration, POST /api/generate/chat, GET /api/generate/datasets, GET /api/health/ragflow); IntegrationPromptBuilder
- `src/OpenClaw.Web/OpenClaw.Web.csproj` — Added AI.RagFlow project reference
- `src/AI.Ollama/OllamaServiceExtensions.cs` — Default port changed to 15434
- `src/AI.Ollama/OllamaService.cs` — Doc comment updated
- `src/Workflow.Temporal/TemporalOptions.cs` — Default port changed to 15233
- `src/Observability/ObservabilityServiceExtensions.cs` — Doc comment updated
- `src/Observability/LokiObservabilityEventLog.cs` — Doc comment updated
- `src/Ingestion/BrokerOptions.cs` — Doc comment updated
- `src/Ingestion.Nats/NatsServiceExtensions.cs` — Doc comment updated
- `src/OpenClaw.Web/appsettings.Development.json` — Ollama address updated to 15434
- `rules/architecture-rules.md` — Added principles 8 (Self-Hosted GraphRAG) and 9 (Non-Common Ports)
- `rules/milestones.md` — Added GraphRAG vision, updated chunk 009 description, non-common ports
- `rules/quality-pillars.md` — Added GraphRAG to design philosophy
- `docs/ai-strategy.md` — Added self-hosted GraphRAG section with architecture diagram and port table
- `docs/operations-runbook.md` — Updated port references
- `tests/UnitTests/UnitTests.csproj` — Added AI.RagFlow project reference
- `tests/WorkflowTests/SampleTest.cs` — Updated expected default port to 15233

### Port mapping (15xxx range)

| Service | Host Port | Container Port |
|---------|-----------|----------------|
| Ollama | 15434 | 11434 |
| RagFlow UI | 15080 | 80 |
| RagFlow API | 15380 | 9380 |
| Loki | 15100 | 3100 |
| Temporal gRPC | 15233 | 7233 |
| Temporal UI | 15280 | 8080 |
| NATS | 15222 | 4222 |

### Test results

- ContractTests: 29 passed
- UnitTests: 58 passed (47 existing + 11 new RagFlow tests)
- WorkflowTests: 20 passed
- IntegrationTests: 17 passed
- PlaywrightTests: 13 passed
- LoadTests: 1 passed
- **Total: 138 tests, 0 failures**

## Reality Filter Enforcement – Production-Ready Cleanup

- **Date**: 2026-03-15
- **Status**: done
- **Goal**: Remove ALL pretend, demo, hacky, and conceptual code from the repository. Enforce rule that every committed file must be production-ready.

### What was removed and why

**Toy EIP pattern implementations** (22 files in Processing.Routing + Processing.Transform):
Removed ContentBasedRouter, MessageFilter, RecipientList, Splitter, Aggregator, ScatterGather,
RoutingSlip, DynamicRouter, PipelineBuilder, WireTap, PublishSubscribeChannel, IdempotentReceiver,
Resequencer, RetryHandler, CircuitBreaker, IMessageRouter, MessageTranslator, ContentEnricher,
ContentFilter, ClaimCheck, Normalizer, IMessageTransformer. These had race conditions, no thread
safety, no persistence, no logging, no error handling — in-memory-only conceptual code that would
fail under any production load. The patterns are correctly scheduled as separate chunks (012-018)
where they will get proper production implementations using battle-tested libraries.

**PatternDemoTests** (24 files): Tests for the removed toy implementations.

**Interface-only projects** (6 projects with no implementations):
- Connector.Email, Connector.File, Connector.Http, Connector.Sftp — scheduled for chunks 019-022
- Storage.Cassandra — scheduled for chunk 007
- RuleEngine — to be implemented in a dedicated chunk

**Stub Program.cs files** (3 files):
- Admin.Api, Admin.Web, Gateway.Api — just health-check endpoints with no real functionality. Scheduled for chunk 010.

**BaseActivity** (abstract class): No subclasses anywhere in the codebase.

### Rules updated
- `rules/reality-filter.md` — added comprehensive "All Code Must Be Production-Ready" section
- `rules/coding-standards.md` — added same rules (no pretend, no demo, no hacky, no interface-only projects, no stub Program.cs files)

### Files remaining (53 .cs source files) — all verified as production-quality
- AI.Ollama (4): Real Ollama HTTP client with health checks
- Activities (3): Real validation and logging services with ILogger
- AppHost (1): Real Aspire orchestration with pinned container versions
- Contracts (5): Real message envelope contracts
- Ingestion (6): Real broker abstractions with Kafka/NATS/Pulsar implementations
- Ingestion.Kafka (4): Real Confluent.Kafka producer/consumer
- Ingestion.Nats (3): Real NATS.Net JetStream producer/consumer
- Ingestion.Pulsar (3): Real DotPulsar producer/consumer
- Observability (16): Real Loki-backed observability with OpenTelemetry
- OpenClaw.Web (2): Real web UI with Loki queries and Ollama AI
- ServiceDefaults (1): Real Aspire service defaults
- Workflow.Temporal (5): Real Temporalio workflows with typed activities

### Test results after cleanup
- ContractTests: 29 passed
- UnitTests: 47 passed
- WorkflowTests: 20 passed
- IntegrationTests: 17 passed
- PlaywrightTests: 13 passed
- LoadTests: 1 passed
- **Total: 127 tests, 0 failures**

## Chunk 006 – Temporal workflow host

- **Date**: 2026-03-15
- **Status**: done
- **Goal**: Set up Temporal workflow worker, implement all BizTalk and Enterprise Integration Patterns (EIP), create a dedicated test project demonstrating each pattern, and enforce Reality Filter rules (no stubs, no speculative content, no empty interfaces).

### Architecture

```
Temporal Workflow Host (src/Workflow.Temporal/):
  TemporalOptions          → configuration: ServerAddress, Namespace, TaskQueue
  TemporalServiceExtensions → DI registration for Temporal worker
  IntegrationActivities    → Temporal [Activity] wrappers delegating to Activities services
  ProcessIntegrationMessageWorkflow → sample workflow: validate → log lifecycle stages

Activities (src/Activities/):
  IMessageValidationService + DefaultMessageValidationService → payload validation
  IMessageLoggingService + DefaultMessageLoggingService       → lifecycle stage logging

Aspire AppHost containers:
  temporal (temporalio/auto-setup:latest) → workflow server with auto namespace setup
  temporal-ui (temporalio/ui:latest)      → web UI for workflow inspection

Enterprise Integration Patterns (src/Processing.Routing/, src/Processing.Transform/):

  Message Routing:
    ContentBasedRouter<T>        → routes by message content (BizTalk filter expressions)
    MessageFilter<T>             → predicate-based message filtering
    RecipientList<T>             → dynamic multi-destination routing
    MessageSplitter<T,TItem>     → debatching / composite message splitting
    CountBasedAggregator<T>      → correlated message aggregation (Convoy pattern)
    ScatterGather<TReq,TReply>   → parallel scatter + gather results
    RoutingSlip<T>               → sequential itinerary-based processing
    DynamicRouter<T>             → runtime-configurable routing rules (BRE)
    Pipeline<T>                  → Pipes and Filters (BizTalk pipeline stages)
    InMemoryWireTap<T>           → non-invasive message monitoring
    PublishSubscribeChannel<T>   → broadcast to multiple subscribers
    IdempotentReceiver<T>        → at-most-once message processing
    Resequencer<T>               → reorder out-of-sequence messages
    RetryHandler                 → exponential back-off retry logic
    CircuitBreaker               → failure threshold + auto-recovery

  Message Transformation:
    MessageTranslator<TIn,TOut>  → format conversion (BizTalk Maps)
    ContentEnricher<T>           → augment with external data
    ContentFilter<TIn,TOut>      → remove/normalize fields
    InMemoryClaimCheckStore      → large payload external storage
    MessageNormalizer<T>         → multi-format → canonical conversion

  Already Implemented (Contracts):
    IntegrationEnvelope<T>       → Envelope Wrapper + Canonical Data Model
    FaultEnvelope                → Dead Letter Channel
    CorrelationId/CausationId    → Correlation Identifier
    MessagePriority              → Priority-based processing
    MessageHeaders               → Property Promotion (BizTalk promoted properties)
    DeliveryStatus               → Message lifecycle states
```

- **Files created**:
  - `src/Workflow.Temporal/TemporalOptions.cs` — configuration options (Temporal section)
  - `src/Workflow.Temporal/TemporalServiceExtensions.cs` — DI registration with Temporalio.Extensions.Hosting
  - `src/Workflow.Temporal/Activities/IntegrationActivities.cs` — Temporal activity wrappers
  - `src/Workflow.Temporal/Workflows/ProcessIntegrationMessageWorkflow.cs` — sample validation workflow
  - `src/Activities/IMessageValidationService.cs` — validation interface + MessageValidationResult
  - `src/Activities/DefaultMessageValidationService.cs` — JSON validation implementation
  - `src/Activities/IMessageLoggingService.cs` — logging interface + DefaultMessageLoggingService
  - `src/Processing.Routing/ContentBasedRouter.cs` — content-based routing
  - `src/Processing.Routing/MessageFilter.cs` — predicate message filter
  - `src/Processing.Routing/RecipientList.cs` — dynamic recipient list
  - `src/Processing.Routing/Splitter.cs` — message splitter / debatcher
  - `src/Processing.Routing/Aggregator.cs` — count-based message aggregator
  - `src/Processing.Routing/ScatterGather.cs` — parallel scatter-gather
  - `src/Processing.Routing/RoutingSlip.cs` — itinerary-based routing slip
  - `src/Processing.Routing/DynamicRouter.cs` — runtime-configurable router
  - `src/Processing.Routing/PipelineBuilder.cs` — pipes and filters pipeline
  - `src/Processing.Routing/WireTap.cs` — non-invasive message monitoring
  - `src/Processing.Routing/PublishSubscribeChannel.cs` — pub/sub channel
  - `src/Processing.Routing/IdempotentReceiver.cs` — at-most-once processing
  - `src/Processing.Routing/Resequencer.cs` — message resequencing
  - `src/Processing.Routing/RetryHandler.cs` — retry with exponential back-off
  - `src/Processing.Routing/CircuitBreaker.cs` — circuit breaker pattern
  - `src/Processing.Transform/MessageTranslator.cs` — format translator
  - `src/Processing.Transform/ContentEnricher.cs` — content enrichment
  - `src/Processing.Transform/ContentFilter.cs` — content filtering
  - `src/Processing.Transform/ClaimCheck.cs` — claim check store
  - `src/Processing.Transform/Normalizer.cs` — multi-format normalizer
  - `tests/PatternDemoTests/PatternDemoTests.csproj` — pattern demo test project
  - `tests/PatternDemoTests/ContentBasedRouterTests.cs` — 3 content-based router demos
  - `tests/PatternDemoTests/MessageFilterTests.cs` — 3 message filter demos
  - `tests/PatternDemoTests/RecipientListTests.cs` — 2 recipient list demos
  - `tests/PatternDemoTests/SplitterTests.cs` — 3 splitter demos
  - `tests/PatternDemoTests/AggregatorTests.cs` — 2 aggregator demos
  - `tests/PatternDemoTests/ScatterGatherTests.cs` — 1 scatter-gather demo
  - `tests/PatternDemoTests/RoutingSlipTests.cs` — 2 routing slip demos
  - `tests/PatternDemoTests/DynamicRouterTests.cs` — 2 dynamic router demos
  - `tests/PatternDemoTests/PipelineTests.cs` — 2 pipes and filters demos
  - `tests/PatternDemoTests/WireTapTests.cs` — 2 wire tap demos
  - `tests/PatternDemoTests/PublishSubscribeTests.cs` — 2 pub/sub demos
  - `tests/PatternDemoTests/IdempotentReceiverTests.cs` — 3 idempotent receiver demos
  - `tests/PatternDemoTests/ResequencerTests.cs` — 1 resequencer demo
  - `tests/PatternDemoTests/RetryHandlerTests.cs` — 3 retry handler demos
  - `tests/PatternDemoTests/CircuitBreakerTests.cs` — 4 circuit breaker demos
  - `tests/PatternDemoTests/MessageTranslatorTests.cs` — 1 translator demo
  - `tests/PatternDemoTests/ContentEnricherTests.cs` — 1 enricher demo
  - `tests/PatternDemoTests/ContentFilterTests.cs` — 1 content filter demo
  - `tests/PatternDemoTests/ClaimCheckTests.cs` — 3 claim check demos
  - `tests/PatternDemoTests/NormalizerTests.cs` — 2 normalizer demos
  - `tests/PatternDemoTests/EnvelopeWrapperTests.cs` — 3 envelope wrapper demos
  - `tests/PatternDemoTests/DeadLetterChannelTests.cs` — 3 dead letter demos
  - `tests/PatternDemoTests/CorrelationIdentifierTests.cs` — 2 correlation demos
  - `tests/PatternDemoTests/MessagePriorityTests.cs` — 2 priority demos
  - `tests/WorkflowTests/SampleTest.cs` → renamed to TemporalOptionsTests (3 tests)
  - `tests/WorkflowTests/DefaultMessageValidationServiceTests.cs` — 7 validation tests
  - `tests/WorkflowTests/MessageValidationResultTests.cs` — 3 result tests
  - `tests/WorkflowTests/IntegrationActivitiesTests.cs` — 3 activity delegation tests
  - `tests/WorkflowTests/ProcessIntegrationMessageWorkflowTests.cs` — 4 workflow tests (skip when server unavailable)
- **Files modified**:
  - `Directory.Packages.props` — added Temporalio 1.11.1, Temporalio.Extensions.Hosting 1.11.1
  - `src/Workflow.Temporal/Workflow.Temporal.csproj` — added Temporalio, Activities, Contracts refs
  - `src/Workflow.Temporal/Program.cs` — wired Temporal worker via AddTemporalWorkflows
  - `src/Activities/Activities.csproj` — added Contracts project reference
  - `src/AppHost/Program.cs` — added Temporal server + UI containers
  - `src/Processing.Routing/Processing.Routing.csproj` — added Contracts reference
  - `src/Processing.Routing/IMessageRouter.cs` — expanded with typed Route<T> method
  - `src/Processing.Transform/Processing.Transform.csproj` — added Contracts reference
  - `src/Processing.Transform/IMessageTransformer.cs` — expanded with typed Transform method
  - `tests/WorkflowTests/WorkflowTests.csproj` — added Temporalio, project references
  - `EnterpriseIntegrationPlatform.sln` — added PatternDemoTests project
  - `rules/milestones.md` — chunk 006 → done, next chunk → 007
- **Test counts**:
  - WorkflowTests: 20 (was 1 placeholder, +19 new)
  - PatternDemoTests: 53 (new project)
  - ContractTests: 29 (unchanged)
  - UnitTests: 47 (unchanged)
  - IntegrationTests: 17 (unchanged)
  - PlaywrightTests: 13 (unchanged)
  - LoadTests: 1 (unchanged)
  - **Total: 180 tests, 0 failures, 0 warnings, 0 errors**

## Chunk 005 – Configurable message broker ingestion

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Implement broker abstraction with Kafka, NATS JetStream (default), and Pulsar (Key_Shared) providers for message ingestion.

### Architecture

```
Broker Abstraction Layer (src/Ingestion/):
  IMessageBrokerProducer → publishes IntegrationEnvelope<T> to a named topic
  IMessageBrokerConsumer → subscribes to a topic with consumer group semantics
  BrokerType enum        → NatsJetStream (0, default), Kafka (1), Pulsar (2)
  BrokerOptions          → deployment-time configuration (Broker section)
  EnvelopeSerializer     → JSON serialisation for broker transport

Provider Implementations:
  NATS JetStream (src/Ingestion.Nats/)   → per-subject independence, no HOL blocking (DEFAULT)
  Apache Kafka (src/Ingestion.Kafka/)    → broadcast streams, audit logs, fan-out analytics
  Apache Pulsar (src/Ingestion.Pulsar/)  → Key_Shared subscription, key-based distribution

Aspire AppHost:
  nats (nats:latest --jetstream) → default queue broker container
  Configuration: Broker:BrokerType + Broker:ConnectionString

Critical constraint: Recipient A must NOT block Recipient B, even at 1 million recipients.
  NATS: per-subject queue groups bypass HOL blocking
  Pulsar: Key_Shared distributes by correlationId key across consumers
```

- **Files created**:
  - `src/Ingestion/Ingestion.csproj` — broker abstraction library project
  - `src/Ingestion/BrokerType.cs` — enum: NatsJetStream, Kafka, Pulsar
  - `src/Ingestion/BrokerOptions.cs` — configuration options (Broker section)
  - `src/Ingestion/IMessageBrokerProducer.cs` — producer interface
  - `src/Ingestion/IMessageBrokerConsumer.cs` — consumer interface
  - `src/Ingestion/EnvelopeSerializer.cs` — JSON serialisation for envelopes
  - `src/Ingestion/IngestionServiceExtensions.cs` — AddBrokerOptions DI registration
  - `src/Ingestion.Nats/Ingestion.Nats.csproj` — NATS JetStream provider project
  - `src/Ingestion.Nats/NatsJetStreamProducer.cs` — NATS producer
  - `src/Ingestion.Nats/NatsJetStreamConsumer.cs` — NATS consumer with queue groups
  - `src/Ingestion.Nats/NatsServiceExtensions.cs` — AddNatsJetStreamBroker DI registration
  - `src/Ingestion.Pulsar/Ingestion.Pulsar.csproj` — Pulsar provider project
  - `src/Ingestion.Pulsar/PulsarProducer.cs` — Pulsar producer (keyed by correlationId)
  - `src/Ingestion.Pulsar/PulsarConsumer.cs` — Pulsar consumer with Key_Shared subscription
  - `src/Ingestion.Pulsar/PulsarServiceExtensions.cs` — AddPulsarBroker DI registration
  - `src/Ingestion.Kafka/KafkaProducer.cs` — Kafka producer
  - `src/Ingestion.Kafka/KafkaConsumer.cs` — Kafka consumer
  - `src/Ingestion.Kafka/KafkaServiceExtensions.cs` — AddKafkaBroker DI registration
  - `tests/UnitTests/EnvelopeSerializerTests.cs` — 6 serialisation tests
  - `tests/UnitTests/BrokerOptionsTests.cs` — 6 configuration tests
  - `tests/UnitTests/BrokerTypeTests.cs` — 4 enum tests
  - `tests/UnitTests/IngestionServiceExtensionsTests.cs` — 3 DI registration tests
  - `rules/reality-filter.md` — REALITY FILTER AI agent enforcement rules
- **Files modified**:
  - `Directory.Packages.props` — added NATS.Net 2.7.3, DotPulsar 5.2.2, Confluent.Kafka 2.13.2
  - `src/Ingestion.Kafka/Ingestion.Kafka.csproj` — added Confluent.Kafka + Ingestion project references
  - `src/Ingestion.Kafka/Program.cs` — wired broker options and KafkaBroker registration
  - `src/AppHost/Program.cs` — added NATS JetStream container (nats:latest --jetstream)
  - `EnterpriseIntegrationPlatform.sln` — added Ingestion, Ingestion.Nats, Ingestion.Pulsar projects
  - `tests/UnitTests/UnitTests.csproj` — added Ingestion project reference
  - `rules/milestones.md` — chunk 005 → done, next chunk → 006
- **Test counts**:
  - UnitTests: 47 (was 28, +19 broker tests)
  - ContractTests: 29 (unchanged)
  - Build: 0 warnings, 0 errors

## Chunk 009 – Remove InMemoryObservabilityEventLog, Loki-only observability

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Remove InMemoryObservabilityEventLog entirely. All observability uses real Loki storage via Aspire. No in-memory fallback.

### Architecture (Loki-only)

```
Aspire AppHost containers:
  loki (grafana/loki:3.4.2) → durable log storage for all lifecycle events, traces, status, metadata
  ollama (ollama/ollama)     → local LLM inference
  ragflow (infiniflow/ragflow) → RAG for integration docs

Observability storage:
  IObservabilityEventLog interface
  └── LokiObservabilityEventLog → real storage via Loki HTTP push API + LogQL queries

OpenClaw.Web:
  Always uses LokiObservabilityEventLog (Loki__BaseAddress from Aspire, defaults to localhost:3100)
```

- **Files deleted**:
  - `src/Observability/InMemoryObservabilityEventLog.cs` — removed in-memory fallback
  - `tests/UnitTests/InMemoryObservabilityEventLogTests.cs` — removed its test
- **Files modified**:
  - `src/Observability/ObservabilityServiceExtensions.cs` — removed parameterless `AddPlatformObservability()` overload, kept only `AddPlatformObservability(string lokiBaseUrl)`
  - `src/Observability/IObservabilityEventLog.cs` — updated doc to reference Loki only
  - `src/OpenClaw.Web/Program.cs` — removed conditional fallback, always uses Loki
  - `tests/UnitTests/MessageLifecycleRecorderTests.cs` — replaced InMemoryObservabilityEventLog with NSubstitute mock
  - `tests/UnitTests/MessageStateInspectorTests.cs` — replaced InMemoryObservabilityEventLog with NSubstitute mock
- **Test counts**:
  - UnitTests: 28 (was 29, -1 InMemory smoke test removed)
  - IntegrationTests: 9 (8 Loki tests + 1 placeholder)
  - Build: 0 warnings, 0 errors

## Chunk 009 – Loki-backed observability storage with real integration tests

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Replace in-memory-only observability tests with real Loki storage. InMemoryObservabilityEventLog should have only 1 test; all behavioural tests must use real storage via Testcontainers. Loki and its storage must be in Aspire's app.

### Architecture (Loki integration)

```
Aspire AppHost containers:
  loki (grafana/loki:3.4.2) → durable log storage for all lifecycle events, traces, status, metadata
  ollama (ollama/ollama)     → local LLM inference
  ragflow (infiniflow/ragflow) → RAG for integration docs

Observability storage:
  IObservabilityEventLog interface (unchanged)
  ├── LokiObservabilityEventLog   → real storage via Loki HTTP push API + LogQL queries
  └── InMemoryObservabilityEventLog → dev-only fallback (1 smoke test)

OpenClaw.Web auto-selects:
  Loki__BaseAddress env var set → uses LokiObservabilityEventLog
  No Loki URL                  → falls back to InMemoryObservabilityEventLog
```

- **Files created**:
  - `src/Observability/LokiObservabilityEventLog.cs` — full Loki HTTP push + LogQL query implementation
  - `tests/IntegrationTests/LokiObservabilityEventLogTests.cs` — 8 integration tests with real Loki via Testcontainers
- **Files modified**:
  - `src/AppHost/Program.cs` — added Loki container (grafana/loki:3.4.2) with persistent volume, passed Loki__BaseAddress to OpenClaw
  - `src/Observability/ObservabilityServiceExtensions.cs` — added overload `AddPlatformObservability(lokiBaseUrl)` for Loki-backed registration
  - `src/OpenClaw.Web/Program.cs` — auto-selects Loki-backed storage when Loki__BaseAddress is available
  - `tests/UnitTests/InMemoryObservabilityEventLogTests.cs` — reduced from 8 tests to 1 smoke test
  - `tests/IntegrationTests/IntegrationTests.csproj` — added Testcontainers, Contracts, Observability references
  - `Directory.Packages.props` — added Testcontainers 4.5.0
  - `rules/milestones.md` — updated chunk 009 description
- **Test counts**:
  - UnitTests: 29 (was 36, -7 InMemory tests removed, +0)
  - IntegrationTests: 9 (was 1, +8 Loki tests)
  - Total across all projects: 82 tests, all passing
  - Build: 0 warnings, 0 errors

## Chunk 009 enhancement – RagFlow in Aspire, demo data seeder, Ollama health, expanded tests

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Add RagFlow + Ollama containers to Aspire AppHost, seed demo observability data, add Ollama health endpoint to OpenClaw, expand Playwright and unit test coverage.

### Architecture additions

```
Aspire AppHost containers:
  ollama (ollama/ollama) → local LLM inference for OpenClaw AI + RagFlow
  ragflow (infiniflow/ragflow:v0.16.0-slim) → RAG for integration docs

OpenClaw.Web enhancements:
  DemoDataSeeder → seeds order-02, shipment-123, invoice-001 lifecycle events
  /api/health/ollama → returns { available: true/false, service: "ollama" }
  UI header → live Ollama status indicator (green/red badge)
  UI hint → mentions RagFlow for RAG documentation queries
```

- **Files created**:
  - `src/OpenClaw.Web/DemoDataSeeder.cs` — background service seeding demo lifecycle events
  - `tests/UnitTests/InMemoryObservabilityEventLogTests.cs` — 8 unit tests for observability store
- **Files modified**:
  - `src/AppHost/Program.cs` — added Ollama + RagFlow containers with volumes, env vars, endpoints
  - `src/OpenClaw.Web/Program.cs` — added DemoDataSeeder, /api/health/ollama endpoint, Ollama status badge in UI, RagFlow mention in hint
  - `tests/PlaywrightTests/OpenClawUiTests.cs` — expanded from 8 to 13 tests (Ollama status, seeded data queries, Ollama unavailable warning)
  - `rules/milestones.md` — updated chunk 009 description
- **Test counts**:
  - UnitTests: 36 (was 28, +8 observability log tests)
  - PlaywrightTests: 13 (was 8, +5 new tests)
  - Total across all projects: 81 tests, all passing
  - Build: 0 warnings, 0 errors


## Chunk 009 refactor – Isolate observability storage, Prometheus, Playwright tests

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Separate production message storage from observability storage. Add Prometheus as the metrics backend. Add Playwright UI tests. Notify explicitly when Ollama is unavailable.

### Architecture (production vs observability separation)

```
Production Layer (message processing pipeline only):
  IMessageStateStore → InMemoryMessageStateStore
  (Used ONLY by services processing messages. Swappable for Cassandra.)

Observability Layer (isolated, for operators via OpenClaw):
  Prometheus (/metrics endpoint) → stores/queries aggregated metrics
  IObservabilityEventLog → InMemoryObservabilityEventLog → stores/queries lifecycle events
  (Swappable for ELK/Seq/Loki for production log aggregation.)

MessageLifecycleRecorder writes to BOTH:
  → IMessageStateStore (production)
  → IObservabilityEventLog (observability)
  → OpenTelemetry (traces + metrics → Prometheus)

MessageStateInspector queries ONLY observability:
  → IObservabilityEventLog (NOT IMessageStateStore)
  → ITraceAnalyzer (Ollama AI) for diagnostic summary
```

- **Files created**:
  - `src/Observability/IObservabilityEventLog.cs` — interface for isolated observability event storage
  - `src/Observability/InMemoryObservabilityEventLog.cs` — in-memory implementation (swappable for ELK/Seq)
  - `tests/PlaywrightTests/PlaywrightTests.csproj` — Playwright + xUnit test project
  - `tests/PlaywrightTests/OpenClawUiTests.cs` — 8 Playwright UI tests (graceful skip when browsers not installed)
- **Files modified**:
  - `Directory.Packages.props` — added `OpenTelemetry.Exporter.Prometheus.AspNetCore`, `Microsoft.Playwright`, `Microsoft.AspNetCore.Mvc.Testing`
  - `src/ServiceDefaults/ServiceDefaults.csproj` — added Prometheus exporter package reference
  - `src/ServiceDefaults/Extensions.cs` — added `.AddPrometheusExporter()` to metrics pipeline, `app.MapPrometheusScrapingEndpoint()` to endpoint mapping
  - `src/Observability/MessageLifecycleRecorder.cs` — now writes to both `IMessageStateStore` (production) AND `IObservabilityEventLog` (observability)
  - `src/Observability/MessageStateInspector.cs` — queries `IObservabilityEventLog` instead of `IMessageStateStore`; returns explicit Ollama unavailable notification via `InspectionResult.OllamaAvailable` flag
  - `src/Observability/ObservabilityServiceExtensions.cs` — registers `IObservabilityEventLog` alongside production store
  - `src/OpenClaw.Web/Program.cs` — updated hint text to mention Prometheus; shows yellow "⚠️ Ollama Unavailable" notification card when AI is down
  - `tests/UnitTests/MessageLifecycleRecorderTests.cs` — updated tests to verify dual-write to both stores
  - `tests/UnitTests/MessageStateInspectorTests.cs` — tests now use observability log; added `OllamaAvailable` assertions
  - `rules/milestones.md` — updated chunk 009 description
- **Notes**:
  - All 28 unit tests pass. Build: 0 warnings, 0 errors.
  - Prometheus `/metrics` endpoint now exposed on all services via ServiceDefaults.
  - When Ollama is unavailable, UI shows explicit notification instead of fallback.

## Chunk 008 & 009 – Ollama AI integration + OpenTelemetry Observability + OpenClaw Web UI

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Implement full observability stack with message state store, OpenTelemetry instrumentation, Ollama AI-powered trace analysis, and the OpenClaw web UI for querying message state from any device.

### Architecture

OpenTelemetry instruments all services but does NOT store data. The observability layer adds:

1. **IMessageStateStore** – queryable store that records every lifecycle event for every message
2. **MessageLifecycleRecorder** – writes to the store AND emits OpenTelemetry traces/metrics simultaneously
3. **MessageStateInspector** – answers "where is my shipment for order 02?" by querying the store, then sending the full history to Ollama for AI-powered analysis
4. **OpenClaw.Web** – ASP.NET Core web app (registered in Aspire AppHost) that provides:
   - Responsive web UI accessible from any device (phone/tablet/desktop)
   - REST API endpoints for querying by business key or correlation ID
   - AI-generated diagnostic summaries via Ollama

### Flow: "Where is my shipment with order 02?"

```
User → OpenClaw Web UI → /api/inspect/business/order-02
  → MessageStateInspector queries InMemoryMessageStateStore
  → Gets full lifecycle: [Pending → InFlight (Routing) → InFlight (Transform) → ...]
  → Sends to TraceAnalyzer → Ollama generates summary
  → Returns InspectionResult with AI summary + event timeline
```

- **Files created**:
  - `src/Observability/PlatformActivitySource.cs` — central ActivitySource for distributed tracing
  - `src/Observability/PlatformMeters.cs` — counters and histograms for message processing metrics
  - `src/Observability/TraceEnricher.cs` — enriches Activity spans with IntegrationEnvelope metadata
  - `src/Observability/CorrelationPropagator.cs` — propagates correlation IDs across service boundaries
  - `src/Observability/MessageTracer.cs` — high-level API for tracing message lifecycle stages
  - `src/Observability/MessageEvent.cs` — record of a single lifecycle event
  - `src/Observability/IMessageStateStore.cs` — interface for storing/querying message state
  - `src/Observability/InMemoryMessageStateStore.cs` — in-memory implementation (swappable for Cassandra)
  - `src/Observability/MessageLifecycleRecorder.cs` — records events to store + emits OTel
  - `src/Observability/ITraceAnalyzer.cs` — interface for AI-assisted trace analysis
  - `src/Observability/TraceAnalyzer.cs` — Ollama-backed implementation
  - `src/Observability/ObservabilityServiceExtensions.cs` — DI registration
  - `src/AI.Ollama/OllamaService.cs` — HttpClient-based Ollama API client
  - `src/AI.Ollama/OllamaHealthCheck.cs` — health check for Ollama connectivity
  - `src/AI.Ollama/OllamaServiceExtensions.cs` — DI registration
  - `src/OpenClaw.Web/OpenClaw.Web.csproj` — ASP.NET Core web app project
  - `src/OpenClaw.Web/Program.cs` — API endpoints + embedded responsive HTML UI
  - `src/OpenClaw.Web/appsettings.json`, `appsettings.Development.json`
  - `src/OpenClaw.Web/Properties/launchSettings.json`
  - `tests/UnitTests/InMemoryMessageStateStoreTests.cs` — 8 tests for the state store
  - `tests/UnitTests/MessageStateInspectorTests.cs` — 5 tests for inspector + AI fallback
  - `tests/UnitTests/MessageLifecycleRecorderTests.cs` — 7 tests for lifecycle recording
  - `tests/UnitTests/TraceEnricherTests.cs` — 3 tests for trace enrichment
  - `tests/UnitTests/TraceAnalyzerTests.cs` — 4 tests for AI trace analysis
- **Files modified**:
  - `src/Observability/DiagnosticsConfig.cs` — expanded with ActivitySource, Meter, ServiceVersion
  - `src/Observability/Observability.csproj` — added references to Contracts, AI.Ollama, OpenTelemetry
  - `src/Observability/MessageStateInspector.cs` — rewritten to query state store + Ollama + return InspectionResult
  - `src/AI.Ollama/IOllamaService.cs` — added GenerateAsync, AnalyseAsync, IsHealthyAsync methods
  - `src/AI.Ollama/AI.Ollama.csproj` — added FrameworkReference for health checks
  - `src/AppHost/AppHost.csproj` — added ProjectReference to OpenClaw.Web
  - `src/AppHost/Program.cs` — added OpenClaw.Web with WithExternalHttpEndpoints()
  - `tests/UnitTests/UnitTests.csproj` — added ProjectReferences to Contracts, Observability, AI.Ollama
  - `rules/milestones.md` — marked chunks 008 and 009 as done
  - `rules/completion-log.md` — this entry
- **Notes**:
  - All 28 unit tests pass (27 new + 1 pre-existing placeholder)
  - Build: 0 warnings, 0 errors
  - OpenClaw is registered in Aspire AppHost with `WithExternalHttpEndpoints()` for device access
  - InMemoryMessageStateStore supports business key (case-insensitive), correlation ID, and message ID lookups
  - When Ollama is unavailable, fallback summaries are generated from stored state
  - The state store is designed to be swappable — replace InMemoryMessageStateStore with a Cassandra-backed implementation for production

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Define the full set of shared message contracts in the `Contracts` project
- **Files modified**:
  - `src/Contracts/IntegrationEnvelope.cs` — added `SchemaVersion` (default `"1.0"`), `CausationId` (nullable Guid), `Priority` (default `Normal`), and static `Create<T>()` factory method
  - `tests/ContractTests/ContractTests.csproj` — added `<ProjectReference>` to `Contracts`
- **Files created**:
  - `src/Contracts/MessageHeaders.cs` — string constants for well-known metadata keys (TraceId, SpanId, ContentType, SchemaVersion, SourceTopic, ConsumerGroup, LastAttemptAt, RetryCount)
  - `src/Contracts/MessagePriority.cs` — enum (Low, Normal, High, Critical)
  - `src/Contracts/DeliveryStatus.cs` — enum (Pending, InFlight, Delivered, Failed, Retrying, DeadLettered)
  - `src/Contracts/FaultEnvelope.cs` — record with static `Create<T>()` factory for dead-letter / fault scenarios
  - `tests/ContractTests/IntegrationEnvelopeTests.cs` — 15 focused unit tests
  - `tests/ContractTests/FaultEnvelopeTests.cs` — 9 focused unit tests
  - `tests/ContractTests/MessageHeadersTests.cs` — 5 focused unit tests
- **Notes**:
  - `Contracts` project retains ZERO project dependencies (pure DTOs and value types)
  - `IntegrationEnvelope<T>.Create()` provides a convenient factory that auto-generates MessageId and Timestamp; correlationId and causationId are optional parameters
  - `FaultEnvelope.Create<T>()` captures exception details (type, message, stack trace) and preserves the original CorrelationId for end-to-end tracing
  - All 33 tests pass (29 new contract tests + 4 pre-existing placeholder tests)
  - Build: 0 warnings, 0 errors



- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Configure Aspire AppHost to orchestrate all service projects and add ServiceDefaults to each service
- **Files modified**:
  - `src/AppHost/AppHost.csproj` — added ProjectReference for Gateway.Api, Ingestion.Kafka, Workflow.Temporal, Admin.Api, Admin.Web
  - `src/AppHost/Program.cs` — wired up all services using builder.AddProject<Projects.*>(); Admin.Web references Admin.Api via WithReference
  - `src/Gateway.Api/Gateway.Api.csproj` — added ProjectReference to ServiceDefaults
  - `src/Gateway.Api/Program.cs` — added AddServiceDefaults() and MapDefaultEndpoints()
  - `src/Ingestion.Kafka/Ingestion.Kafka.csproj` — added ProjectReference to ServiceDefaults
  - `src/Ingestion.Kafka/Program.cs` — added AddServiceDefaults()
  - `src/Workflow.Temporal/Workflow.Temporal.csproj` — added ProjectReference to ServiceDefaults
  - `src/Workflow.Temporal/Program.cs` — added AddServiceDefaults()
  - `src/Admin.Api/Admin.Api.csproj` — added ProjectReference to ServiceDefaults
  - `src/Admin.Api/Program.cs` — added AddServiceDefaults() and MapDefaultEndpoints()
  - `src/Admin.Web/Admin.Web.csproj` — added ProjectReference to ServiceDefaults
  - `src/Admin.Web/Program.cs` — added AddServiceDefaults() and MapDefaultEndpoints()
  - `rules/milestones.md` — marked chunk 003 as done, updated Next Chunk to 004
  - `rules/completion-log.md` — this entry
- **Notes**:
  - AppHost project references enable Aspire SDK to generate Projects.* types for type-safe orchestration
  - All 5 service projects now call AddServiceDefaults() for OpenTelemetry, health checks, service discovery, and resilience
  - Web services (Gateway.Api, Admin.Api, Admin.Web) also call MapDefaultEndpoints() for /health and /alive endpoints in Development
  - Worker services (Ingestion.Kafka, Workflow.Temporal) call AddServiceDefaults() on IHostApplicationBuilder
  - Build: 0 warnings, 0 errors; all 5 test projects pass

## Chunk 002 – GitHub Actions CI pipeline

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Add automated CI pipeline to build and test the solution on every push and PR
- **Files created**:
  - `.github/workflows/ci.yml`
- **Files modified**:
  - `EnterpriseIntegrationPlatform/rules/milestones.md` — added resumption prompt at top, inserted Chunk 002, renumbered subsequent chunks
  - `EnterpriseIntegrationPlatform/rules/completion-log.md` — this entry
- **Notes**:
  - Workflow triggers on push to `main` and `copilot/**` branches, and on PRs to `main`
  - Uses `actions/setup-dotnet@v4` with .NET 10.x
  - Builds in Release configuration, runs all test projects
  - All 5 test projects (UnitTests, IntegrationTests, ContractTests, WorkflowTests, LoadTests) pass

## Chunk 001 – Repository scaffold

- **Date**: 2026-03-14
- **Status**: done
- **Goal**: Create the full solution structure with all projects and directory layout
- **Scope**: Solution file, project files, directory structure, global configuration
- **Files created**:
  - `EnterpriseIntegrationPlatform.sln`
  - `global.json`
  - `Directory.Build.props`
  - `Directory.Packages.props`
  - `.editorconfig`
  - `src/AppHost/AppHost.csproj` + `Program.cs`, `appsettings.json`, `launchSettings.json`
  - `src/ServiceDefaults/ServiceDefaults.csproj` + `Extensions.cs`
  - `src/Gateway.Api/Gateway.Api.csproj` + `Program.cs`
  - `src/Ingestion.Kafka/Ingestion.Kafka.csproj` + `Program.cs`
  - `src/Contracts/Contracts.csproj` + `IntegrationEnvelope.cs`
  - `src/Workflow.Temporal/Workflow.Temporal.csproj` + `Program.cs`
  - `src/Activities/Activities.csproj` + `BaseActivity.cs`
  - `src/Connectors/Connector.Http/Connector.Http.csproj` + `IHttpConnector.cs`
  - `src/Connectors/Connector.Sftp/Connector.Sftp.csproj` + `ISftpConnector.cs`
  - `src/Connectors/Connector.Email/Connector.Email.csproj` + `IEmailConnector.cs`
  - `src/Connectors/Connector.File/Connector.File.csproj` + `IFileConnector.cs`
  - `src/Processing.Transform/Processing.Transform.csproj` + `IMessageTransformer.cs`
  - `src/Processing.Routing/Processing.Routing.csproj` + `IMessageRouter.cs`
  - `src/Storage.Cassandra/Storage.Cassandra.csproj` + `ICassandraRepository.cs`
  - `src/AI.Ollama/AI.Ollama.csproj` + `IOllamaService.cs`
  - `src/RuleEngine/RuleEngine.csproj` + `IRuleEngine.cs`
  - `src/Admin.Api/Admin.Api.csproj` + `Program.cs`
  - `src/Admin.Web/Admin.Web.csproj` + `Program.cs`
  - `src/Observability/Observability.csproj` + `DiagnosticsConfig.cs`
  - `tests/UnitTests/UnitTests.csproj` + `SampleTest.cs`
  - `tests/IntegrationTests/IntegrationTests.csproj` + `SampleTest.cs`
  - `tests/ContractTests/ContractTests.csproj` + `SampleTest.cs`
  - `tests/WorkflowTests/WorkflowTests.csproj` + `SampleTest.cs`
  - `tests/LoadTests/LoadTests.csproj` + `SampleTest.cs`
  - `docs/*.md` (20 documentation files)
  - `rules/*.md` (4 rules files)
- **Notes**:
  - Initially scaffolded with .NET 9, then upgraded to .NET 10 / Aspire 13.1.2
  - ServiceDefaults updated to latest Aspire template with OpenTelemetry, health checks, service discovery
  - All packages updated to latest versions (OpenTelemetry 1.14.0, FluentAssertions 8.8.0, xunit.runner 3.1.5, Test.Sdk 18.3.0)
  - Added `docs/developer-setup.md` with .NET 10 installation instructions
