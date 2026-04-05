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

**Current stats:** 1,498 UnitTests + 58 Contract + 29 Workflow + 17 Integration + 10 Load + 19 Vitest = **1,631 total tests**. 48 src projects.

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

**Chunk 088** — Rule Engine In-Memory Caching with Periodic Refresh

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
