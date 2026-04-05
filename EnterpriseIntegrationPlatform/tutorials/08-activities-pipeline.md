# Tutorial 08 — Activities and the Pipeline

## What You'll Learn

- How activities are the building blocks of integration pipelines
- The activity service interfaces (persistence, validation, notification)
- How the Demo.Pipeline orchestrates end-to-end message flow
- The relationship between activities, workflows, and brokers

---

## What Are Activities?

In the EIP world, **Pipes and Filters** is a fundamental pattern — messages flow through a series of processing steps, each step doing one thing. In this platform, each "filter" is a Temporal **activity**.

```
                         Pipes and Filters
┌────────┐    ┌──────────┐    ┌───────────┐    ┌─────────┐    ┌─────────┐
│ Ingest │───▶│ Validate │───▶│ Transform │───▶│  Route  │───▶│ Deliver │
└────────┘    └──────────┘    └───────────┘    └─────────┘    └─────────┘
   Activity      Activity       Activity        Activity       Activity
```

Each activity:
- Receives an `IntegrationEnvelope<T>` (or input derived from it)
- Performs one operation
- Returns a result
- Is orchestrated by a Temporal workflow

---

## Activity Service Interfaces

Activities delegate to **service interfaces** defined in `src/Activities/`. This separation keeps activities thin and services independently testable.

### IPersistenceActivityService

```csharp
// src/Activities/IPersistenceActivityService.cs
public interface IPersistenceActivityService
{
    Task SaveMessageAsync(
        IntegrationPipelineInput input,
        CancellationToken cancellationToken = default);

    Task UpdateDeliveryStatusAsync(
        Guid messageId,
        Guid correlationId,
        DateTimeOffset recordedAt,
        string status,
        CancellationToken cancellationToken = default);

    Task SaveFaultAsync(
        Guid messageId,
        Guid correlationId,
        string messageType,
        string faultedBy,
        string reason,
        int retryCount,
        CancellationToken cancellationToken = default);
}
```

**Purpose:** Save messages to Cassandra with delivery status tracking. Every message is persisted on entry (status: `Pending`), updated as it progresses (`InFlight`, `Delivered`, `Failed`).

### IMessageValidationService

```csharp
// src/Activities/IMessageValidationService.cs
public interface IMessageValidationService
{
    Task<MessageValidationResult> ValidateAsync(
        string messageType,
        string payloadJson);
}

public record MessageValidationResult(bool IsValid, string? Reason = null)
{
    public static MessageValidationResult Success { get; } = new(true);
    public static MessageValidationResult Failure(string reason) => new(false, reason);
}
```

**Purpose:** Validate message content — schema validation, required fields, business rules. Returns a `MessageValidationResult` indicating success or the reason for failure.

### INotificationActivityService

```csharp
// src/Activities/INotificationActivityService.cs
public interface INotificationActivityService
{
    Task PublishAckAsync(
        Guid messageId,
        Guid correlationId,
        string topic,
        CancellationToken cancellationToken = default);

    Task PublishNackAsync(
        Guid messageId,
        Guid correlationId,
        string reason,
        string topic,
        CancellationToken cancellationToken = default);
}
```

**Purpose:** Publish Ack/Nack notifications. On success, publish Ack so downstream systems know the message was processed. On failure, publish Nack with a reason so they can react.

### ICompensationActivityService

```csharp
// src/Activities/ICompensationActivityService.cs
public interface ICompensationActivityService
{
    Task<bool> CompensateAsync(
        Guid correlationId,
        string stepName);
}
```

**Purpose:** Undo the effects of a completed step during saga compensation. Returns `true` on success, `false` on failure.

---

## The Demo Pipeline

The `src/Demo.Pipeline/` project shows a complete end-to-end integration pipeline:

