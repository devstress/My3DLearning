# Tutorial 46 — Complete End-to-End Integration

Wire together multiple EIP patterns into a complete end-to-end integration pipeline.

## Key Types

```csharp
// src/Workflow.Temporal/Workflows/IntegrationPipelineWorkflow.cs (simplified)
[Workflow]
public class IntegrationPipelineWorkflow
{
    [WorkflowRun]
    public async Task<IntegrationPipelineResult> RunAsync(IntegrationPipelineInput input)
    {
        // Step 1: Persist message to storage
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) => act.PersistMessageAsync(input),
            PipelineActivityOptions);

        // Step 2: Validate message schema and content
        var validation = await Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            ValidationActivityOptions);

        if (!validation.IsValid)
        {
            if (input.NotificationsEnabled)
            {
                await Workflow.ExecuteActivityAsync(
                    (PipelineActivities act) =>
                        act.PublishNackAsync(input.MessageId, input.CorrelationId,
                            validation.Reason ?? "Validation failed", input.NackSubject),
                    PipelineActivityOptions);
            }
            return new IntegrationPipelineResult(input.MessageId, false, validation.Reason);
        }

        // Steps 3-4: Transform and Route are handled externally via
        // the Normalizer and Content-Based Router patterns — the workflow
        // publishes to the appropriate channel and downstream consumers
        // handle format conversion and routing decisions.

        // Step 5: Publish success acknowledgment
        if (input.NotificationsEnabled)
        {
            await Workflow.ExecuteActivityAsync(
                (PipelineActivities act) =>
                    act.PublishAckAsync(input.MessageId, input.CorrelationId,
                        input.AckSubject),
                PipelineActivityOptions);
        }

        return new IntegrationPipelineResult(input.MessageId, true);
    }
}
```

```csharp
// src/Connector.Http/HttpConnectorAdapter.cs
public sealed class HttpConnectorAdapter : IConnector
{
    public async Task<ConnectorResult> SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        ConnectorSendOptions options,
        CancellationToken cancellationToken = default)
    {
        // Sends the envelope payload via HTTP to the configured endpoint
        // Returns ConnectorResult with success/failure status
    }
}
```

## Exercises

### 1. PipelineOptions — PropertiesAssignable

```csharp
var opts = new PipelineOptions
{
    AckSubject = "ack-topic",
    NackSubject = "nack-topic",
};

Assert.That(opts.AckSubject, Is.EqualTo("ack-topic"));
Assert.That(opts.NackSubject, Is.EqualTo("nack-topic"));
```

### 2. IntegrationPipelineInput — RecordShape

```csharp
var input = new IntegrationPipelineInput(
    Guid.NewGuid(), Guid.NewGuid(), null, DateTimeOffset.UtcNow,
    "OrderService", "order.created", "1.0", 1, "{}", null, "ack", "nack");

Assert.That(input.MessageId, Is.Not.EqualTo(Guid.Empty));
Assert.That(input.Source, Is.EqualTo("OrderService"));
Assert.That(input.AckSubject, Is.EqualTo("ack"));
```

### 3. IntegrationPipelineResult — RecordShape

```csharp
var result = new IntegrationPipelineResult(Guid.NewGuid(), true);

Assert.That(result.IsSuccess, Is.True);
Assert.That(result.FailureReason, Is.Null);
```

### 4. PipelineOrchestrator — ProcessAsync DispatchesToWorkflow

```csharp
var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
dispatcher.DispatchAsync(
    Arg.Any<IntegrationPipelineInput>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>())
    .Returns(new IntegrationPipelineResult(Guid.NewGuid(), true));

var options = Options.Create(new PipelineOptions
{
    AckSubject = "ack",
    NackSubject = "nack",
});

var orchestrator = new PipelineOrchestrator(
    dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

var envelope = IntegrationEnvelope<JsonElement>.Create(
    JsonSerializer.Deserialize<JsonElement>("{}"),
    "TestService", "test.event");

await orchestrator.ProcessAsync(envelope);

await dispatcher.Received(1).DispatchAsync(
    Arg.Any<IntegrationPipelineInput>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>());
```

### 5. IPipelineOrchestrator — InterfaceShape

```csharp
var type = typeof(IPipelineOrchestrator);

Assert.That(type.IsInterface, Is.True);
Assert.That(type.GetMethod("ProcessAsync"), Is.Not.Null);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial46/Lab.cs`](../tests/TutorialLabs/Tutorial46/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial46.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial46/Exam.cs`](../tests/TutorialLabs/Tutorial46/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial46.Exam"
```

---

**Previous: [← Tutorial 45](45-performance-profiling.md)** | **Next: [Tutorial 47 →](47-saga-compensation.md)**
