# Tutorial 14 — Process Manager

Centralised stateful orchestration via Temporal workflows — decides the next step based on intermediate results and compensates on failure.

---

## Learning Objectives

1. Understand the Process Manager pattern and how it centralises stateful orchestration via Temporal workflows
2. Convert an `IntegrationEnvelope` into an `IntegrationPipelineInput` for Temporal dispatch
3. Map envelope fields (source, message type, correlation ID) to pipeline input properties
4. Derive a deterministic, idempotent workflow ID from the message identifier
5. Serialize payload and metadata into JSON for cross-boundary transport
6. Configure Ack/Nack subjects via `PipelineOptions` for asynchronous result handling

---

## Key Types

```csharp
// src/Contracts/IntegrationPipelineInput.cs
public sealed record IntegrationPipelineInput(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset Timestamp,
    string Source,
    string MessageType,
    string SchemaVersion,
    int Priority,
    string PayloadJson,
    string? MetadataJson,
    string AckSubject,
    string NackSubject)
{
    public bool NotificationsEnabled { get; init; }
}

// src/Contracts/IntegrationPipelineResult.cs
public sealed record IntegrationPipelineResult(
    Guid MessageId,
    bool IsSuccess,
    string? FailureReason = null);

// src/Demo.Pipeline/PipelineOrchestrator.cs
public sealed class PipelineOrchestrator
{
    public Task ProcessAsync<T>(IntegrationEnvelope<T> envelope) { ... }
}

// src/Demo.Pipeline/ITemporalWorkflowDispatcher.cs
public interface ITemporalWorkflowDispatcher
{
    Task<IntegrationPipelineResult> DispatchAsync(
        IntegrationPipelineInput input,
        string workflowId,
        CancellationToken cancellationToken = default);
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Process Manager concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `ProcessAsync_DispatchesCorrectWorkflowId` | Workflow ID derived deterministically from MessageId |
| 2 | `ProcessAsync_MapsEnvelopeFieldsToInput` | Core envelope fields mapped to pipeline input |
| 3 | `ProcessAsync_SerializesPayloadAsJson` | Payload serialized as JSON string |
| 4 | `ProcessAsync_WithMetadata_SerializesMetadataJson` | Metadata dictionary serialized to JSON |
| 5 | `ProcessAsync_EmptyMetadata_SetsMetadataJsonNull` | Empty/null metadata maps to null JSON |
| 6 | `ProcessAsync_SetsAckAndNackSubjectsFromOptions` | Ack/Nack subjects sourced from PipelineOptions |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial14.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Process Manager pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_PriorityMapping_CastsEnumToInt` | 🟢 Starter |
| 2 | `Intermediate_IdempotentWorkflowId_DeterministicFromMessageId` | 🟡 Intermediate |
| 3 | `Advanced_CausationIdAndTimestamp_PreservedInInput` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial14.Exam"
```

---

**Previous: [← Tutorial 13 — Routing Slip](13-routing-slip.md)** | **Next: [Tutorial 15 — Message Translator →](15-message-translator.md)**
