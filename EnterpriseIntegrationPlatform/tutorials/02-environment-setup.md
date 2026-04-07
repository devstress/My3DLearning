# Tutorial 02 — Temporal.io Workflow Orchestration

Orchestrate multi-step integration pipelines with Temporal.io — durable workflows that survive crashes, enforce all-or-nothing semantics, and scale horizontally via task queues. This tutorial covers the saga pattern, fan-out/split, and the scalability model that makes Temporal the backbone of reliable integrations.

## Learning Objectives

After completing this tutorial you will be able to:

1. Dispatch a Temporal workflow from `PipelineOrchestrator` and verify envelope-to-input field mapping
2. Explain the saga pattern — success path (persist → validate → ack) and failure path with compensation
3. Implement custom compensation handlers that roll back completed steps in reverse (LIFO) order
4. Fan out a batch of messages into parallel, independent Temporal workflow executions
5. Configure `TemporalOptions` and `PipelineOptions` for task queues, timeouts, and namespaces
6. Wire `PipelineOrchestrator` through Aspire DI with configurable Ack/Nack NATS subjects

## Key Types

```csharp
// src/Demo.Pipeline/ITemporalWorkflowDispatcher.cs — dispatches workflows to Temporal
public interface ITemporalWorkflowDispatcher
{
    Task<IntegrationPipelineResult> DispatchAsync(
        IntegrationPipelineInput input,
        string workflowId,
        CancellationToken cancellationToken = default);
}

// src/Demo.Pipeline/PipelineOrchestrator.cs — converts envelopes to workflow input
public sealed class PipelineOrchestrator : IPipelineOrchestrator
{
    // Maps IntegrationEnvelope<JsonElement> to IntegrationPipelineInput,
    // assigns deterministic workflow ID ("integration-{messageId}"),
    // and dispatches to Temporal
    Task ProcessAsync(IntegrationEnvelope<JsonElement> envelope, CancellationToken ct);
}

// src/Activities/IntegrationPipelineInput.cs — workflow input contract
public sealed record IntegrationPipelineInput(
    Guid MessageId, Guid CorrelationId, Guid? CausationId,
    DateTimeOffset Timestamp, string Source, string MessageType,
    string SchemaVersion, int Priority, string PayloadJson,
    string? MetadataJson, string AckSubject, string NackSubject,
    bool NotificationsEnabled = false);

// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs — saga with compensation
[Workflow]
public class AtomicPipelineWorkflow
{
    // Persist → Validate → Deliver/Compensate — all-or-nothing with rollback
}

// src/Workflow.Temporal/TemporalOptions.cs — worker scalability settings
public sealed class TemporalOptions
{
    public string TaskQueue { get; set; } = "integration-workflows";
    public string Namespace { get; set; } = "default";
    public string ServerAddress { get; set; } = "localhost:15233";
}
```

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how Temporal workflow dispatch, saga
> compensation, fan-out, and scalability settings work. Uses `MockTemporalWorkflowDispatcher`
> for unit-level validation.

| # | Test | Concept |
|---|------|---------|
| 1 | `WorkflowDispatch_EnvelopeFieldsMappedToInput` | Envelope fields map to Temporal input contract |
| 2 | `WorkflowDispatch_WorkflowIdDerivedFromMessageId` | Deterministic workflow ID prevents duplicates |
| 3 | `SagaPattern_SuccessPath_AllStepsComplete` | Success path — persist → validate → ack |
| 4 | `SagaPattern_FailurePath_CompensationTriggered` | Failure path — compensation via nack |
| 5 | `SagaPattern_CustomCompensationHandler_ExecutesRollback` | Custom compensation in reverse (LIFO) order |
| 6 | `FanOut_MultipleMessagesDispatchedIndependently` | Split batch into parallel workflows |
| 7 | `TemporalOptions_DefaultScalabilitySettings` | Task queue, namespace, server address defaults |
| 8 | `PipelineOptions_ConfiguresAckNackSubjects` | Ack/Nack subjects and workflow timeout |
| 9 | `AspireHost_WiresOrchestratorViaDI` | Aspire DI wiring with mock dispatcher |
| 10 | `CorrelationAndCausation_PropagatedThroughWorkflow` | Correlation + causation flow into workflow |

> 💻 [`tests/TutorialLabs/Tutorial02/Lab.cs`](../tests/TutorialLabs/Tutorial02/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial02.Lab"
```

---

## Exam — Assessment Challenges

> **Purpose:** Prove you can apply Temporal workflow orchestration in realistic
> scenarios. Each challenge builds on the previous one.

| Difficulty | Challenge | What you prove |
|------------|-----------|---------------|
| 🟢 Starter | `Starter_SagaCompensation_TracksStepsAndRollsBack` | Multi-step saga with LIFO compensation tracking |
| 🟡 Intermediate | `Intermediate_FanOut_AggregatesResultsFromParallelWorkflows` | Fan-out with per-workflow success/failure aggregation |
| 🔴 Advanced | `Advanced_NotificationsEnabled_AckSubjectConfigured` | Notification-enabled workflow with custom Ack/Nack subjects via DI |

> 💻 [`tests/TutorialLabs/Tutorial02/Exam.cs`](../tests/TutorialLabs/Tutorial02/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial02.Exam"
```

---

**Previous: [← Tutorial 01 — Introduction](01-introduction.md)** | **Next: [Tutorial 03 — Your First Message →](03-first-message.md)**
