# Tutorial 07 — Temporal Workflows

## What You'll Learn

- What Temporal.io is and why the platform uses it
- How workflows orchestrate message processing
- Durable execution and fault tolerance
- The IntegrationPipelineWorkflow and AtomicPipelineWorkflow
- Saga compensation for distributed transactions

---

## Why Temporal?

Traditional message processing is fragile:

```
Receive → Validate → Transform → Route → Deliver
                          ↑
                     Server crashes here.
                     Message is lost.
```

**Temporal.io** solves this with **durable execution**. Every workflow step is persisted. If the server crashes, Temporal automatically resumes from the last completed step:

```
Receive → Validate → Transform → [CRASH] → [RESTART] → Route → Deliver
                                              ↑
                                    Resumes from here
```

### Key Temporal Concepts

| Concept | Description |
|---------|-------------|
| **Workflow** | A durable, long-running function that orchestrates activities |
| **Activity** | A single unit of work (validate, transform, route, deliver) |
| **Worker** | A process that polls for and executes workflows/activities |
| **Task Queue** | Named queue where workflows and activities are dispatched |
| **Signal** | External input to a running workflow (e.g., manual approval) |
| **Query** | Read the current state of a running workflow |

---

## The Integration Pipeline Workflow

The core workflow orchestrates every message through the processing pipeline:

```csharp
// src/Workflow.Temporal/Workflows/IntegrationPipelineWorkflow.cs (simplified)

[Workflow]
public class IntegrationPipelineWorkflow
{
    [WorkflowRun]
    public async Task<IntegrationPipelineResult> RunAsync(
        IntegrationPipelineInput input)
    {
        // Step 1: Persist the message (status: Pending)
        await Workflow.ExecuteActivityAsync(
            (IntegrationActivities a) => a.PersistMessageAsync(input),
            ActivityOptions);

        // Step 2: Validate the message
        var validationResult = await Workflow.ExecuteActivityAsync(
            (IntegrationActivities a) => a.ValidateMessageAsync(input),
            ActivityOptions);

        if (!validationResult.IsValid)
        {
            // Publish Nack and route to DLQ
            await Workflow.ExecuteActivityAsync(
                (IntegrationActivities a) => a.PublishNackAsync(input, validationResult),
                ActivityOptions);
            return IntegrationPipelineResult.Failed(validationResult.Errors);
        }

        // Step 3: Update status to InFlight
        await Workflow.ExecuteActivityAsync(
            (IntegrationActivities a) => a.UpdateStatusAsync(input, DeliveryStatus.InFlight),
            ActivityOptions);

        // Step 4: Publish Ack
        await Workflow.ExecuteActivityAsync(
            (IntegrationActivities a) => a.PublishAckAsync(input),
            ActivityOptions);

        return IntegrationPipelineResult.Succeeded();
    }
}
```

### What Makes This Durable

Each `ExecuteActivityAsync` call is recorded by Temporal. If the worker crashes after Step 2 but before Step 3, Temporal will:

1. Detect the worker is gone
2. Assign the workflow to another worker
3. Replay Steps 1 and 2 (already completed — just fast-forward)
4. Execute Step 3 from where it left off

**Zero message loss, guaranteed.**

---

## The Atomic Pipeline Workflow

The `AtomicPipelineWorkflow` adds **saga compensation** — if a step fails after earlier steps have committed side effects, compensation activities undo those effects:

```
Step 1: Reserve inventory    ✅ (committed)
Step 2: Charge payment       ✅ (committed)
Step 3: Send to warehouse    ❌ (FAILED)

Compensation (reverse order):
Step 2 comp: Refund payment  ✅
Step 1 comp: Release inventory ✅
Publish Nack with compensation details
```

```csharp
// Simplified saga compensation flow
[Workflow]
public class AtomicPipelineWorkflow
{
    [WorkflowRun]
    public async Task<IntegrationPipelineResult> RunAsync(
        IntegrationPipelineInput input)
    {
        var completedSteps = new Stack<string>();

        try
        {
            // Execute steps, tracking each completed one
            await ExecuteStep("persist", input);
            completedSteps.Push("persist");

            await ExecuteStep("validate", input);
            completedSteps.Push("validate");

            await ExecuteStep("transform", input);
            completedSteps.Push("transform");

            await ExecuteStep("deliver", input);
            completedSteps.Push("deliver");

            await PublishAck(input);
            return IntegrationPipelineResult.Succeeded();
        }
        catch (Exception ex)
        {
            // Compensate in reverse order
            while (completedSteps.Count > 0)
            {
                var step = completedSteps.Pop();
                await Workflow.ExecuteActivityAsync(
                    (SagaCompensationActivities a) =>
                        a.CompensateAsync(step, input),
                    CompensationOptions);
            }

            await PublishNack(input, ex);
            return IntegrationPipelineResult.Failed(ex.Message);
        }
    }
}
```

