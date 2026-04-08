# Tutorial 16 — Transform Pipeline

Chain ordered `ITransformStep` instances through an `ITransformPipeline` to transform payloads in sequence.

---

## Learning Objectives

1. Understand the Transform Pipeline pattern and how ordered steps transform payloads in sequence
2. Create an `ITransformPipeline` with one or more `ITransformStep` implementations
3. Verify step ordering, payload mutation, and `StepsApplied` count after execution
4. Configure `TransformOptions` to disable the pipeline or set payload size limits
5. Handle step failures gracefully when `StopOnStepFailure` is disabled
6. Inspect `TransformResult` metadata including `ContentType` and step-applied markers

---

## Key Types

```csharp
// src/Processing.Transform/ITransformPipeline.cs
public interface ITransformPipeline
{
    Task<TransformResult> ExecuteAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Transform/ITransformStep.cs
public interface ITransformStep
{
    string Name { get; }
    Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Transform/TransformContext.cs
public sealed class TransformContext
{
    public string Payload { get; }
    public string ContentType { get; }
    public Dictionary<string, string> Metadata { get; init; } = new();

    public TransformContext WithPayload(string payload, string contentType) => ...;
    public TransformContext WithPayload(string payload) => ...;
}
```

```csharp
// src/Processing.Transform/TransformResult.cs
public sealed record TransformResult(
    string Payload,
    string ContentType,
    int StepsApplied,
    IReadOnlyDictionary<string, string> Metadata);
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Transform Pipeline concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Pipeline_SingleStep_TransformsPayload` | Single step transforms payload and reports StepsApplied |
| 2 | `Pipeline_MultipleSteps_ChainsTransformations` | Multiple steps chain transformations in order |
| 3 | `Pipeline_Disabled_ReturnsInputUnchanged` | Disabled pipeline returns input unchanged |
| 4 | `Pipeline_StepFailure_SkippedWhenNotStopOnFailure` | Step failure skipped when StopOnStepFailure is false |
| 5 | `Pipeline_MaxPayloadSize_RejectsOversized` | Payload exceeding max size throws InvalidOperationException |
| 6 | `Pipeline_E2E_PublishTransformedToNatsEndpoint` | End-to-end transform and publish via real NATS |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial16.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Transform Pipeline pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_RegexReplace_MasksPhoneNumbers` | 🟢 Starter |
| 2 | `Intermediate_JsonPathFilter_RetainsOnlySpecifiedPaths` | 🟡 Intermediate |
| 3 | `Advanced_MultiStep_TransformAndPublish` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial16.Exam"
```

---

**Previous: [← Tutorial 15 — Message Translator](15-message-translator.md)** | **Next: [Tutorial 17 — Normalizer →](17-normalizer.md)**
