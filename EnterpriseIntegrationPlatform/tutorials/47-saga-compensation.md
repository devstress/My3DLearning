# Tutorial 47 — Saga Compensation

## What You'll Learn

- Deep dive on `AtomicPipelineWorkflow` and saga compensation
- Step tracking and completion state management
- Nack-triggered reverse-order compensation via `SagaCompensationActivities`
- Handling partial compensation scenarios
- The difference between `IntegrationPipelineWorkflow` and `AtomicPipelineWorkflow`
- Real-world saga example: inventory → payment → shipping → failure → undo

## Two Pipeline Workflows Compared

```
┌──────────────────────────────────────────────────────────────────────┐
│  IntegrationPipelineWorkflow (No Compensation)                       │
│                                                                      │
│  Step 1 ──▶ Step 2 ──▶ Step 3 ──▶ Step 4                           │
│  If Step 3 fails: retry (Temporal policy) or fail the workflow       │
│  No undo of Step 1 or Step 2.                                        │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│  AtomicPipelineWorkflow (Full Saga Compensation)                     │
│                                                                      │
│  Step 1 ──▶ Step 2 ──▶ Step 3 ──▶ Step 4                           │
│  If Step 3 fails: Nack + compensate Step 2 ──▶ compensate Step 1    │
│  Reverse-order undo of all completed steps.                          │
└──────────────────────────────────────────────────────────────────────┘
```

## AtomicPipelineWorkflow Structure

```csharp
// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs (simplified)
[Workflow]
public class AtomicPipelineWorkflow
{
    [WorkflowRun]
    public async Task<AtomicPipelineResult> RunAsync(IntegrationPipelineInput input)
    {
        var completedSteps = new List<string>();

        // Step 1: Persist message as Pending
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) => act.PersistMessageAsync(input),
            PipelineActivityOptions);
        completedSteps.Add("PersistMessage");

        // Step 2: Log Received lifecycle event
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.LogStageAsync(input.MessageId, input.MessageType, "Received"),
            PipelineActivityOptions);
        completedSteps.Add("LogReceived");

        // Step 3: Validate message
        var validation = await Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            ValidationActivityOptions);

        if (!validation.IsValid)
        {
            // Nack path: compensate all completed steps, then Nack
            return await HandleNackWithRollbackAsync(
                input, completedSteps, validation.Reason ?? "Validation failed");
        }

        // Step 4: Update status to Delivered
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.UpdateDeliveryStatusAsync(
                    input.MessageId, input.CorrelationId,
                    input.Timestamp, "Delivered"),
            PipelineActivityOptions);

        // Step 5: Publish Ack (only if notifications enabled)
        if (input.NotificationsEnabled)
        {
            await Workflow.ExecuteActivityAsync(
                (PipelineActivities act) =>
                    act.PublishAckAsync(input.MessageId, input.CorrelationId, input.AckSubject),
                PipelineActivityOptions);
        }

        return new AtomicPipelineResult(input.MessageId, true);
    }
}
```

## Step Tracking

Each successfully completed activity is recorded in a `List<string>`. If a later step fails, the workflow knows exactly which steps need compensation:

```csharp
var completedSteps = new List<string>();

// After each activity completes successfully:
await Workflow.ExecuteActivityAsync(
    (PipelineActivities act) => act.PersistMessageAsync(input),
    PipelineActivityOptions);
completedSteps.Add("PersistMessage");   // Track it

await Workflow.ExecuteActivityAsync(
    (PipelineActivities act) =>
        act.LogStageAsync(input.MessageId, input.MessageType, "Received"),
    PipelineActivityOptions);
completedSteps.Add("LogReceived");      // Track it
```

```
Completed Steps List (reversed for compensation):
┌─────────────────┐
│ 2. LogReceived  │ ← compensate first
├─────────────────┤
│ 1. PersistMessage│ ← compensate second
└─────────────────┘
```

## Reverse-Order Compensation

`HandleNackWithRollbackAsync` compensates steps in reverse order via `SagaCompensationActivities`:

```csharp
// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs (simplified)
private async Task<AtomicPipelineResult> HandleNackWithRollbackAsync(
    IntegrationPipelineInput input,
    List<string> completedSteps,
    string failureReason)
{
    var compensatedSteps = new List<string>();

    // Compensate in reverse order (last committed step first)
    foreach (var step in Enumerable.Reverse(completedSteps))
    {
        try
        {
            var success = await Workflow.ExecuteActivityAsync(
                (SagaCompensationActivities act) =>
                    act.CompensateStepAsync(input.CorrelationId, step),
                CompensationActivityOptions);

            if (success)
                compensatedSteps.Add(step);
        }
        catch (Exception)
        {
            // Log but continue — partial compensation is better than none
        }
    }

    // Save fault, update status to Failed, publish Nack...
    return new AtomicPipelineResult(
        input.MessageId, false, failureReason,
        compensatedSteps.AsReadOnly());
}
```

```
  Forward execution:           Compensation (on failure):

  PersistMessage ──────▶       ◀────── CompensateStepAsync("PersistMessage")
  LogReceived ─────────▶       ◀────── CompensateStepAsync("LogReceived")
  Validate ────────✗ FAIL      (not completed, skip)
```

