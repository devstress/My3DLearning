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

✅ All chunks (200–251) complete — see `rules/completion-log.md` for full history.

---

## Phase 30 — Quality Hardening (Audit-Driven)

> **Origin:** Independent audit of the EIP codebase identified concrete weak spots.
> Every chunk in this phase addresses a verified gap with production-quality code
> and new unit tests. No scaffolding, no placeholders, no conceptual work.

### Chunk 300 — ✅ Done — see `rules/completion-log.md`

### Chunk 301 — NATS JetStream provider hardening

| Status | `not-started` |
|---|---|
| **Goal** | Harden `Ingestion.Nats` from demo-grade to production-grade. |
| **Changes** | (a) Add `NatsOptions` class (IOptions pattern) with `Url`, `MaxReconnectRetries`, `ReconnectWaitMs`, `StreamCreateRetries`. (b) Implement `IAsyncDisposable` on both Producer and Consumer with real resource cleanup. (c) Add `NatsHealthCheck : IHealthCheck` that verifies JetStream API responsiveness. (d) Update `NatsServiceExtensions` to use `IOptions<NatsOptions>`, register health check, validate configuration. (e) Add OpenTelemetry `ActivitySource` tracing on publish/consume. (f) Fix infinite-loop risk in `EnsureStreamAsync` — throw after max retries exhausted. |
| **Tests** | New unit tests in `tests/UnitTests/`: `NatsOptionsTests.cs` (validation, defaults), `NatsHealthCheckTests.cs` (healthy/unhealthy scenarios), `NatsServiceExtensionsTests.cs` (registration, health check, config validation). Minimum 12 new tests. |
| **Acceptance** | `dotnet build` 0 warnings. All existing + new tests pass. NATS provider LOC increases from ~237 to ~400+. |

### Chunk 302 — Kafka provider hardening

| Status | `not-started` |
|---|---|
| **Goal** | Harden `Ingestion.Kafka` from demo-grade to production-grade. |
| **Changes** | (a) Add `KafkaOptions` class (IOptions pattern) with `BootstrapServers`, `Acks`, `EnableIdempotence`, `CompressionType`, `LingerMs`, `BatchSize`, `SessionTimeoutMs`, `GroupId`, `AutoOffsetReset`, `EnableAutoCommit`. (b) Implement `IAsyncDisposable` on Producer (flush + dispose) and Consumer (close + dispose). (c) Add `KafkaHealthCheck : IHealthCheck` that queries broker metadata. (d) Update `KafkaServiceExtensions` to use `IOptions<KafkaOptions>`, register health check, apply tuning defaults. (e) Remove `Program.cs` stub or replace with proper hosted-service wiring. (f) Add OpenTelemetry `ActivitySource` tracing on produce/consume. |
| **Tests** | New/expanded unit tests: `KafkaOptionsTests.cs` (validation, defaults, tuning), `KafkaHealthCheckTests.cs` (healthy/unhealthy), update existing `KafkaServiceExtensionsTests.cs`. Minimum 12 new tests. |
| **Acceptance** | `dotnet build` 0 warnings. All existing + new tests pass. Kafka provider LOC increases from ~207 to ~450+. |

### Chunk 303 — Pulsar provider hardening

| Status | `not-started` |
|---|---|
| **Goal** | Harden `Ingestion.Pulsar` from demo-grade to production-grade. Fix critical producer-per-message anti-pattern. |
| **Changes** | (a) Add `PulsarOptions` class (IOptions pattern) with `ServiceUrl`, `OperationTimeoutMs`, `ConnectionTimeoutMs`, `KeepAliveIntervalMs`. (b) **Fix PulsarProducer**: cache producer instances per topic using `ConcurrentDictionary<string, IProducer<byte[]>>` — stop creating a new producer per `PublishAsync` call. (c) Implement `IAsyncDisposable` on Producer (dispose cached producers) and Consumer (dispose consumer). (d) Add `PulsarHealthCheck : IHealthCheck`. (e) Update `PulsarServiceExtensions` to use `IOptions<PulsarOptions>`, register health check, validate URL. (f) Add OpenTelemetry `ActivitySource` tracing. |
| **Tests** | New/expanded unit tests: `PulsarOptionsTests.cs`, `PulsarHealthCheckTests.cs`, update existing `PulsarServiceExtensionsTests.cs`, new `PulsarProducerCachingTests.cs` (verify producer reuse per topic). Minimum 12 new tests. |
| **Acceptance** | `dotnet build` 0 warnings. All existing + new tests pass. Pulsar producer-per-message bug eliminated. Pulsar provider LOC increases from ~189 to ~400+. |

