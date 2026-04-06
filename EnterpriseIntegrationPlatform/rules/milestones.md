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

✅ Phases 1–24 complete — see `rules/completion-log.md` for full history.

48 src projects. All 50 tutorials rewritten with BizTalk-style Lab + Exam exercises focused on EIP patterns, scalability, and atomicity.

---

## Phase 27 — Coding Tutorial Labs & Exams

**Goal:** Convert all 50 tutorials from conceptual/MCQ format to coding-only format. Each tutorial gets:
- `tests/TutorialLabs/TutorialXX/Lab.cs` — Complete, runnable NUnit test class demonstrating the pattern
- `tests/TutorialLabs/TutorialXX/Exam.cs` — Coding exam challenges (NOT multiple choice)
- Updated tutorial `.md` file pointing to the implementation folder

**Project:** `tests/TutorialLabs/TutorialLabs.csproj` (added to solution, references all src projects)

**Key API findings for remaining chunks:**
- **DynamicRouter**: implements `IDynamicRouter` + `IRouterControlChannel`. Constructor: `IMessageBrokerProducer`, `IOptions<DynamicRouterOptions>`, `ILogger<DynamicRouter>`. Methods: `RegisterAsync()`, `UnregisterAsync()`, `RouteAsync<T>()`, `GetRoutingTable()`.
- **RecipientListRouter**: implements `IRecipientList`. Constructor: `IMessageBrokerProducer`, `IOptions<RecipientListOptions>`, `ILogger<RecipientListRouter>`. Uses `RecipientListRule` with `RoutingOperator`.
- **RoutingSlipRouter**: implements `IRoutingSlipRouter`. Constructor: `IEnumerable<IRoutingSlipStepHandler>`, `IMessageBrokerProducer`, `ILogger<RoutingSlipRouter>`. Handlers implement `IRoutingSlipStepHandler`.
- **Process Manager**: `PipelineOrchestrator` and `ITemporalWorkflowDispatcher` in `Demo.Pipeline`. Uses `IntegrationPipelineInput`/`IntegrationPipelineResult` from Activities.
- **MessageTranslator<TIn,TOut>**: takes `IPayloadTransform<TIn,TOut>`, `IMessageBrokerProducer`, `IOptions<TranslatorOptions>`, `ILogger`. `FuncPayloadTransform<TIn,TOut>` wraps a delegate.
- **Transform steps**: `JsonToXmlStep`, `XmlToJsonStep`, `RegexReplaceStep`, `JsonPathFilterStep`, `TransformPipeline`.

| Chunk | Scope | Status |
|-------|-------|--------|
| Chunk | Scope | Status |
|-------|-------|--------|
| 101 | Update all 50 tutorial .md files — replace MCQ Exam sections with "See coding exam" pointers, update Lab sections to reference TutorialLabs | not-started |
| 102 | Update tutorials/README.md — document new coding-only format and TutorialLabs project | not-started |

**Next chunk:** 101

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
