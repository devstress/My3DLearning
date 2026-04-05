# Tutorial 14 — Process Manager

## What You'll Learn

- The EIP Process Manager pattern for long-running stateful orchestration
- How Temporal workflows implement the Process Manager role
- `IntegrationPipelineWorkflow` and `AtomicPipelineWorkflow` as concrete managers
- Saga compensation via `SagaCompensationActivities`
- The difference between a process manager and a routing slip

---

## EIP Pattern: Process Manager

> *"Use a central processing unit, a Process Manager, to maintain the state of the sequence and determine the next processing step based on intermediate results."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌────────────────────────────────────────────┐
  │            Process Manager                  │
  │   (maintains state, decides next step)      │
  │                                            │
  │   Step 1 ──▶ Step 2 ──▶ Step 3             │
  │     ✅         ✅         ❌               │
  │                                            │
  │   Compensation: Step 2⁻¹ ──▶ Step 1⁻¹      │
  └────────────────────────────────────────────┘
```

Unlike a routing slip (decentralised, message-carried state), the Process Manager is a **centralised stateful orchestrator** that decides the next step based on intermediate results and can branch, loop, or compensate.

---

## Platform Implementation

### IntegrationPipelineWorkflow

```csharp
// src/Workflow.Temporal/Workflows/IntegrationPipelineWorkflow.cs (simplified)
[Workflow]
public class IntegrationPipelineWorkflow
{
    [WorkflowRun]
    public async Task<IntegrationPipelineResult> RunAsync(
        IntegrationPipelineInput input)
    {
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities a) => a.PersistMessageAsync(input), opts);

        var validation = await Workflow.ExecuteActivityAsync(
            (IntegrationActivities a) =>
                a.ValidateMessageAsync(input.MessageType, input.PayloadJson), opts);

        if (!validation.IsValid)
            return await HandleFailureAsync(input, validation.Reason);

        return await HandleSuccessAsync(input);
    }
}
```

The workflow **decides** the next step based on intermediate results (`validation.IsValid`). This is the core Process Manager behaviour — it is not a fixed pipeline.

### AtomicPipelineWorkflow (Saga variant)

```csharp
// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs (simplified)
[Workflow]
public class AtomicPipelineWorkflow
{
    [WorkflowRun]
    public async Task<AtomicPipelineResult> RunAsync(
        IntegrationPipelineInput input)
    {
        var completedSteps = new List<string>();
        // Execute steps, tracking each ...
        // On failure → compensate in reverse order
    }

    private async Task<AtomicPipelineResult> HandleNackWithRollbackAsync(
        IntegrationPipelineInput input,
        List<string> completedSteps,
        string failureReason)
    {
        foreach (var step in Enumerable.Reverse(completedSteps))
        {
            await Workflow.ExecuteActivityAsync(
                (SagaCompensationActivities a) =>
                    a.CompensateStepAsync(input.CorrelationId, step), opts);
        }
        // Publish Nack ...
    }
}
```

### SagaCompensationActivities

```csharp
// src/Workflow.Temporal/Activities/SagaCompensationActivities.cs
public sealed class SagaCompensationActivities
{
    [Activity]
    public async Task<bool> CompensateStepAsync(Guid correlationId, string stepName)
    {
        await _logging.LogAsync(correlationId, stepName, $"CompensationStarted:{stepName}");
        var success = await _compensationService.CompensateAsync(correlationId, stepName);
        var stage = success ? $"CompensationSucceeded:{stepName}" : $"CompensationFailed:{stepName}";
        await _logging.LogAsync(correlationId, stepName, stage);
        return success;
    }
}
```

---

## Scalability Dimension

Temporal workers scale horizontally — multiple workers poll the same task queue and Temporal distributes workflow executions across them. Each workflow execution is single-threaded (deterministic replay) but thousands of concurrent workflow instances can run in parallel across the worker fleet. The process manager state lives in Temporal's persistence layer, not in the worker memory.

---

## Atomicity Dimension

The `AtomicPipelineWorkflow` implements full **saga compensation**. Completed steps are tracked in a list. On failure, compensation activities run in reverse order to undo side effects. Even if a worker crashes mid-compensation, Temporal replays the workflow from the last durable checkpoint and continues compensating. The workflow always terminates with either an Ack (success) or a Nack (failure with compensation details), guaranteeing closed-loop notification.

---

## Lab

**Objective:** Trace the Process Manager's orchestration of multi-step workflows with saga compensation, and analyze how centralized coordination enables **atomic** all-or-nothing processing.

### Step 1: Trace a Compensation Sequence

A workflow has steps: Persist → Validate → Transform → Deliver. Transform succeeds but Deliver fails after all retries. Open `src/Workflow.Temporal/AtomicPipelineWorkflow.cs` and trace:

1. Which steps need compensation? (only steps that committed work)
2. In what order do compensation steps execute? (hint: reverse)
3. What does `SagaCompensationActivities.CompensateStepAsync` do for each step?

List the compensation sequence:

| Order | Compensating | Original Step |
|-------|-------------|---------------|
| 1 | Undo Transform | Transform |
| 2 | ? | ? |
| 3 | ? | ? |

### Step 2: Handle Compensation Failures

The compensation for "Persist" itself fails. Open `src/Workflow.Temporal/Activities/SagaCompensationActivities.cs` and answer:

- What does the `CompensateStepAsync` method return when compensation fails?
- Does Temporal retry the compensation? With what policy?
- What is logged? How does the operations team know that manual intervention is required?

Design an alerting strategy for compensation failures — this is the **atomicity boundary** of the system.

### Step 3: Compare Process Manager vs. Routing Slip

| Aspect | Process Manager | Routing Slip |
|--------|----------------|-------------|
| Coordination | Centralized (Temporal) | Decentralized (in-message) |
| Compensation | Full saga support | Limited / manual |
| Visibility | Full execution history | ? |
| Scalability bottleneck | Temporal server | ? |
| Best for | Complex branching, compensation | ? |

When would a Process Manager's centralized coordination be worth the **scalability** trade-off vs. a Routing Slip?

## Exam

1. In a Process Manager with saga compensation, why must compensation steps execute in **reverse order**?
   - A) It's a convention with no technical reason
   - B) Later steps may depend on earlier steps' committed state — reverse-order compensation ensures each rollback sees the state from the steps that preceded it, maintaining consistency
   - C) Temporal only supports reverse execution
   - D) Reverse order is faster for the scheduler

2. A compensation step itself fails. What is the correct platform behavior for maintaining **atomicity**?
   - A) Silently ignore the failure and mark the saga as complete
   - B) Log the failure, mark the saga as partially compensated, and alert the operations team — some atomicity violations require human intervention when automatic compensation is impossible
   - C) Restart the entire original workflow from Step 1
   - D) Route the compensation failure to the Dead Letter Queue and retry indefinitely

3. What is the key advantage of the Process Manager pattern over the Routing Slip for **enterprise-grade atomicity**?
   - A) Process Managers are faster for simple linear pipelines
   - B) The Process Manager maintains a durable execution history with full saga compensation — if any step fails, all committed work can be rolled back to restore consistency
   - C) Process Managers don't require a message broker
   - D) Routing Slips cannot carry parameters

---

**Previous: [← Tutorial 13 — Routing Slip](13-routing-slip.md)** | **Next: [Tutorial 15 — Message Translator →](15-message-translator.md)**
