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
            (PipelineActivities act) => act.PersistMessageAsync(input),
            PipelineActivityOptions);

        // Step 2: Log Received lifecycle event
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.LogStageAsync(input.MessageId, input.MessageType, "Received"),
            PipelineActivityOptions);

        // Step 3: Validate the message
        var validation = await Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            ValidationActivityOptions);

        if (!validation.IsValid)
        {
            // Publish Nack and update status to Failed
            await Workflow.ExecuteActivityAsync(
                (PipelineActivities act) =>
                    act.UpdateDeliveryStatusAsync(
                        input.MessageId, input.CorrelationId,
                        input.Timestamp, "Failed"),
                PipelineActivityOptions);

            if (input.NotificationsEnabled)
            {
                await Workflow.ExecuteActivityAsync(
                    (PipelineActivities act) =>
                        act.PublishNackAsync(
                            input.MessageId, input.CorrelationId,
                            validation.Reason ?? "Validation failed", input.NackSubject),
                    PipelineActivityOptions);
            }

            return new IntegrationPipelineResult(input.MessageId, false, validation.Reason);
        }

        // Step 4: Update status to Delivered
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.UpdateDeliveryStatusAsync(
                    input.MessageId, input.CorrelationId,
                    input.Timestamp, "Delivered"),
            PipelineActivityOptions);

        // Step 5: Publish Ack
        if (input.NotificationsEnabled)
        {
            await Workflow.ExecuteActivityAsync(
                (PipelineActivities act) =>
                    act.PublishAckAsync(input.MessageId, input.CorrelationId, input.AckSubject),
                PipelineActivityOptions);
        }

        return new IntegrationPipelineResult(input.MessageId, true);
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
// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs (simplified)
[Workflow]
public class AtomicPipelineWorkflow
{
    [WorkflowRun]
    public async Task<AtomicPipelineResult> RunAsync(
        IntegrationPipelineInput input)
    {
        var completedSteps = new List<string>();

        // Step 1: Persist message as Pending
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) => act.PersistMessageAsync(input),
            PipelineActivityOptions);
        completedSteps.Add("PersistMessage");

        // Step 2: Validate message
        var validation = await Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            ValidationActivityOptions);

        if (!validation.IsValid)
        {
            // Compensate all previously completed steps in reverse order
            foreach (var step in Enumerable.Reverse(completedSteps))
            {
                await Workflow.ExecuteActivityAsync(
                    (SagaCompensationActivities act) =>
                        act.CompensateStepAsync(input.CorrelationId, step),
                    CompensationActivityOptions);
            }

            // Save fault and publish Nack
            return new AtomicPipelineResult(
                input.MessageId, false, validation.Reason);
        }

        // Step 3: Update status to Delivered and Publish Ack
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.UpdateDeliveryStatusAsync(
                    input.MessageId, input.CorrelationId,
                    input.Timestamp, "Delivered"),
            PipelineActivityOptions);

        return new AtomicPipelineResult(input.MessageId, true);
    }
}
```

---

## Workflow Activities

Activities are the building blocks that workflows orchestrate. Each activity is a stateless function that performs one task:

```csharp
// src/Workflow.Temporal/Activities/IntegrationActivities.cs (simplified)
// Handles validation and processing-stage logging

[Activity]
public class IntegrationActivities
{
    [Activity]
    public async Task<MessageValidationResult> ValidateMessageAsync(
        string messageType, string payloadJson)
    {
        // Validate the message against schema and business rules
        return await _validation.ValidateAsync(messageType, payloadJson);
    }

    [Activity]
    public async Task LogProcessingStageAsync(
        Guid messageId, string messageType, string stage)
    {
        // Record a lifecycle stage for observability
    }
}

// src/Workflow.Temporal/Activities/PipelineActivities.cs (simplified)
// Handles persistence, delivery status, acknowledgments, and faults

[Activity]
public class PipelineActivities
{
    [Activity]
    public async Task PersistMessageAsync(IntegrationPipelineInput input)
    {
        // Save message to Cassandra with status: Pending
        await _persistence.SaveMessageAsync(input);
    }

    [Activity]
    public async Task PublishAckAsync(
        Guid messageId, Guid correlationId, string topic)
    {
        // Publish acknowledgment to Ack topic
        await _notification.PublishAckAsync(messageId, correlationId, topic);
    }

    [Activity]
    public async Task PublishNackAsync(
        Guid messageId, Guid correlationId, string reason, string topic)
    {
        // Publish negative acknowledgment to Nack topic
        await _notification.PublishNackAsync(messageId, correlationId, reason, topic);
    }
}
```

> **Note:** Activities are split across two classes: `IntegrationActivities` (validation and logging) and `PipelineActivities` (persistence and notifications). A third class, `SagaCompensationActivities`, handles rollback (see Tutorial 47).

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

## Lab

**Objective:** Trace how Temporal workflows enforce **atomic processing** with saga compensation, and design a failure recovery strategy for a multi-step integration pipeline.

### Step 1: Trace a Failure Recovery Path

A workflow has 4 steps: Validate → Transform → Route → Deliver. Step 3 (Route) fails after Step 2 has already committed its result. Open `src/Workflow.Temporal/` and trace the code path:

1. What does Temporal do when Step 3 throws an exception? (hint: retry policy)
2. If all retries are exhausted, how does the `AtomicPipelineWorkflow` trigger saga compensation?
3. What does `SagaCompensationActivities.CompensateStepAsync` do for Steps 1 and 2?

Draw the timeline showing: original steps executed, failure point, compensation steps in reverse order.

### Step 2: Design Compensation for a Business Scenario

Design saga compensation for an order fulfilment workflow:

| Step | Action | Compensation |
|------|--------|-------------|
| 1 | Create customer record in CRM | ? |
| 2 | Reserve inventory in warehouse | ? |
| 3 | Charge payment via gateway | ? |
| 4 | Send confirmation email | ? |

For each compensation, identify: Is it idempotent? What happens if the compensation itself fails? How does the `CorrelationId` link the original action to its compensation?

### Step 3: Evaluate Scalability of Workflow Workers

Temporal workers poll task queues for workflow and activity tasks. Consider a scenario with 100 concurrent integrations:

- How many workflow workers should you run? What happens when you add more?
- What is the relationship between worker count and **throughput**?
- Why does Temporal's durable execution model prevent duplicate processing even when workers scale horizontally?

## Exam

1. What happens when a Temporal workflow worker crashes in the middle of executing an activity?
   - A) The message is lost permanently
   - B) Another worker picks up the activity from the last checkpoint — Temporal's event history ensures exactly-once execution semantics with durable state
   - C) The entire workflow restarts from Step 1
   - D) The broker automatically retries the message

2. In the Saga Compensation pattern, why must compensation steps execute in **reverse order**?
   - A) Reverse order is faster for the runtime to schedule
   - B) Later steps may depend on earlier steps' state — compensating in reverse ensures each rollback sees a consistent state from the steps that preceded it
   - C) The EIP book mandates reverse order for all patterns
   - D) Temporal only supports reverse-order execution

3. How does Temporal's durable execution model ensure **atomicity** across a multi-step integration pipeline?
   - A) It wraps all steps in a database transaction
   - B) It persists each step's completion in an event history — if a worker fails, another worker replays the history and resumes from the exact point of failure, never re-executing completed steps
   - C) It locks the message broker partition until all steps complete
   - D) It copies messages to a backup queue before processing

---

**Previous: [← Tutorial 06 — Messaging Channels](06-messaging-channels.md)** | **Next: [Tutorial 08 — Activities and the Pipeline →](08-activities-pipeline.md)**