---

## Workflow Activities

Activities are the building blocks that workflows orchestrate. Each activity is a stateless function that performs one task:

```csharp
// src/Workflow.Temporal/Activities/IntegrationActivities.cs (simplified)

[Activity]
public class IntegrationActivities
{
    [ActivityMethod]
    public async Task PersistMessageAsync(IntegrationPipelineInput input)
    {
        // Save message to Cassandra with status: Pending
        await _persistence.SaveMessageAsync(input.Envelope, DeliveryStatus.Pending);
    }

    [ActivityMethod]
    public async Task<ValidationResult> ValidateMessageAsync(
        IntegrationPipelineInput input)
    {
        // Validate the message against schema and business rules
        return await _validation.ValidateAsync(input.Envelope);
    }

    [ActivityMethod]
    public async Task PublishAckAsync(IntegrationPipelineInput input)
    {
        // Publish acknowledgment to Ack topic
        await _notification.PublishAckAsync(input.Envelope);
    }

    [ActivityMethod]
    public async Task PublishNackAsync(
        IntegrationPipelineInput input,
        ValidationResult result)
    {
        // Publish negative acknowledgment to Nack topic
        await _notification.PublishNackAsync(input.Envelope, result.Errors);
    }
}
```

### Activity Design Principles

1. **Stateless** — Activities don't hold state between executions
2. **Idempotent** — Running twice produces the same result (critical for retries)
3. **Single responsibility** — Each activity does one thing
4. **Testable** — Activities can be unit tested in isolation with mocked dependencies

---

## Ack/Nack Notification Loopback

Every workflow ends by publishing either an **Ack** (success) or **Nack** (failure):

```
Workflow completes successfully:
  → Publish Ack to "notifications.ack.{messageType}"
  → External systems subscribe to confirm delivery

Workflow fails (after retries):
  → Compensate prior steps (if saga)
  → Publish Nack to "notifications.nack.{messageType}"
  → External systems subscribe to handle failure
  → Message routed to DLQ for inspection
```

This ensures **closed-loop integration** — the sender always knows the outcome.

---

## How Messages Trigger Workflows

The `Demo.Pipeline` project shows how messages flow from the broker to Temporal:

```
1. IntegrationPipelineWorker (BackgroundService) subscribes to broker topic
2. Message arrives → Worker creates IntegrationPipelineInput
3. Worker calls TemporalWorkflowDispatcher to start a workflow
4. Temporal assigns the workflow to a worker on the task queue
5. Workflow executes activities in sequence
6. Activities call domain services (persistence, validation, routing, delivery)
7. Workflow publishes Ack or Nack and completes
```

---

## Testing Workflows

Workflow tests use Temporal's local dev server:

```csharp
[TestFixture]
public class IntegrationPipelineWorkflowTests
{
    [Test]
    public async Task RunAsync_ValidMessage_PublishesAck()
    {
        // Arrange: create a valid envelope and mock services
        var input = CreateValidInput();

        // Act: run the workflow
        var result = await RunWorkflow(input);

        // Assert: workflow succeeded, Ack was published
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.AckPublished, Is.True);
    }

    [Test]
    public async Task RunAsync_InvalidMessage_PublishesNack()
    {
        // Arrange: create an invalid envelope
        var input = CreateInvalidInput();

        // Act: run the workflow
        var result = await RunWorkflow(input);

        // Assert: workflow failed, Nack was published
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.NackPublished, Is.True);
    }
}
```

---

## Exercises

1. **Failure scenario**: A workflow has 4 steps. Step 3 fails. What does Temporal do? What happens if the worker crashes during Step 3's retry?

2. **Saga compensation**: Design compensation for: (1) create customer record, (2) provision email account, (3) send welcome email. What compensates each step?

3. **Ack/Nack design**: An order processing workflow has 5 steps. Step 4 (warehouse check) says "out of stock." Should this be a Nack? What information should the Nack carry?

---

**Previous: [← Tutorial 06 — Messaging Channels](06-messaging-channels.md)** | **Next: [Tutorial 08 — Activities and the Pipeline →](08-activities-pipeline.md)**
