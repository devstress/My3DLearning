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

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial13/Lab.cs`](../tests/TutorialLabs/Tutorial13/Lab.cs)

**Objective:** Build a Routing Slip, trace failure recovery with partial completion, and compare the Routing Slip pattern's **scalability** against Process Manager workflows.

### Step 1: Build a Routing Slip with Parameters

Write C# code to construct a `RoutingSlip` with three steps:

```csharp
var slip = new RoutingSlip([
    new RoutingSlipStep("Validate"),
    new RoutingSlipStep("Transform", Parameters: new Dictionary<string, string>
    {
        ["targetFormat"] = "XML",
        ["schemaVersion"] = "2.0"
    }),
    new RoutingSlipStep("Deliver", Parameters: new Dictionary<string, string>
    {
        ["endpoint"] = "https://partner.example.com/api/orders"
    })
]);
```

Open `src/Contracts/RoutingSlip.cs` and verify the record structure. How does each step carry its own parameters? Why is this important for **atomicity** — each step is self-contained with all the data it needs.

### Step 2: Trace a Partial-Completion Recovery

A message has completed Validate and Transform but the worker crashes during Deliver. The message is redelivered with the slip attached:

1. What does `RemainingSlip` contain? (hint: only Deliver remains)
2. How does the platform know which steps already completed?
3. Are Validate and Transform re-executed? Why or why not?

Draw the recovery timeline and explain how the Routing Slip pattern achieves **idempotent resume** — crashed messages resume from exactly where they left off.

### Step 3: Compare Routing Slip vs. Temporal Workflow

| Aspect | Routing Slip | Temporal Workflow (Process Manager) |
|--------|-------------|-------------------------------------|
| State persistence | In the message itself | In Temporal's event history |
| Dynamic step addition | ? | ? |
| Compensation support | ? | ? |
| Scalability | ? | ? |
| Best for | ? | ? |

When would you choose a Routing Slip over a full Temporal workflow? Consider: simple linear pipelines vs. complex branching logic.

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial13/Exam.cs`](../tests/TutorialLabs/Tutorial13/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 12 — Recipient List](12-recipient-list.md)** | **Next: [Tutorial 14 — Process Manager →](14-process-manager.md)**
