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
public class AtomicPipelineWorkflow
{
    private readonly List<CompletedStep> _completedSteps = new();

    public async Task<PipelineResult> RunAsync(IntegrationPipelineInput input)
    {
        try
        {
            var validated = await ExecuteTracked("Validate", input);
            var transformed = await ExecuteTracked("Transform", validated);
            var routed = await ExecuteTracked("Route", transformed);
            var delivered = await ExecuteTracked("Deliver", routed);

            if (input.NotificationsEnabled)
                await PublishAck(delivered);

            return PipelineResult.Success(delivered);
        }
        catch (ActivityFailedException ex)
        {
            if (input.NotificationsEnabled)
                await PublishNack(ex);

            await CompensateAsync();
            return PipelineResult.Failed(ex);
        }
    }
}
```

## Step Tracking

Each activity execution is tracked before proceeding:

```csharp
private async Task<StepResult> ExecuteTracked(
    string stepName, object input)
{
    var result = await ExecuteActivity(stepName, input);
    _completedSteps.Add(new CompletedStep
    {
        Name = stepName,
        CompletedAt = DateTime.UtcNow,
        CompensationData = result.CompensationData
    });
    return result;
}
```

```
Completed Steps Stack (LIFO for compensation):
┌─────────────────┐
│ 3. Route        │ ← compensate first
├─────────────────┤
│ 2. Transform    │ ← compensate second
├─────────────────┤
│ 1. Validate     │ ← compensate last
└─────────────────┘
```

## Reverse-Order Compensation

`SagaCompensationActivities` undo steps in reverse order:

```csharp
private async Task CompensateAsync()
{
    // Reverse order: last completed step compensated first
    for (int i = _completedSteps.Count - 1; i >= 0; i--)
    {
        var step = _completedSteps[i];
        await ExecuteActivity<SagaCompensationActivities>(
            $"Compensate{step.Name}",
            step.CompensationData);
    }
}
```

```
  Forward execution:      Compensation (on failure):

  Validate ──────▶        ◀────── UndoValidate
  Transform ─────▶        ◀────── UndoTransform
  Route ─────────▶        ◀────── UndoRoute
  Deliver ───✗ FAIL       (not completed, skip)
```

## Partial Compensation

If compensation itself fails, the workflow records the partial state:

```csharp
catch (Exception compensationEx)
{
    // Log and continue compensating remaining steps
    _logger.LogError(compensationEx,
        "Compensation failed for step {Step}", step.Name);
    failedCompensations.Add(step.Name);
    // Do NOT throw — attempt all remaining compensations
}
```

Uncompensated steps are flagged for manual review via Admin.Api.

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
public class SagaCompensationActivities
{
    public async Task CompensateChargePayment(PaymentData data)
    {
        await _paymentService.RefundAsync(data.TransactionId, data.Amount);
    }

    public async Task CompensateReserveInventory(InventoryData data)
    {
        await _inventoryService.ReleaseAsync(data.Sku, data.Quantity);
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

## Exercises

1. What happens if `CompensateChargePayment` fails with a network timeout?
   Design a retry policy that balances urgency (customer refund) with safety
   (no double refund).

2. Add a fourth step "Send Confirmation Email" to the saga. What does its
   compensation look like? Is email compensation even possible?

3. Compare the `IntegrationPipelineWorkflow` and `AtomicPipelineWorkflow`.
   When would you choose one over the other? Consider throughput vs. consistency.

**Previous: [← Tutorial 46](46-complete-integration.md)** | **Next: [Tutorial 48 →](48-notification-use-cases.md)**
