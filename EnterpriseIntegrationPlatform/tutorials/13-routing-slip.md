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

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Routing Slip pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_FullPipeline_ExecutesAllStepsSequentially` | 🟢 Starter |
| 2 | `Intermediate_PartialFailure_StopsAtFailedStep` | 🟡 Intermediate |
| 3 | `Advanced_MissingSlip_ThrowsInvalidOperation` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial13.Exam"
```

---

**Previous: [← Tutorial 12 — Recipient List](12-recipient-list.md)** | **Next: [Tutorial 14 — Process Manager →](14-process-manager.md)**