```
┌─────────────────────────────────────────────────────────────┐
│                     Demo.Pipeline                            │
│                                                             │
│  ┌─────────────────────┐                                    │
│  │ IntegrationPipeline │   BackgroundService that           │
│  │ Worker              │   subscribes to broker topic       │
│  └──────────┬──────────┘                                    │
│             │                                               │
│             ▼                                               │
│  ┌─────────────────────┐                                    │
│  │ PipelineOrchestrator│   Coordinates message flow         │
│  └──────────┬──────────┘                                    │
│             │                                               │
│             ▼                                               │
│  ┌─────────────────────┐                                    │
│  │ TemporalWorkflow    │   Dispatches to Temporal           │
│  │ Dispatcher          │   workflow execution               │
│  └──────────┬──────────┘                                    │
│             │                                               │
│             ▼                                               │
│  ┌─────────────────────┐                                    │
│  │ Temporal Workflow    │   Orchestrates activities:         │
│  │                     │   Persist → Validate → Route →    │
│  │                     │   Deliver → Ack/Nack              │
│  └─────────────────────┘                                    │
└─────────────────────────────────────────────────────────────┘
```

### IntegrationPipelineWorker

The worker is a .NET `BackgroundService` that continuously listens for messages:

```csharp
// Simplified from src/Demo.Pipeline/IntegrationPipelineWorker.cs
public class IntegrationPipelineWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _consumer.SubscribeAsync<JsonElement>(
            topic: _options.InboundSubject,
            consumerGroup: _options.ConsumerGroup,
            handler: async envelope =>
            {
                // Dispatch to pipeline orchestrator
                await _orchestrator.ProcessAsync(envelope, ct);
            },
            cancellationToken: ct);
    }
}
```

### PipelineOrchestrator

The orchestrator wraps the message in pipeline input and dispatches to Temporal:

```csharp
// Simplified from src/Demo.Pipeline/PipelineOrchestrator.cs
public sealed class PipelineOrchestrator : IPipelineOrchestrator
{
    public async Task ProcessAsync(
        IntegrationEnvelope<JsonElement> envelope,
        CancellationToken cancellationToken = default)
    {
        var input = new IntegrationPipelineInput
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            // ... map from envelope to workflow input
        };

        await _dispatcher.DispatchAsync(input, cancellationToken);
    }
}
```

---

## End-to-End Message Flow

Here's the complete journey of a message through the platform:

```
1. EXTERNAL SYSTEM
   └─ Sends HTTP POST to Gateway.Api
   
2. GATEWAY.API (Messaging Gateway pattern)
   ├─ Validates the request
   ├─ Wraps payload in IntegrationEnvelope
   └─ Publishes to broker topic "eip.inbound.orders"

3. BROKER (NATS JetStream / Kafka / Pulsar)
   └─ Durably stores the message

4. INTEGRATION PIPELINE WORKER (Demo.Pipeline)
   ├─ Subscribes to "eip.inbound.orders"
   ├─ Picks up the message
   └─ Dispatches to Temporal

5. TEMPORAL WORKFLOW
   ├─ Activity 1: Persist message (Cassandra, status: Pending)
   ├─ Activity 2: Validate message (schema + business rules)
   ├─ Activity 3: Update status (InFlight)
   ├─ Activity 4: Transform payload (if needed)
   ├─ Activity 5: Route to destination
   ├─ Activity 6: Deliver via connector (HTTP/SFTP/Email/File)
   ├─ Activity 7: Update status (Delivered)
   └─ Activity 8: Publish Ack

6. ACK/NACK NOTIFICATION
   └─ Published to "notifications.ack.orders"
   
7. OBSERVABILITY
   └─ OpenTelemetry traces, logs, and metrics recorded at every step
```

---

## Pipeline Configuration

The pipeline is configured via `PipelineOptions`:

```csharp
public class PipelineOptions
{
    public string NatsUrl { get; set; }             // NATS server URL
    public string InboundSubject { get; set; }      // Where to listen
    public string AckSubject { get; set; }          // Ack notification subject
    public string NackSubject { get; set; }         // Nack notification subject
    public string ConsumerGroup { get; set; }       // Consumer group name
    public string TemporalServerAddress { get; set; } // Temporal gRPC address
    public string TemporalNamespace { get; set; }   // Temporal namespace
    public string TemporalTaskQueue { get; set; }   // Temporal task queue
    public TimeSpan WorkflowTimeout { get; set; }   // Workflow timeout
}
```

