# Tutorial 13 — Routing Slip

Each message carries its own processing itinerary — steps are executed sequentially and the slip advances after each one.

---

## Learning Objectives

1. Understand the Routing Slip pattern and how it attaches a processing itinerary to each message
2. Execute a single routing-slip step and verify successful forwarding to a destination topic
3. Advance through a multi-step slip, confirming step-by-step consumption
4. Pass step-specific parameters to handlers and verify they are received correctly
5. Handle step failures and missing handlers gracefully without crashing the pipeline
6. Detect a missing routing slip on an envelope and confirm the expected exception

---

## Key Types

```csharp
// src/Contracts/RoutingSlip.cs
public sealed record RoutingSlip(IReadOnlyList<RoutingSlipStep> Steps)
{
    public const string MetadataKey = "RoutingSlip";
    public bool IsComplete => Steps.Count == 0;
    public RoutingSlipStep? CurrentStep => Steps.Count > 0 ? Steps[0] : null;

    public RoutingSlip Advance()
    {
        if (IsComplete)
            throw new InvalidOperationException("Cannot advance a completed routing slip.");
        return new RoutingSlip(Steps.Skip(1).ToList().AsReadOnly());
    }
}

// src/Contracts/RoutingSlipStep.cs
public sealed record RoutingSlipStep(
    string StepName,
    string? DestinationTopic = null,
    IReadOnlyDictionary<string, string>? Parameters = null);

// src/Processing.Routing/IRoutingSlipRouter.cs
public interface IRoutingSlipRouter
{
    Task<RoutingSlipStepResult> ExecuteCurrentStepAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}

// src/Processing.Routing/IRoutingSlipStepHandler.cs
public interface IRoutingSlipStepHandler
{
    string StepName { get; }
    Task<bool> HandleAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyDictionary<string, string>? parameters,
        CancellationToken cancellationToken = default);
}

// src/Processing.Routing/RoutingSlipStepResult.cs
public sealed record RoutingSlipStepResult(
    string StepName,
    bool Succeeded,
    string? FailureReason,
    RoutingSlip RemainingSlip,
    string? ForwardedToTopic);
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Routing Slip concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `ExecuteStep_SingleStep_SucceedsAndForwards` | Execute one step and verify forwarding to a destination topic |
| 2 | `ExecuteStep_NoDestination_CompletesInProcess` | Step without a destination completes without publishing |
| 3 | `ExecuteStep_HandlerFails_ReturnsFalseResult` | Handler returning false produces a failed result |
| 4 | `ExecuteStep_NoHandlerRegistered_FailsGracefully` | Missing handler is detected and reported gracefully |
| 5 | `ExecuteStep_MultiStepSlip_AdvancesCorrectly` | Multi-step slip advances and preserves remaining steps |
| 6 | `ExecuteStep_WithParameters_PassesParametersToHandler` | Step-specific parameters are forwarded to the handler |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial13.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_FullPipeline_ExecutesAllStepsSequentially` | 🟢 Starter | FullPipeline — ExecutesAllStepsSequentially |
| 2 | `Intermediate_PartialFailure_StopsAtFailedStep` | 🟡 Intermediate | PartialFailure — StopsAtFailedStep |
| 3 | `Advanced_MissingSlip_ThrowsInvalidOperation` | 🔴 Advanced | MissingSlip — ThrowsInvalidOperation |

> 💻 [`tests/TutorialLabs/Tutorial13/Exam.cs`](../tests/TutorialLabs/Tutorial13/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial13.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial13.ExamAnswers"
```
---

**Previous: [← Tutorial 12 — Recipient List](12-recipient-list.md)** | **Next: [Tutorial 14 — Process Manager →](14-process-manager.md)**