## Partial Compensation

If compensation itself fails, the workflow catches the exception and continues with remaining steps:

```csharp
catch (Exception)
{
    // Log but continue — partial compensation is better than none.
    // The step is NOT added to compensatedSteps, so the result
    // tracks exactly which steps were successfully rolled back.
}
```

Uncompensated steps are visible in the `AtomicPipelineResult.CompensatedSteps` list — operators can compare it with the original `completedSteps` to identify gaps requiring manual review via Admin.Api.

## Real-World Example: E-Commerce Order

```
┌──────────────────────────────────────────────────────────┐
│                 Order Processing Saga                     │
│                                                          │
│  Step 1: Reserve Inventory   ──▶ 10 Widgets reserved    │
│  Step 2: Charge Payment      ──▶ $99.99 charged         │
│  Step 3: Ship Order          ──✗ FAILURE (out of stock   │
│                                   at warehouse)          │
│                                                          │
│  Compensation (reverse order):                           │
│  Undo Step 2: Refund Payment ──▶ $99.99 refunded        │
│  Undo Step 1: Release Inv.   ──▶ 10 Widgets released    │
└──────────────────────────────────────────────────────────┘
```

```csharp
// src/Workflow.Temporal/Activities/SagaCompensationActivities.cs
public sealed class SagaCompensationActivities
{
    private readonly ICompensationActivityService _compensationService;
    private readonly IMessageLoggingService _logging;

    [Activity]
    public async Task<bool> CompensateStepAsync(Guid correlationId, string stepName)
    {
        await _logging.LogAsync(correlationId, stepName, $"CompensationStarted:{stepName}");
        var success = await _compensationService.CompensateAsync(correlationId, stepName);
        var stage = success
            ? $"CompensationSucceeded:{stepName}"
            : $"CompensationFailed:{stepName}";
        await _logging.LogAsync(correlationId, stepName, stage);
        return success;
    }
}
```

## Nack-Triggered Compensation

When `NotificationsEnabled = true`, the failure publishes a Nack before
compensation begins:

```
  Activity Failure
       │
       ├──▶ Publish Nack ──▶ NATS notification subject
       │        │
       │        ▼
       │    <Nack>not ok because of {ErrorMessage}</Nack>
       │
       └──▶ Begin compensation (reverse-order)
```

## Scalability Dimension

Saga compensation is orchestrated by Temporal, which distributes compensation
activities across the worker pool. Long-running compensations (e.g., refund
processing) do not block other workflows — Temporal's task queue ensures
parallel execution across workers.

## Atomicity Dimension

The saga pattern provides **semantic atomicity** — while not a database
transaction, it guarantees that either all steps complete successfully or all
completed steps are compensated. This is the strongest consistency guarantee
available in a distributed system without two-phase commit.

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial47/Lab.cs`](../tests/TutorialLabs/Tutorial47/Lab.cs)

**Objective:** Design saga compensation for multi-step workflows, analyze compensation failure strategies, and compare workflow types for **throughput vs. consistency** trade-offs.

### Step 1: Design Compensation for Non-Reversible Actions

Add a fourth step "SendConfirmation" (email) to the saga:

| Step | Action | Compensation | Reversible? |
|------|--------|-------------|-------------|
| 1. ValidateOrder | Schema validation | No-op (read-only) | N/A |
| 2. ChargePayment | Debit customer account | Refund credit | Yes |
| 3. ReserveInventory | Decrement stock | Increment stock | Yes |
| 4. SendConfirmation | Email customer | ??? | **No** |

Email is non-reversible. Design a compensating action:
- Send a "cancellation notice" email? (creates customer confusion)
- Log the non-reversible action for manual review? (operationally safer)
- Accept that some actions cannot be compensated? (pragmatic)

How does this challenge the **theoretical atomicity** of saga compensation?

### Step 2: Handle Compensation Failures

`CompensateStepAsync` for Step 2 (Refund) fails with a network timeout. Design a retry policy:

| Concern | Policy |
|---------|--------|
| Urgency | Customer expects refund quickly |
| Safety | Must not issue double refund |
| Idempotency | Refund API must be idempotent (check by `CorrelationId`) |
| Retry limit | 5 attempts with exponential backoff |
| Escalation | After 5 failures → alert operations team for manual refund |

Open `src/Workflow.Temporal/Activities/SagaCompensationActivities.cs` and check: How does the platform handle compensation activity failures?

### Step 3: Compare Workflow Types

| Aspect | IntegrationPipelineWorkflow | AtomicPipelineWorkflow |
|--------|---------------------------|----------------------|
| Compensation | None (fire-and-forget) | Full saga compensation |
| Throughput | Higher (no compensation overhead) | Lower (tracks compensation state) |
| Consistency guarantee | Best-effort delivery | All-or-nothing |
| Best for | Non-critical notifications | Financial transactions, order processing |

When would you choose `IntegrationPipelineWorkflow` over `AtomicPipelineWorkflow`?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial47/Exam.cs`](../tests/TutorialLabs/Tutorial47/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 46](46-complete-integration.md)** | **Next: [Tutorial 48 →](48-notification-use-cases.md)**
