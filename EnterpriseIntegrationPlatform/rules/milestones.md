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

## Completed Phases

✅ Phases 1–21 complete — see `rules/completion-log.md` for full history.

**Current stats:** 1,479 UnitTests + 58 Contract + 29 Workflow + 17 Integration + 10 Load + 19 Vitest = **1,612 total tests**. 48 src projects.

---

### Phase 19 — Tutorial Audit as New Developer (Round 6)

✅ Phase 19 complete — see `rules/completion-log.md`.

### Phase 20 — Tutorial Audit as New Developer (Round 7)

✅ Phase 20 complete — fixed 7 tutorials (03, 17, 26, 28, 29, 45, 48) plus INormalizer.cs xmldoc.

### Phase 21 — Tutorial Code Snippet Accuracy Audit

✅ Phase 21 complete — fixed 4 tutorials (26, 31, 35, 38) with code snippets mismatched against actual source code.

---

### Phase 22 — Implement Unfulfilled Tutorial Promises

**Scope:** Audit of all 50 tutorials against source code found 13 features that tutorials promise but are not implemented. These chunks implement the missing features so that every tutorial claim is backed by working code.

#### Chunk 081 — Unified Broker Selection via AddIngestion

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 05 — Message Brokers (line 124) |
| Claim | `services.AddIngestion(options => { options.BrokerType = BrokerType.NatsJetStream; })` — a unified DI registration method that reads `BrokerType` and registers the correct producer/consumer. |
| Current State | `BrokerType` enum exists and is used in `BrokerTransactionalClient`, but no `AddIngestion()` method exists that wires up producer/consumer based on `BrokerType`. Each broker must be registered separately. |
| Implementation | Add `AddIngestion(Action<BrokerOptions> configure)` to `IngestionServiceExtensions.cs` that switches on `BrokerOptions.BrokerType` to call `AddNatsJetStreamBroker`, `AddKafkaBroker`, or `AddPulsarBroker`. Add unit tests. |
| Files | `src/Ingestion/IngestionServiceExtensions.cs`, `tests/UnitTests/IngestionServiceExtensionsTests.cs` |

#### Chunk 082 — MessageFilter No-Silent-Drop Enforcement

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 10 — Message Filter (line 94) |
| Claim | "The platform enforces no silent drops in production deployments." and "If the DLQ publish fails, the source message is Nacked and redelivered." |
| Current State | `MessageFilter.FilterAsync()` silently discards when `DiscardTopic` is null. No Nack-on-DLQ-failure logic exists. |
| Implementation | Add `RequireDiscardTopic` boolean (default false) to `MessageFilterOptions`. When true, throw `InvalidOperationException` if `DiscardTopic` is not set. Wrap the discard publish in try-catch; on failure, throw so the caller can Nack. Add unit tests for both behaviors. |
| Files | `src/Processing.Routing/MessageFilterOptions.cs`, `src/Processing.Routing/MessageFilter.cs`, `tests/UnitTests/MessageFilterTests.cs` |

#### Chunk 083 — Content Enricher: Database and Cache Sources

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 18 — Content Enricher (line 7) |
| Claim | "Enrichment sources: HTTP lookups, database queries, cache" |
| Current State | `ContentEnricher` only supports HTTP GET. No database or cache enrichment. |
| Implementation | Extract enrichment source as `IEnrichmentSource` interface with `FetchAsync(string lookupKey, CancellationToken ct)`. Implement `HttpEnrichmentSource` (extract current HTTP logic), `DatabaseEnrichmentSource` (uses `IDbConnection` with parameterized SQL from options), and `CachedEnrichmentSource` (decorator using `IMemoryCache` with configurable TTL). `ContentEnricher` takes `IEnrichmentSource` instead of `IHttpClientFactory`. Add `EnrichmentSourceType` enum to options. Add unit tests for each source and caching behavior. |
| Files | New `src/Processing.Transform/IEnrichmentSource.cs`, `src/Processing.Transform/HttpEnrichmentSource.cs`, `src/Processing.Transform/DatabaseEnrichmentSource.cs`, `src/Processing.Transform/CachedEnrichmentSource.cs`, `src/Processing.Transform/ContentEnricherOptions.cs`, `src/Processing.Transform/ContentEnricher.cs`, `tests/UnitTests/ContentEnricherTests.cs` |

