# EnterpriseIntegrationPlatform – Milestones

> **To continue development, tell the AI agent:**
>
> ```
> Continue
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

✅ Phases 1–28 complete — see `rules/completion-log.md` for full history.

49 src projects. 522 TutorialLabs tests across 50 tutorials. 38 BrokerAgnosticTests. Broker providers: NATS JetStream, Kafka, Pulsar, **PostgreSQL**. All EIP routing patterns verified on all 4 transports.

✅ Phase 29 complete — see `rules/completion-log.md` for full history.

All 50 tutorials now have Lab.cs + Exam.cs (fill-in-blank) + Exam.Answers.cs. 150 ExamAnswers tests pass. 512 total TutorialLabs tests pass. Exam.cs uses `#if EXAM_STUDENT` conditional compilation (students define EXAM_STUDENT to enable).

---

### File Format Rules

**Lab.cs** header:
```
// ============================================================================
// Tutorial XX – Topic Name (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test to see how [topic] works...
// CONCEPTS DEMONSTRATED (one per test):
//   1. TestName — concept
// INFRASTRUCTURE: NatsBrokerEndpoint / MockEndpoint
// ============================================================================
```

**Exam.cs** (student fill-in-the-blank):
```
// ============================================================================
// Tutorial XX – Topic Name (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — description
//   🟡 Intermediate — description
//   🔴 Advanced     — description
// ============================================================================
```
- Key setup/assertion/logic lines replaced with `// TODO: <hint>`
- Test methods named: `Starter_...`, `Intermediate_...`, `Advanced_...`
- Tests will NOT compile or pass as-is (missing variable assignments, pipeline wiring, assertions)

**Exam.Answers.cs** (answer key):
```
// ============================================================================
// Tutorial XX – Topic Name (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
// ============================================================================
```
- Full working code, identical structure to Exam.cs but with blanks filled in
- Class name: `ExamAnswers` (in same namespace)
- Must compile and pass all tests

**Markdown** sections (in order):
1. `# Tutorial XX — Topic Name` + intro line
2. `## Learning Objectives` (4-6 items)
3. `## Key Types` (keep existing)
4. `## Lab — Guided Practice` (quote + table + dotnet test command)
5. `## Exam — Fill in the Blanks` (quote + table + dotnet test command + note about Answers file)
6. Nav links

### Chunks

All chunks (200–251) completed. Exam.cs fill-in-blank and Exam.Answers.cs created for all 50 tutorials. Chunk 251 (final validation) remains as a future polish item.

| Chunk | Tutorials | Status |
|-------|-----------|--------|
| 251 | Final validation: build all, run all tests, verify Exam.cs files don't compile (blanks), Exam.Answers.cs pass | `not-started` |

### Next Chunk

**Chunk 251** — Final validation

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
