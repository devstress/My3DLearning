# Tutorial 08 — Activities and the Pipeline

Activity service interfaces, the Pipes-and-Filters pipeline, and end-to-end message orchestration via Temporal.

## Learning Objectives

After completing this tutorial you will be able to:

1. Validate message payloads using `DefaultMessageValidationService` with JSON schema checks
2. Construct `IntegrationPipelineInput` and `IntegrationPipelineResult` records for workflow execution
3. Build multi-stage pipelines (Validate → Publish, Persist → Validate → Publish)
4. Route validation failures to the Invalid Message Channel as a dead-letter queue
5. Chain four pipeline stages (Persist → Validate → Log → Publish) with audit trail verification

## Key Types

```csharp
// src/Activities/IPersistenceActivityService.cs
public interface IPersistenceActivityService
{
    Task SaveMessageAsync(
        IntegrationPipelineInput input,
        CancellationToken cancellationToken = default);

    Task UpdateDeliveryStatusAsync(
        Guid messageId, Guid correlationId,
        DateTimeOffset recordedAt, string status,
        CancellationToken cancellationToken = default);

    Task SaveFaultAsync(
        Guid messageId, Guid correlationId,
        string messageType, string faultedBy, string reason, int retryCount,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Activities/IMessageValidationService.cs
public interface IMessageValidationService
{
    Task<MessageValidationResult> ValidateAsync(
        string messageType, string payloadJson);
}

public record MessageValidationResult(bool IsValid, string? Reason = null)
{
    public static MessageValidationResult Success { get; } = new(true);
    public static MessageValidationResult Failure(string reason) => new(false, reason);
}
```

```csharp
// src/Activities/INotificationActivityService.cs
public interface INotificationActivityService
{
    Task PublishAckAsync(
        Guid messageId, Guid correlationId, string topic,
        CancellationToken cancellationToken = default);

    Task PublishNackAsync(
        Guid messageId, Guid correlationId, string reason, string topic,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Activities/ICompensationActivityService.cs
public interface ICompensationActivityService
{
    Task<bool> CompensateAsync(Guid correlationId, string stepName);
}
```

```csharp
// src/Contracts/IntegrationPipelineInput.cs
public sealed record IntegrationPipelineInput(
    Guid MessageId, Guid CorrelationId, Guid? CausationId,
    DateTimeOffset Timestamp, string Source, string MessageType,
    string SchemaVersion, int Priority, string PayloadJson,
    string? MetadataJson, string AckSubject, string NackSubject);
```

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how validation, pipeline input/result records,
> and multi-stage pipeline patterns work through DefaultMessageValidationService and MockEndpoint.
> Read the code and comments to understand each concept before moving to the Exam.

| # | Test | Concept |
|---|------|---------|
| 1 | `ValidationStage_ValidJsonPayload_Succeeds` | Valid JSON payload passes validation |
| 2 | `ValidationStage_EmptyPayload_FailsWithReason` | Empty payload rejected with reason |
| 3 | `ValidationStage_NonJsonPayload_FailsWithReason` | Non-JSON payload rejected with reason |
| 4 | `IntegrationPipelineInput_RecordConstruction_AllFields` | Pipeline input record construction |
| 5 | `IntegrationPipelineResult_SuccessAndFailure_RecordSemantics` | Pipeline result success/failure semantics |
| 6 | `PipelineChain_ValidateAndPublish_EndToEnd` | Two-stage pipeline: Validate → Publish |
| 7 | `PipelineChain_ValidationFails_RoutesToInvalidChannel` | Validation failure routes to Invalid Message Channel |
| 8 | `PipelineChain_PersistValidatePublish_ThreeStages` | Three-stage pipeline: Persist → Validate → Publish |
| 9 | `PipelineChain_PersistValidateLogPublish_FourStages` | Four-stage pipeline with audit logging |

> 💻 [`tests/TutorialLabs/Tutorial08/Lab.cs`](../tests/TutorialLabs/Tutorial08/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial08.Lab"
```

---

## Exam — Assessment Challenges

> **Purpose:** Prove you can apply pipeline patterns in realistic scenarios —
> metadata enrichment, DLQ routing on failure, and full multi-stage verification.

| Difficulty | Challenge | What you prove |
|------------|-----------|---------------|
| 🟢 Starter | `Starter_EnrichAndPublish_MetadataPreserved` | Enrich envelope metadata and verify preservation through pipeline |
| 🟡 Intermediate | `Intermediate_ValidationFailure_RoutesDlqAndSkipsOutput` | Route invalid messages to DLQ while skipping normal output |
| 🔴 Advanced | `Advanced_MultiStage_PersistValidatePublishVerify` | Full Persist → Validate → Publish pipeline with audit logging |

> 💻 [`tests/TutorialLabs/Tutorial08/Exam.cs`](../tests/TutorialLabs/Tutorial08/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial08.Exam"
```

---

**Previous: [← Tutorial 07 — Temporal Workflows](07-temporal-workflows.md)** | **Next: [Tutorial 09 — Content-Based Router →](09-content-based-router.md)**
