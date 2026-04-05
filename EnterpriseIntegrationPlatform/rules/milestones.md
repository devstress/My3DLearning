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

➡️ Phase 15 — Tutorial Fixes Round 2 (chunk 079)

---

### Phase 15 — Tutorial Fixes Round 2

Re-audit of all 50 tutorials (2026-04-05) found **17 tutorials still have errors** that were either introduced after Phase 13 fixes or missed entirely.

| Chunk | Goal | Tutorials | Status |
|-------|------|-----------|--------|
| 079 | Fix tutorials 48, 49 and update test counts | 48, 49 | `not-started` |

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
