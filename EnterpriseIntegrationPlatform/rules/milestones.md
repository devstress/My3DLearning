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

✅ Phases 1–14 complete — see `rules/completion-log.md` for full history.

**Current stats:** 1,472 UnitTests + 58 Contract + 29 Workflow + 17 Integration + 10 Load + 19 Vitest = **1,605 total tests**. 48 src projects.

## Next Chunk

➡️ Phase 15 — Tutorial Fixes Round 2 (chunk 077)

---

### Phase 15 — Tutorial Fixes Round 2

Re-audit of all 50 tutorials (2026-04-05) found **17 tutorials still have errors** that were either introduced after Phase 13 fixes or missed entirely.

| Chunk | Goal | Tutorials | Status |
|-------|------|-----------|--------|
| 077 | Fix tutorials 31, 32, 37, 38 (advanced pattern & connector errors) | 31, 32, 37, 38 | `not-started` |
| 078 | Fix tutorials 42, 44, 45, 46 (config, DR, profiling, end-to-end errors) | 42, 44, 45, 46 | `not-started` |
| 079 | Fix tutorials 48, 49 and update test counts | 48, 49 | `not-started` |

---

#### Chunk 077 — Fix Tutorials 31, 32, 37, 38

**Tutorial 31 — Event Sourcing:**

| Issue | Severity |
|-------|----------|
| `IEventProjection<TState>` method shown as synchronous `TState Apply(TState state, EventEnvelope @event)` but actual is `Task<TState> ProjectAsync(TState state, EventEnvelope envelope, CancellationToken ct)`. Wrong name, wrong return type, missing cancellation token. | 🔴 ERROR |
| `TemporalQuery` parameter shown as `int batchSize = 100` but actual is `int maxEventsPerRead = 1000`. Different name and default. | 🟡 WARNING |

**Tutorial 32 — Multi-Tenancy:**

| Issue | Severity |
|-------|----------|
| `ITenantOnboardingService.OnboardAsync` does not exist — actual method is `ProvisionAsync`. | 🔴 ERROR |
| `ITenantOnboardingService.OffboardAsync` does not exist — actual method is `DeprovisionAsync`. | 🔴 ERROR |
| `OnboardAsync` return type shown as `Task<TenantContext>` but actual is `Task<TenantOnboardingResult>`. | 🔴 ERROR |
| `OffboardAsync` return type shown as `Task` but actual `DeprovisionAsync` returns `Task<TenantOnboardingResult>`. | 🔴 ERROR |
| `TenantOnboardingRequest` missing required `TenantId` and `TenantPlan Plan` parameters. Property named `Properties` should be `Metadata` (type `IReadOnlyDictionary` not `IDictionary`). | 🔴 ERROR |

**Tutorial 37 — File Connector:**

| Issue | Severity |
|-------|----------|
| File path shown as `src/Connector.FileSystem/` but actual directory is `src/Connector.File/`. | 🟡 WARNING |

**Tutorial 38 — OpenTelemetry:**

| Issue | Severity |
|-------|----------|
| `DiagnosticsConfig` shown as instance class with `init` properties but actual is a `static` class with `const` and `static readonly` members. | 🔴 ERROR |

---

#### Chunk 078 — Fix Tutorials 42, 44, 45, 46

**Tutorial 42 — Configuration:**

| Issue | Severity |
|-------|----------|
| `IFeatureFlagService.GetVariantAsync` shown as `(string flagName, string? tenantId, CancellationToken ct)` but actual is `(string name, string variantKey, CancellationToken ct)`. Completely different parameters. | 🔴 ERROR |
| `IConfigurationStore.WatchAsync` return type shown as `IAsyncEnumerable<ConfigurationChange>` but actual is `IObservable<ConfigurationChange>`. Different consumption pattern. | 🔴 ERROR |
| `IConfigurationStore.GetAsync` `environment` parameter shown as required but actual has default `"default"`. | 🟡 WARNING |

**Tutorial 44 — Disaster Recovery:**

| Issue | Severity |
|-------|----------|
| `DisasterRecoveryService` class shown but does not exist. Actual architecture uses `IFailoverManager`, `IReplicationManager`, `IDrDrillRunner`. | 🔴 ERROR |
| `DrDrillService` class shown but actual is `DrDrillRunner`. | 🔴 ERROR |
| `InitiateFailoverAsync(FailoverRequest)` does not exist. Actual is `IFailoverManager.FailoverAsync(string targetRegionId, CancellationToken)`. | 🔴 ERROR |

**Tutorial 45 — Performance Profiling:**

| Issue | Severity |
|-------|----------|
| `ContinuousProfiler.CaptureSnapshot()` return type shown as `ProfilingSnapshot` but actual is `ProfileSnapshot` with nested structure (Cpu, Memory, Gc sub-objects). | 🔴 ERROR |
| `ContinuousProfiler.GetSnapshots(int count = 10)` — actual is `GetSnapshots(DateTimeOffset from, DateTimeOffset to)`. Completely different parameters. | 🔴 ERROR |
| `GcMonitor.GetHistory(int count = 10)` — actual `GetHistory()` takes no parameters. | 🔴 ERROR |
| `GcMonitor.GetRecommendations()` shown returning `IReadOnlyList<string>` but actual returns `IReadOnlyList<GcTuningRecommendation>`. | 🔴 ERROR |

**Tutorial 46 — Complete End-to-End Integration:**

| Issue | Severity |
|-------|----------|
| `HttpChannelAdapter : IChannelAdapter` class shown but does not exist. Actual is `HttpConnectorAdapter : IConnector` at `src/Connector.Http/HttpConnectorAdapter.cs`. | 🔴 ERROR |
| Activity class names in workflow example differ from actual (`ValidateActivity` etc. vs `PipelineActivities`/`IntegrationActivities`). | 🟡 WARNING |

---

#### Chunk 079 — Fix Tutorials 48, 49 + Test Counts

**Tutorial 48 — Notification Use Cases:**

| Issue | Severity |
|-------|----------|
| `NotificationDecisionService` class shown but does not exist in codebase. Notification logic is in workflow activities. | 🟡 WARNING |

**Tutorial 49 — Testing Integrations:**

| Issue | Severity |
|-------|----------|
| Test code shows `_mapper.MapAck(envelope)` and `_mapper.MapNack(envelope, "timeout")` but actual API is `MapAck(Guid messageId, Guid correlationId)` and `MapNack(Guid messageId, Guid correlationId, string errorMessage)`. Test code will not compile. | 🔴 ERROR |
| Test count shows "1,400 unit tests / 1,538 total" but actual count is now **1,472 unit tests / 1,605 total** (after Phase 14). | 🟡 WARNING |

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
