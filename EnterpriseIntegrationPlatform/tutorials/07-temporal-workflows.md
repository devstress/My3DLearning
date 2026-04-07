# Tutorial 07 — Temporal Workflows

Durable workflow orchestration with `IntegrationPipelineWorkflow`, `AtomicPipelineWorkflow`, and saga compensation.

## Learning Objectives

After completing this tutorial you will be able to:

1. Configure `TemporalOptions` for server address, namespace, and task queue
2. Discover workflow types in the Temporal assembly via reflection
3. Dispatch integration messages through `PipelineOrchestrator` to Temporal workflows
4. Generate deterministic workflow IDs from message identifiers for idempotent dispatch
5. Serialize payload and metadata to JSON for Temporal workflow input
6. Handle dispatch failures gracefully without losing messages

## Key Types

```csharp
// src/Workflow.Temporal/TemporalOptions.cs
public sealed class TemporalOptions
{
    public const string SectionName = "Temporal";
    public string ServerAddress { get; set; } = "localhost:15233";
    public string Namespace { get; set; } = "default";
    public string TaskQueue { get; set; } = "integration-workflows";
}
```

```csharp
// src/Workflow.Temporal/Workflows/IntegrationPipelineWorkflow.cs
[Workflow]
public class IntegrationPipelineWorkflow
{
    [WorkflowRun]
    public async Task<IntegrationPipelineResult> RunAsync(
        IntegrationPipelineInput input) { /* Persist → Log → Validate → Ack/Nack */ }
}
```

```csharp
// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs
[Workflow]
public class AtomicPipelineWorkflow
{
    [WorkflowRun]
    public async Task<AtomicPipelineResult> RunAsync(
        IntegrationPipelineInput input) { /* Persist → Validate → Compensate on failure */ }
}
```

```csharp
// src/Activities/IMessageValidationService.cs
public interface IMessageValidationService
{
    Task<MessageValidationResult> ValidateAsync(string messageType, string payloadJson);
}

public record MessageValidationResult(bool IsValid, string? Reason = null)
{
    public static MessageValidationResult Success { get; } = new(true);
    public static MessageValidationResult Failure(string reason) => new(false, reason);
}
```

```csharp
// src/Activities/IMessageLoggingService.cs
public interface IMessageLoggingService
{
    Task LogAsync(Guid messageId, string messageType, string stage);
}
```

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how Temporal workflow configuration,
> orchestrator dispatch, and failure handling work through MockTemporalWorkflowDispatcher.
> Read the code and comments to understand each concept before moving to the Exam.

| # | Test | Concept |
|---|------|---------|
| 1 | `TemporalOptions_Defaults_TaskQueueAndNamespace` | TemporalOptions defaults and section name |
| 2 | `WorkflowTypes_AllFourExistInAssembly` | Workflow type discovery via reflection |
| 3 | `PipelineOptions_Defaults_AckNackSubjects` | PipelineOptions Ack/Nack subject defaults |
| 4 | `PipelineOrchestrator_DispatchesCorrectInput` | Orchestrator converts envelope to pipeline input |
| 5 | `PipelineOrchestrator_WorkflowId_DerivedFromMessageId` | Deterministic workflow ID from MessageId |
| 6 | `PipelineOrchestrator_SerializesPayloadAndMetadata` | Payload and metadata serialized to JSON |
| 7 | `PipelineOrchestrator_MapsPriorityAsInt` | MessagePriority enum mapped to integer |
| 8 | `PipelineOrchestrator_DispatchFailure_HandledGracefully` | Failure result handled without throwing |
| 9 | `PipelineOrchestrator_MultipleDispatches_CountTracked` | Multiple dispatches tracked for assertions |

> 💻 [`tests/TutorialLabs/Tutorial07/Lab.cs`](../tests/TutorialLabs/Tutorial07/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial07.Lab"
```

---

## Exam — Assessment Challenges

> **Purpose:** Prove you can apply Temporal workflow patterns in realistic scenarios —
> host-based DI wiring, failure handling, and correlation propagation.

| Difficulty | Challenge | What you prove |
|------------|-----------|---------------|
| 🟢 Starter | `Starter_AspireHost_OrchestratorDispatchesViaDI` | Wire PipelineOrchestrator via DI and dispatch through Aspire host |
| 🟡 Intermediate | `Intermediate_WorkflowFailure_LogsWarning` | Handle workflow failure result gracefully |
| 🔴 Advanced | `Advanced_CorrelationAndCausation_PropagatedToInput` | Correlation and causation IDs propagated through dispatch |

> 💻 [`tests/TutorialLabs/Tutorial07/Exam.cs`](../tests/TutorialLabs/Tutorial07/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial07.Exam"
```

---

**Previous: [← Tutorial 06 — Messaging Channels](06-messaging-channels.md)** | **Next: [Tutorial 08 — Activities and the Pipeline →](08-activities-pipeline.md)**
