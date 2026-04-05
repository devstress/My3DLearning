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

**Current stats:** 1,518 UnitTests + 58 Contract + 29 Workflow + 17 Integration + 10 Load + 19 Vitest = **1,651 total tests**. 48 src projects.

**Next chunk:** Phase 22 complete — all 13 chunks (080-092) done.

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

#### Chunk 090 — EnvironmentOverrideProvider: EIP__ Environment Variable Convention

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Tutorial | 42 — Configuration (line 121) |
| Claim | "The `EnvironmentOverrideProvider` reads environment variables using the convention `EIP__Key__SubKey` (double underscore as separator). Environment variables take precedence over store values." |
| Current State | `EnvironmentOverrideProvider` only does cascading resolution from the `IConfigurationStore`. It never reads `System.Environment.GetEnvironmentVariable()`. |
| Implementation | In `ResolveAsync`, before falling back to the store, check `Environment.GetEnvironmentVariable($"EIP__{key.Replace(":", "__")}")`. If found, return a synthetic `ConfigurationEntry` with that value. Add `ResolveManyAsync` override similarly. Add unit tests using environment variable injection. |
| Files | `src/Configuration/EnvironmentOverrideProvider.cs`, `tests/UnitTests/EnvironmentOverrideProviderTests.cs` |

#### Chunk 092 — Kustomize Base Directory Structure

| Field | Value |
|-------|-------|
| Status | `done` |
| Tutorial | 43 — Kubernetes Deployment (lines 91-104) |
| Claim | Tutorial shows flat `base/` with `deployment.yaml` and `service.yaml`. |
| Current State | Actual structure has `base/admin-api/` and `base/openclaw-web/` subdirectories. |
| Implementation | Updated tutorial 43 to match the actual directory structure (service-specific subdirectories, namespace.yaml, prod PDB files). |
| Files | `tutorials/43-kubernetes-deployment.md` |

## Next Chunk

Phase 22 complete — all 13 chunks (080-092) done.

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