---

## Testing Activities

Activities are tested in isolation with mocked dependencies:

```csharp
[TestFixture]
public class PersistenceActivityTests
{
    private IPersistenceActivityService _persistence;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IPersistenceActivityService>();
    }

    [Test]
    public async Task SaveMessage_StoresWithPendingStatus()
    {
        var input = CreateTestPipelineInput();

        await _persistence.SaveMessageAsync(input);

        await _persistence.Received(1).SaveMessageAsync(
            input, Arg.Any<CancellationToken>());
    }
}
```

---

## Lab

**Objective:** Design an activity pipeline for a real integration scenario, analyze failure modes, and identify where the Pipes and Filters pattern enables **independent scaling** of each stage.

### Step 1: Design a Pipeline for XML Invoice Processing

You receive XML invoices via SFTP. Design the complete activity sequence using the platform's activity classes:

| Step | Activity | Class | Purpose |
|------|----------|-------|---------|
| 1 | Validate | `IntegrationActivities.ValidateMessageAsync` | Schema + payload checks |
| 2 | ? | ? | Sanitize input (XSS, SQL injection) |
| 3 | ? | ? | Transform XML → canonical JSON |
| 4 | ? | ? | Enrich with customer data from CRM |
| 5 | ? | ? | Route to correct downstream system |
| 6 | ? | ? | Deliver via HTTP connector |
| 7 | ? | ? | Persist to Cassandra |
| 8 | ? | ? | Send Ack/Nack notification |

Open `src/Activities/` and `src/Workflow.Temporal/Activities/` to find the actual activity classes.

### Step 2: Analyze Failure Modes and Atomicity

For your pipeline above, analyze what happens at each failure point:

- Step 3 fails with a **transient** error (network timeout) — what retry policy applies?
- Step 3 fails with a **permanent** error (invalid XML schema) — where does the message go?
- Step 6 fails after Step 7 already persisted — what compensation is needed?

Explain how the Ack/Nack pattern at Step 8 ensures the originating system knows the final outcome, preserving **end-to-end atomicity**.

### Step 3: Evaluate Per-Stage Scalability

The Pipes and Filters pattern allows each activity to scale independently. For your pipeline:

- Which step is likely the bottleneck under high load? (hint: external API calls)
- How would you scale Step 4 (CRM enrichment) without affecting Steps 1-3?
- What is the advantage of Temporal's activity-level retry over retrying the entire pipeline?

## Exam

1. In the Pipes and Filters pattern, what property must each filter (activity) maintain to allow **independent scaling**?
   - A) All filters must share a single database connection
   - B) Each filter processes the message using only the data in the envelope — no shared mutable state between filters — so multiple instances can run in parallel
   - C) Filters must execute in a single thread to ensure ordering
   - D) Each filter must cache results for the next filter

2. Why does the platform split processing into separate activities (Validate, Transform, Route, Deliver) rather than a single monolithic handler?
   - A) .NET requires separate classes for each async operation
   - B) Separate activities enable independent retry policies, individual scaling, and granular saga compensation — a failure in Transform doesn't require re-running Validate
   - C) Temporal cannot execute more than one method per workflow
   - D) Separate activities reduce the total number of code lines

3. What happens when an activity fails with a permanent error (e.g., invalid schema) in this platform?
   - A) The workflow retries indefinitely until the message becomes valid
   - B) The message is routed to the Dead Letter Queue with the failure reason, a Nack notification is sent to the originating system, and the workflow terminates cleanly
   - C) The activity silently drops the message
   - D) The Temporal worker crashes and restarts

---

**Previous: [← Tutorial 07 — Temporal Workflows](07-temporal-workflows.md)** | **Next: [Tutorial 09 — Content-Based Router →](09-content-based-router.md)**
