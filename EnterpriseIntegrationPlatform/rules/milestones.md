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

---

## Phase 29 — Tutorial Redesign (Lab / Exam / Exam.Answers)

> **Goal:** Every tutorial gets three test files:
>
> - **Lab.cs** — Guided practice (complete, runnable, concept-per-test)
> - **Exam.cs** — Fill-in-the-blank (student version with `// TODO:` placeholders where key lines are blanked out)
> - **Exam.Answers.cs** — Complete answer key (full working code, compiles and passes)
>
> Each tutorial markdown gets: Learning Objectives, Key Types, Lab table, Exam table, no old Exercises section.

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

| Chunk | Tutorials | Status |
|-------|-----------|--------|
| 200 | Rescan & clean: remove old Exercises from all 50 markdowns, remove `Substitute.For` mock code from markdown, verify Key Types sections are correct | `not-started` |
| 201 | Tutorial 01 — Introduction to Enterprise Integration | `not-started` |
| 202 | Tutorial 02 — Temporal.io Workflow Orchestration | `not-started` |
| 203 | Tutorial 03 — Your First Message | `not-started` |
| 204 | Tutorial 04 — The Integration Envelope | `not-started` |
| 205 | Tutorial 05 — Message Brokers | `not-started` |
| 206 | Tutorial 06 — Messaging Channels | `not-started` |
| 207 | Tutorial 07 — Temporal Workflows | `not-started` |
| 208 | Tutorial 08 — Activities and the Pipeline | `not-started` |
| 209 | Tutorial 09 — Content-Based Router | `not-started` |
| 210 | Tutorial 10 — Message Filter | `not-started` |
| 211 | Tutorial 11 — Dynamic Router | `not-started` |
| 212 | Tutorial 12 — Recipient List | `not-started` |
| 213 | Tutorial 13 — Routing Slip | `not-started` |
| 214 | Tutorial 14 — Process Manager | `not-started` |
| 215 | Tutorial 15 — Message Translator | `not-started` |
| 216 | Tutorial 16 — Transform Pipeline (REFERENCE EXAMPLE — do this first) | `not-started` |
| 217 | Tutorial 17 — Normalizer | `not-started` |
| 218 | Tutorial 18 — Content Enricher | `not-started` |
| 219 | Tutorial 19 — Content Filter | `not-started` |
| 220 | Tutorial 20 — Splitter | `not-started` |
| 221 | Tutorial 21 — Aggregator | `not-started` |
| 222 | Tutorial 22 — Scatter-Gather | `not-started` |
| 223 | Tutorial 23 — Request-Reply | `not-started` |
| 224 | Tutorial 24 — Retry Framework | `not-started` |
| 225 | Tutorial 25 — Dead Letter Queue | `not-started` |
| 226 | Tutorial 26 — Message Replay | `not-started` |
| 227 | Tutorial 27 — Resequencer | `not-started` |
| 228 | Tutorial 28 — Competing Consumers | `not-started` |
| 229 | Tutorial 29 — Throttle & Rate Limiting | `not-started` |
| 230 | Tutorial 30 — Rule Engine | `not-started` |
| 231 | Tutorial 31 — Event Sourcing | `not-started` |
| 232 | Tutorial 32 — Multi-Tenancy | `not-started` |
| 233 | Tutorial 33 — Security | `not-started` |
| 234 | Tutorial 34 — HTTP Connector | `not-started` |
| 235 | Tutorial 35 — SFTP Connector | `not-started` |
| 236 | Tutorial 36 — Email Connector | `not-started` |
| 237 | Tutorial 37 — File Connector | `not-started` |
| 238 | Tutorial 38 — OpenTelemetry | `not-started` |
| 239 | Tutorial 39 — Message Lifecycle | `not-started` |
| 240 | Tutorial 40 — RAG with Ollama | `not-started` |
| 241 | Tutorial 41 — OpenClaw Web UI | `not-started` |
| 242 | Tutorial 42 — Configuration | `not-started` |
| 243 | Tutorial 43 — Kubernetes Deployment | `not-started` |
| 244 | Tutorial 44 — Disaster Recovery | `not-started` |
| 245 | Tutorial 45 — Performance Profiling | `not-started` |
| 246 | Tutorial 46 — Complete End-to-End Integration | `not-started` |
| 247 | Tutorial 47 — Saga Compensation | `not-started` |
| 248 | Tutorial 48 — Notification Use Cases | `not-started` |
| 249 | Tutorial 49 — Testing Integrations | `not-started` |
| 250 | Tutorial 50 — Best Practices & Design Guidelines | `not-started` |
| 251 | Final validation: build all, run all tests, verify Exam.cs files don't compile (blanks), Exam.Answers.cs pass | `not-started` |

### Next Chunk

**Chunk 200** — Rescan & clean all 50 tutorials (remove old Exercises, Substitute.For code, verify markdown)

Then **Chunk 216** — Tutorial 16 as the reference example for the new Exam format.

Then chunks 201–215, 217–250 can follow in any order.

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