#### Chunk 084 — Normalizer: Use XmlRootName Option

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 17 — Normalizer (line 82) |
| Claim | "Root element name used when converting non-XML formats to XML" |
| Current State | `NormalizerOptions.XmlRootName` property exists but is never read by `MessageNormalizer`. Dead code. |
| Implementation | If the normalizer is asked to produce XML output (or if a future XML output mode is added), use `XmlRootName` as the root element. Alternatively, if only JSON output is supported, use `XmlRootName` when parsing XML→JSON to name the wrapper property. Document the actual usage in xmldoc. Add unit test proving the option is respected. |
| Files | `src/Processing.Transform/MessageNormalizer.cs`, `src/Processing.Transform/NormalizerOptions.cs`, `tests/UnitTests/MessageNormalizerTests.cs` |

#### Chunk 085 — Aggregator Store Idempotency on MessageId

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 21 — Aggregator (line 112) |
| Claim | "the store must be idempotent on MessageId" — redelivered messages should not be duplicated in the aggregation group. |
| Current State | `InMemoryMessageAggregateStore.AddAsync()` blindly appends every envelope. Duplicate `MessageId` values are not detected. |
| Implementation | In `AddAsync`, check if any existing envelope in the group has the same `MessageId`. If so, skip the add and return the existing snapshot. Add unit tests for duplicate detection. |
| Files | `src/Processing.Aggregator/InMemoryMessageAggregateStore.cs`, `tests/UnitTests/InMemoryMessageAggregateStoreTests.cs` |

#### Chunk 086 — ReplayId Header Injection in MessageReplayer

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 26 — Message Replay (lines 9, 31, 34, 94, 106, 114) |
| Claim | "Every replayed message receives a `ReplayId` header (a GUID) linking it back to the replay operation" for audit-trail separation and idempotent consumer deduplication. |
| Current State | `MessageReplayer.ReplayAsync()` copies envelope metadata but never injects a `ReplayId` header. `SkippedCount` is always 0. |
| Implementation | Generate a single `ReplayId` (GUID) per `ReplayAsync` invocation. Add `MessageHeaders.ReplayId` constant to `src/Contracts/MessageHeaders.cs`. Inject `replayedEnvelope.Metadata[MessageHeaders.ReplayId] = replayId.ToString()` for each message. Track skipped messages (e.g. if a message was already replayed based on presence of existing `ReplayId` and dedup option in `ReplayOptions`). Add unit tests. |
| Files | `src/Contracts/MessageHeaders.cs`, `src/Processing.Replay/MessageReplayer.cs`, `src/Processing.Replay/ReplayOptions.cs`, `tests/UnitTests/MessageReplayerTests.cs` |

#### Chunk 087 — Backpressure Pauses Scale-Down in Competing Consumers

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 28 — Competing Consumers (line 113) |
| Claim | "When `IsBackpressured` is true, the orchestrator pauses scale-down and can signal upstream producers (via broker flow control or HTTP 429) to slow ingestion." |
| Current State | `CompetingConsumerOrchestrator.EvaluateAndScaleAsync()` never reads `_backpressure.IsBackpressured`. Scale-down proceeds regardless of backpressure state. |
| Implementation | In the scale-down branch of `EvaluateAndScaleAsync`, check `_backpressure.IsBackpressured` and skip scale-down if true (log a warning instead). Add unit test verifying scale-down is skipped during backpressure. |
| Files | `src/Processing.CompetingConsumers/CompetingConsumerOrchestrator.cs`, `tests/UnitTests/CompetingConsumersTests/CompetingConsumerOrchestratorTests.cs` |

#### Chunk 088 — Rule Engine In-Memory Caching with Periodic Refresh

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 30 — Rule Engine (line 134) |
| Claim | "Rules are cached in memory and refreshed periodically." |
| Current State | `BusinessRuleEngine.EvaluateAsync()` calls `_ruleStore.GetAllAsync()` on every single message evaluation. No caching. |
| Implementation | Add `CacheEnabled` (default true) and `CacheRefreshIntervalMs` (default 60000) to `RuleEngineOptions`. In `BusinessRuleEngine`, maintain a `IReadOnlyList<BusinessRule>? _cachedRules` field and a `DateTimeOffset _lastRefresh`. On `EvaluateAsync`, if cache is stale (elapsed > interval) or null, refresh from store. Add unit tests for cache hit, cache miss, and refresh behavior. |
| Files | `src/RuleEngine/RuleEngineOptions.cs`, `src/RuleEngine/BusinessRuleEngine.cs`, `tests/UnitTests/BusinessRuleEngineTests.cs` |

