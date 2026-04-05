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

✅ Phases 1–16 complete — see `rules/completion-log.md` for full history.

**Current stats:** 1,472 UnitTests + 58 Contract + 29 Workflow + 17 Integration + 10 Load + 19 Vitest = **1,605 total tests**. 48 src projects.

---

### Phase 17 — Tutorial Audit & Fixes (Round 4)

✅ Phase 17 complete.

**Scope:** Fresh re-audit of all 50 tutorials by a new agent with no prior context — follow each tutorial as a reader would, verifying every code snippet, interface signature, property type, enum value, and file path against the actual source.

**Findings:** 4 tutorials had remaining issues; 46 passed clean.

| Tutorial | Issue | Fix Applied |
|----------|-------|-------------|
| 07 — Temporal Workflows | Workflow code block still used wrong activity classes (`IntegrationActivities` for persist/ack/nack instead of `PipelineActivities`), wrong method names (`UpdateStatusAsync` vs `UpdateDeliveryStatusAsync`), wrong params (`validationResult.Errors` vs `validation.Reason`), saga block used `CompensateAsync` vs `CompensateStepAsync`, returned `IntegrationPipelineResult` instead of `AtomicPipelineResult` | Rewrote both workflow code blocks to match actual `IntegrationPipelineWorkflow.cs` and `AtomicPipelineWorkflow.cs` with correct activity class routing, parameter signatures, and return types |
| 31 — Event Sourcing | `ISnapshotStore.LoadAsync` return type shown as nullable tuple `Task<(TState?, long)?>` — actual is non-nullable `Task<(TState?, long)>` | Removed trailing `?` from tuple return type |
| 42 — Configuration | `FeatureFlag.TargetTenants` type shown as `IReadOnlyList<string>` → actual is `List<string>?`; `NotificationsEnabled` value lowercase `"notifications.enabled"` → actual PascalCase `"Notifications.Enabled"`; file path `src/Configuration/` → actual `src/Activities/` | Updated record definition to positional record matching source; fixed constant casing and file path |
| 46 — Complete Integration | Return type `PipelineResult` → actual `IntegrationPipelineResult`; non-existent factory methods `PipelineResult.Failed()`/`Succeeded()` → actual uses constructor; `validation.ErrorMessage` → actual `validation.Reason`; `input.SourceTopic` → actual `input.NackSubject`/`input.AckSubject` | Rewrote workflow code block with correct types, constructors, property names, and notification guard |

## Next Chunk

All phases complete (1–17). See `rules/completion-log.md` for full history.

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
