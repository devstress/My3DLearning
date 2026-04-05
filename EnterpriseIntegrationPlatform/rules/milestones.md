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

✅ Phases 1–23 complete — see `rules/completion-log.md` for full history.

48 src projects. All unit test coverage complete.

**Next chunk:** 093

---

### Phase 24 — Tutorial Exercise Rewrite (BizTalk-style Labs + Exams)

**Scope:** Tutorials 01-05 already rewritten. Tutorials 06-50 still use old `## Exercises` format with theoretical questions and test-writing steps. Rewrite all 45 remaining tutorials to use practical **Lab** (EIP patterns, scalability, atomicity) + certification-style **Exam** format. No test mentions in exercises.

**Previous session completed:**
- Audited all 50 tutorials against source code, fixed 9 code mismatches (tutorials 02, 10, 14, 25, 28, 32, 42, 49, 50)
- Removed all hardcoded test/project counts from tutorials
- Rewrote exercises for tutorials 01-05

#### Chunk 093 — Tutorial Exercise Rewrite: 06-10

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite `## Exercises` → `## Lab` + `## Exam` for tutorials 06 (Messaging Channels), 07 (Temporal Workflows), 08 (Activities Pipeline), 09 (Content-Based Router), 10 (Message Filter) |
| Files | `tutorials/06-messaging-channels.md`, `tutorials/07-temporal-workflows.md`, `tutorials/08-activities-pipeline.md`, `tutorials/09-content-based-router.md`, `tutorials/10-message-filter.md` |

#### Chunk 094 — Tutorial Exercise Rewrite: 11-15

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite exercises for tutorials 11 (Dynamic Router), 12 (Recipient List), 13 (Routing Slip), 14 (Process Manager), 15 (Message Translator) |
| Files | `tutorials/11-dynamic-router.md` through `tutorials/15-message-translator.md` |

#### Chunk 095 — Tutorial Exercise Rewrite: 16-20

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite exercises for tutorials 16 (Transform Pipeline), 17 (Normalizer), 18 (Content Enricher), 19 (Content Filter), 20 (Splitter) |
| Files | `tutorials/16-transform-pipeline.md` through `tutorials/20-splitter.md` |

#### Chunk 096 — Tutorial Exercise Rewrite: 21-25

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite exercises for tutorials 21 (Aggregator), 22 (Scatter-Gather), 23 (Request-Reply), 24 (Retry Framework), 25 (Dead Letter Queue) |
| Files | `tutorials/21-aggregator.md` through `tutorials/25-dead-letter-queue.md` |

#### Chunk 097 — Tutorial Exercise Rewrite: 26-30

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite exercises for tutorials 26 (Message Replay), 27 (Resequencer), 28 (Competing Consumers), 29 (Throttle), 30 (Rule Engine) |
| Files | `tutorials/26-message-replay.md` through `tutorials/30-rule-engine.md` |

#### Chunk 098 — Tutorial Exercise Rewrite: 31-35

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite exercises for tutorials 31 (Event Sourcing), 32 (Multi-Tenancy), 33 (Security), 34 (HTTP Connector), 35 (SFTP Connector) |
| Files | `tutorials/31-event-sourcing.md` through `tutorials/35-connector-sftp.md` |

#### Chunk 099 — Tutorial Exercise Rewrite: 36-40

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite exercises for tutorials 36 (Email Connector), 37 (File Connector), 38 (OpenTelemetry), 39 (Message Lifecycle), 40 (RAG Ollama) |
| Files | `tutorials/36-connector-email.md` through `tutorials/40-rag-ollama.md` |

#### Chunk 100 — Tutorial Exercise Rewrite: 41-45

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite exercises for tutorials 41 (OpenClaw Web), 42 (Configuration), 43 (Kubernetes), 44 (Disaster Recovery), 45 (Performance Profiling) |
| Files | `tutorials/41-openclaw-web.md` through `tutorials/45-performance-profiling.md` |

#### Chunk 101 — Tutorial Exercise Rewrite: 46-50

| Field | Value |
|-------|-------|
| Status | `not-started` |
| Goal | Rewrite exercises for tutorials 46 (Complete Integration), 47 (Saga Compensation), 48 (Notification Use Cases), 49 (Testing Integrations), 50 (Best Practices) |
| Files | `tutorials/46-complete-integration.md` through `tutorials/50-best-practices.md` |

## Next Chunk

093

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