#### Chunk 089 — InputSanitizer: Script Tag, SQL Injection, HTML Entity, and Control Character Detection

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 33 — Security (lines 50-54) |
| Claim | Sanitizer detects and removes: script tags (`<script>`, `onclick`, `onerror`), SQL injection patterns (`'; DROP TABLE`, `OR 1=1`, `UNION SELECT`), HTML entities (`&#60;`, `&lt;`), and control characters (null bytes, Unicode direction overrides). |
| Current State | `InputSanitizer` only removes CRLF + null bytes (3 chars in `DangerousChars`). No XSS, SQL injection, HTML entity, or Unicode override detection. |
| Implementation | Extend `InputSanitizer.Sanitize()` to: (1) strip `<script>` blocks via regex, (2) remove inline event handler attributes (`on\w+=`), (3) decode and re-encode HTML entities to neutralize entity-based bypasses, (4) detect common SQL injection patterns and strip them (configurable via `SanitizationOptions`), (5) remove Unicode direction override chars (U+202A-U+202E, U+2066-U+2069). Extend `IsClean()` to detect these patterns. Add comprehensive unit tests. |
| Files | `src/Security/InputSanitizer.cs`, new `src/Security/SanitizationOptions.cs`, `tests/UnitTests/InputSanitizerTests.cs` |

#### Chunk 090 — EnvironmentOverrideProvider: EIP__ Environment Variable Convention

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 42 — Configuration (line 121) |
| Claim | "The `EnvironmentOverrideProvider` reads environment variables using the convention `EIP__Key__SubKey` (double underscore as separator). Environment variables take precedence over store values." |
| Current State | `EnvironmentOverrideProvider` only does cascading resolution from the `IConfigurationStore`. It never reads `System.Environment.GetEnvironmentVariable()`. |
| Implementation | In `ResolveAsync`, before falling back to the store, check `Environment.GetEnvironmentVariable($"EIP__{key.Replace(":", "__")}")`. If found, return a synthetic `ConfigurationEntry` with that value. Add `ResolveManyAsync` override similarly. Add unit tests using environment variable injection. |
| Files | `src/Configuration/EnvironmentOverrideProvider.cs`, `tests/UnitTests/EnvironmentOverrideProviderTests.cs` |

#### Chunk 091 — DR Status Endpoint and Profiling API Endpoints

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 44 — Disaster Recovery (line 103), 45 — Performance Profiling (lines 153-157) |
| Claims | Tutorial 44: `GET /api/admin/dr/status` endpoint. Tutorial 45: `GET /api/admin/profiling/status`, `POST /api/admin/profiling/cpu/start`, `POST /api/admin/profiling/cpu/stop`, `POST /api/admin/profiling/memory/snap`, `GET /api/admin/profiling/gc/stats`. |
| Current State | DR endpoints exist but no `/api/admin/dr/status`. Profiling has different endpoint structure (`/snapshot`, `/hotspots`, `/operations`, `/gc`, etc.) — none of the 5 claimed endpoints exist. |
| Implementation | **DR**: Add `GET /api/admin/dr/status` that aggregates region health from `IFailoverManager.GetAllRegionsAsync()` + primary region + replication status. **Profiling**: Add the 5 endpoints: `status` (returns whether profiling is active), `cpu/start` (starts continuous profiling), `cpu/stop` (stops + returns latest snapshot), `memory/snap` (captures heap snapshot via `ContinuousProfiler`), `gc/stats` (delegates to `IGcMonitor`). Add contract tests. |
| Files | `src/Admin.Api/Program.cs`, `tests/ContractTests/` |

#### Chunk 092 — Kustomize Base Directory Structure

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 43 — Kubernetes Deployment (lines 91-104) |
| Claim | Tutorial shows flat `base/` with `deployment.yaml` and `service.yaml`. |
| Current State | Actual structure has `base/admin-api/` and `base/openclaw-web/` subdirectories. |
| Implementation | Update tutorial 43 to match the actual directory structure (service-specific subdirectories). This is a documentation fix, not code — the actual structure is correct and better organized. |
| Files | `tutorials/43-kubernetes-deployment.md` |

## Next Chunk

**Chunk 081** — Unified Broker Selection via AddIngestion

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
