# Completion Log

Detailed record of completed chunks, files created/modified, and notes.

See `milestones.md` for current phase status and next chunk.

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
