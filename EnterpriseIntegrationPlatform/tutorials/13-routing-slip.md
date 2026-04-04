# Tutorial 13 — Routing Slip

## What You'll Learn

- The EIP Routing Slip pattern for per-message dynamic pipelines
- How `RoutingSlip` and `RoutingSlipStep` attach a processing plan to a message
- How `IRoutingSlipRouter` executes the current step and calls `Advance()`
- How `IRoutingSlipStepHandler` dispatches to named step implementations
- The difference between a routing slip and a fixed pipeline

---

## EIP Pattern: Routing Slip

> *"Attach a Routing Slip to each message, specifying the sequence of processing steps."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  Envelope + RoutingSlip [Validate, Transform, Enrich]
     │
     ▼
  ┌──────────┐     ┌───────────┐     ┌─────────┐
  │ Validate │────▶│ Transform │────▶│ Enrich  │────▶ Done
  └──────────┘     └───────────┘     └─────────┘
  slip.Advance()   slip.Advance()    slip.IsComplete
```

Unlike a fixed pipeline where every message follows the same path, a routing slip lets **each message carry its own processing plan**. Different messages can visit different steps.

---

## Platform Implementation

### RoutingSlip & RoutingSlipStep (Contracts)

```csharp
// src/Contracts/RoutingSlip.cs
public sealed record RoutingSlip(IReadOnlyList<RoutingSlipStep> Steps)
{
    public const string MetadataKey = "RoutingSlip";
    public bool IsComplete => Steps.Count == 0;
    public RoutingSlipStep? CurrentStep => Steps.Count > 0 ? Steps[0] : null;
    public RoutingSlip Advance() =>
        new RoutingSlip(Steps.Skip(1).ToList().AsReadOnly());
}

// src/Contracts/RoutingSlipStep.cs
public sealed record RoutingSlipStep(
    string StepName,
    string? DestinationTopic = null,
    IReadOnlyDictionary<string, string>? Parameters = null);
```

### IRoutingSlipRouter

```csharp
// src/Processing.Routing/IRoutingSlipRouter.cs
public interface IRoutingSlipRouter
{
    Task<RoutingSlipStepResult> ExecuteCurrentStepAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

### IRoutingSlipStepHandler

```csharp
// src/Processing.Routing/IRoutingSlipStepHandler.cs
public interface IRoutingSlipStepHandler
{
    string StepName { get; }
    Task<bool> HandleAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyDictionary<string, string>? parameters,
        CancellationToken cancellationToken = default);
}
```

### RoutingSlipStepResult

```csharp
public sealed record RoutingSlipStepResult(
    string StepName,
    bool Succeeded,
    string? FailureReason,
    RoutingSlip RemainingSlip,
    string? ForwardedToTopic);
```

After execution, the router calls `Advance()` to consume the completed step. If `DestinationTopic` is set on the current step, the message is forwarded to that topic; otherwise the next step runs in-process.

---

## Scalability Dimension

Each step can be a separate service consuming from its own topic. Step handlers are resolved from DI by `StepName`, so new steps can be deployed independently. The slip travels with the message — no central orchestrator is required. This enables **per-step horizontal scaling**: a "Transform" step with high load can have 10 replicas while a "Validate" step needs only 2.

---

## Atomicity Dimension

The routing slip is stored in the envelope's `Metadata` dictionary as serialised JSON. After each step the advanced slip is written back to metadata before forwarding. If a step fails, the message is Nacked and redelivered with the **original** slip (the step is retried, not skipped). If a step handler returns `false`, the `RoutingSlipStepResult.Succeeded` is `false` and the failure reason is recorded.

---

## Exercises

1. Build a `RoutingSlip` with steps: Validate → Transform → Deliver. The Transform step needs a parameter `"targetFormat" = "XML"`. Write the C# construction code.

2. A message has completed Validate and Transform but crashes during Deliver. What does the `RemainingSlip` look like when the message is redelivered?

3. Compare a routing slip to a Temporal workflow pipeline. When would you choose a slip over a workflow?

---

**Previous: [← Tutorial 12 — Recipient List](12-recipient-list.md)** | **Next: [Tutorial 14 — Process Manager →](14-process-manager.md)**
