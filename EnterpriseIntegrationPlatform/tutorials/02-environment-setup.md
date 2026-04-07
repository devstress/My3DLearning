# Tutorial 02 — Temporal.io Workflow Orchestration

Orchestrate multi-step integration pipelines with Temporal.io — durable workflows that survive crashes, enforce all-or-nothing semantics, and scale horizontally via task queues. This tutorial covers the saga pattern, fan-out/split, and the scalability model that makes Temporal the backbone of reliable integrations.

## Key Types

```csharp
// src/Demo.Pipeline/ITemporalWorkflowDispatcher.cs — dispatches workflows to Temporal
public interface ITemporalWorkflowDispatcher
{
    Task<IntegrationPipelineResult> DispatchAsync(
        IntegrationPipelineInput input,
        string workflowId,
        CancellationToken cancellationToken = default);
}

// src/Demo.Pipeline/PipelineOrchestrator.cs — converts envelopes to workflow input
public sealed class PipelineOrchestrator : IPipelineOrchestrator
{
    // Maps IntegrationEnvelope<JsonElement> to IntegrationPipelineInput,
    // assigns deterministic workflow ID ("integration-{messageId}"),
    // and dispatches to Temporal
    Task ProcessAsync(IntegrationEnvelope<JsonElement> envelope, CancellationToken ct);
}

// src/Activities/IntegrationPipelineInput.cs — workflow input contract
public sealed record IntegrationPipelineInput(
    Guid MessageId, Guid CorrelationId, Guid? CausationId,
    DateTimeOffset Timestamp, string Source, string MessageType,
    string SchemaVersion, int Priority, string PayloadJson,
    string? MetadataJson, string AckSubject, string NackSubject,
    bool NotificationsEnabled = false);

// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs — saga with compensation
[Workflow]
public class AtomicPipelineWorkflow
{
    // Persist → Validate → Deliver/Compensate — all-or-nothing with rollback
}

// src/Workflow.Temporal/TemporalOptions.cs — worker scalability settings
public sealed class TemporalOptions
{
    public string TaskQueue { get; set; } = "integration-workflows";
    public string Namespace { get; set; } = "default";
    public string ServerAddress { get; set; } = "localhost:15233";
}
```

## Exercises

### 1. Dispatch a workflow and verify envelope-to-input mapping

```csharp
var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();
var orchestrator = new PipelineOrchestrator(dispatcher, Options.Create(new PipelineOptions()),
    NullLogger<PipelineOrchestrator>.Instance);

var json = JsonSerializer.Deserialize<JsonElement>("{\"orderId\":\"ORD-100\"}");
var envelope = IntegrationEnvelope<JsonElement>.Create(json, "OrderService", "order.created");

await orchestrator.ProcessAsync(envelope);
// dispatcher.LastInput.MessageId == envelope.MessageId
// dispatcher.LastInput.Source == "OrderService"
```

### 2. Saga pattern — success and failure paths

```csharp
// Success path: workflow completes, Ack published
dispatcher.ReturnsSuccess();
await orchestrator.ProcessAsync(successEnvelope);

// Failure path: workflow fails, compensation + Nack
dispatcher.ReturnsFailure("Validation failed");
await orchestrator.ProcessAsync(failureEnvelope);
```

### 3. Custom compensation with step tracking

```csharp
dispatcher.OnDispatch((input, workflowId) =>
{
    // Simulate: steps complete forward, then compensate in reverse (LIFO)
    var completed = new List<string> { "Persist", "Validate" };
    completed.Reverse(); // Compensate: Validate first, then Persist
    return new IntegrationPipelineResult(input.MessageId, false, "Schema mismatch");
});
```

### 4. Fan-out: split batch into parallel workflows

```csharp
var orderLines = new[] { "{\"sku\":\"A\"}", "{\"sku\":\"B\"}", "{\"sku\":\"C\"}" };
foreach (var line in orderLines)
{
    var json = JsonSerializer.Deserialize<JsonElement>(line);
    var envelope = IntegrationEnvelope<JsonElement>.Create(json, "Splitter", "order.line");
    await orchestrator.ProcessAsync(envelope);
}
// dispatcher.DispatchCount == 3 — three independent workflow executions
```

### 5. Scalability settings — task queues, timeouts, namespaces

```csharp
var options = new TemporalOptions();
// options.TaskQueue = "integration-workflows"  ← which worker pool processes this
// options.Namespace = "default"                ← multi-tenancy isolation
// options.ServerAddress = "localhost:15233"     ← Temporal cluster gRPC

var pipelineOptions = new PipelineOptions();
// pipelineOptions.WorkflowTimeout = TimeSpan.FromMinutes(5) ← max execution time
// pipelineOptions.TemporalTaskQueue = "integration-workflows"
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial02/Lab.cs`](../tests/TutorialLabs/Tutorial02/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial02.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial02/Exam.cs`](../tests/TutorialLabs/Tutorial02/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial02.Exam"
```

---

**Previous: [← Tutorial 01 — Introduction](01-introduction.md)** | **Next: [Tutorial 03 — Your First Message →](03-first-message.md)**