### Chunk 304 — Ingestion.Postgres unit tests

| Status | `not-started` |
|---|---|
| **Goal** | `Ingestion.Postgres` has 611 LOC of production code but **zero unit tests** in `tests/UnitTests/`. Add comprehensive unit test coverage. |
| **Tests** | New files in `tests/UnitTests/`: `PostgresBrokerProducerTests.cs` (publish serialization, null guards, topic validation), `PostgresBrokerConsumerTests.cs` (deserialization, handler invocation, predicate filtering, dispose), `PostgresTransactionalClientTests.cs` (commit/rollback semantics, timeout, compensation), `PostgresConnectionFactoryTests.cs` (connection string validation, options binding), `PostgresBrokerOptionsTests.cs` (defaults, validation), `PostgresServiceExtensionsTests.cs` (DI registration, health check). Minimum 25 new tests. |
| **Acceptance** | `dotnet build` 0 warnings. All new tests pass. Postgres provider has ≥25 unit tests covering all 6 source files. |

### Chunk 305 — Security project unit tests

| Status | `not-started` |
|---|---|
| **Goal** | `Security` project has 356 LOC across 6 source files (InputSanitizer, PayloadSizeGuard, JwtOptions, PayloadTooLargeException, SecurityServiceExtensions) but **zero unit tests** in `tests/UnitTests/`. Add comprehensive coverage. |
| **Tests** | New files in `tests/UnitTests/`: `InputSanitizerTests.cs` (XSS stripping, SQL injection patterns, null/empty input, HTML encoding, script tag removal, event handler attributes), `PayloadSizeGuardTests.cs` (within-limit pass, over-limit throw PayloadTooLargeException, boundary conditions, zero-length, null), `JwtOptionsTests.cs` (defaults, property binding), `SecurityServiceExtensionsTests.cs` (DI registration verification). Minimum 20 new tests. |
| **Acceptance** | `dotnet build` 0 warnings. All new tests pass. Security project has ≥20 unit tests. |

### Chunk 306 — NATS JetStream NatsServiceExtensionsTests expansion

| Status | `not-started` |
|---|---|
| **Goal** | Existing `NatsServiceExtensionsTests.cs` has only basic DI checks. Expand to cover new options-pattern registration, health check registration, and configuration validation from Chunk 301. |
| **Tests** | Expand `tests/UnitTests/NatsServiceExtensionsTests.cs`: verify `IOptions<NatsOptions>` is resolvable, health check registered in `IHealthChecksBuilder`, null/empty connection string throws, default options have expected values. Also add `NatsProducerDisposeTests.cs` and `NatsConsumerDisposeTests.cs` verifying `IAsyncDisposable` cleanup. Minimum 10 new tests. |
| **Acceptance** | `dotnet build` 0 warnings. All tests pass. |

### Chunk 307 — Broker health check integration smoke test

| Status | `not-started` |
|---|---|
| **Goal** | Add a new `BrokerHealthCheckTests.cs` in `tests/UnitTests/` that verifies all 4 broker health checks (NATS, Kafka, Pulsar, Postgres) can be constructed, return `Healthy` when mocked connection succeeds, and return `Unhealthy` when mocked connection fails. |
| **Tests** | `tests/UnitTests/BrokerHealthCheckTests.cs` — 4 healthy + 4 unhealthy = 8 tests. Uses NSubstitute mocks to simulate broker connectivity without real infrastructure. |
| **Acceptance** | `dotnet build` 0 warnings. 8 new tests pass. |

### Chunk 308 — README accuracy pass

| Status | `not-started` |
|---|---|
| **Goal** | Full audit of README.md claims vs. reality. Fix all discrepancies. |
| **Changes** | (a) Update project structure test counts to match actual `dotnet test` output after all Phase 30 chunks. (b) Update "2,000+ automated tests" claim to exact number. (c) Update tech stack versions (Aspire, OpenTelemetry, etc.) if any are stale. (d) Verify all `docs/` links resolve to existing files. (e) Verify the "49 src projects" count is still accurate. |
| **Acceptance** | Every number in README.md is accurate and verifiable by running `dotnet test` and `find`. |

### Next Chunk

**Chunk 301** — NATS JetStream provider hardening.

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
