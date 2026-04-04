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
    Task SaveMessageAsync<T>(
        IntegrationEnvelope<T> envelope,
        DeliveryStatus status,
        CancellationToken cancellationToken = default);

    Task UpdateDeliveryStatusAsync(
        Guid messageId,
        DeliveryStatus status,
        CancellationToken cancellationToken = default);

    Task SaveFaultAsync(
        FaultEnvelope fault,
        CancellationToken cancellationToken = default);
}
```

**Purpose:** Save messages to Cassandra with delivery status tracking. Every message is persisted on entry (status: `Pending`), updated as it progresses (`InFlight`, `Delivered`, `Failed`).

### IMessageValidationService

```csharp
// src/Activities/IMessageValidationService.cs
public interface IMessageValidationService
{
    Task<ValidationResult> ValidateAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

**Purpose:** Validate message content — schema validation, required fields, business rules. Returns errors if validation fails.

### INotificationActivityService

```csharp
// src/Activities/INotificationActivityService.cs
public interface INotificationActivityService
{
    Task PublishAckAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);

    Task PublishNackAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyList<string> errors,
        CancellationToken cancellationToken = default);
}
```

**Purpose:** Publish Ack/Nack notifications. On success, publish Ack so downstream systems know the message was processed. On failure, publish Nack so they can react.

### ICompensationActivityService

```csharp
// src/Activities/ICompensationActivityService.cs
public interface ICompensationActivityService
{
    Task ExecuteCompensationAsync(
        string stepName,
        IntegrationPipelineInput input,
        CancellationToken cancellationToken = default);
}
```

**Purpose:** Undo the effects of a completed step during saga compensation.

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
        await _consumer.SubscribeAsync<object>(
            topic: _options.InputTopic,
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
public class PipelineOrchestrator : IPipelineOrchestrator
{
    public async Task ProcessAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken ct)
    {
        var input = new IntegrationPipelineInput
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            // ... map from envelope to workflow input
        };

        await _dispatcher.DispatchAsync(input, ct);
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
    public string InputTopic { get; set; }      // Where to listen
    public string ConsumerGroup { get; set; }    // Consumer group name
    public int WorkerConcurrency { get; set; }   // Parallel processing
    public TimeSpan ProcessingTimeout { get; set; } // Per-message timeout
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
        var envelope = CreateTestEnvelope();

        await _persistence.SaveMessageAsync(envelope, DeliveryStatus.Pending);

        await _persistence.Received(1).SaveMessageAsync(
            envelope, DeliveryStatus.Pending, Arg.Any<CancellationToken>());
    }
}
```

---

## Exercises

1. **Design a pipeline**: You receive XML invoices via SFTP. Design the activity sequence: what activities do you need? In what order?

2. **Failure handling**: Activity 3 (Transform) fails with a transient error. What happens? What if it fails with a permanent error (invalid schema)?

3. **Extend the pipeline**: You need to add a "content enrichment" step that looks up customer data from a CRM API. Where in the activity chain would you add it? What interface would the activity use?

---

**Previous: [← Tutorial 07 — Temporal Workflows](07-temporal-workflows.md)** | **Next: [Tutorial 09 — Content-Based Router →](09-content-based-router.md)**
