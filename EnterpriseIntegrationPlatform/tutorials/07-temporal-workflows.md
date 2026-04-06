# Tutorial 07 — Temporal Workflows

Durable workflow orchestration with `IntegrationPipelineWorkflow`, `AtomicPipelineWorkflow`, and saga compensation.

---

## Key Types

```csharp
// src/Workflow.Temporal/TemporalOptions.cs
public sealed class TemporalOptions
{
    public const string SectionName = "Temporal";
    public string ServerAddress { get; set; } = "localhost:15233";
    public string Namespace { get; set; } = "default";
    public string TaskQueue { get; set; } = "integration-workflows";
}
```

```csharp
// src/Workflow.Temporal/Workflows/IntegrationPipelineWorkflow.cs
[Workflow]
public class IntegrationPipelineWorkflow
{
    [WorkflowRun]
    public async Task<IntegrationPipelineResult> RunAsync(
        IntegrationPipelineInput input) { /* Persist → Log → Validate → Ack/Nack */ }
}
```

```csharp
// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs
[Workflow]
public class AtomicPipelineWorkflow
{
    [WorkflowRun]
    public async Task<AtomicPipelineResult> RunAsync(
        IntegrationPipelineInput input) { /* Persist → Validate → Compensate on failure */ }
}
```

```csharp
// src/Activities/IMessageValidationService.cs
public interface IMessageValidationService
{
    Task<MessageValidationResult> ValidateAsync(string messageType, string payloadJson);
}

public record MessageValidationResult(bool IsValid, string? Reason = null)
{
    public static MessageValidationResult Success { get; } = new(true);
    public static MessageValidationResult Failure(string reason) => new(false, reason);
}
```

```csharp
// src/Activities/IMessageLoggingService.cs
public interface IMessageLoggingService
{
    Task LogAsync(Guid messageId, string messageType, string stage);
}
```

---

## Exercises

### 1. Verify workflow types exist in the Temporal assembly

```csharp
var assembly = typeof(TemporalOptions).Assembly;

var pipeline = assembly.GetTypes()
    .FirstOrDefault(t => t.Name == "IntegrationPipelineWorkflow");
Assert.That(pipeline, Is.Not.Null);
Assert.That(pipeline!.IsClass, Is.True);

var atomic = assembly.GetTypes()
    .FirstOrDefault(t => t.Name == "AtomicPipelineWorkflow");
Assert.That(atomic, Is.Not.Null);

var saga = assembly.GetTypes()
    .FirstOrDefault(t => t.Name == "SagaCompensationWorkflow");
Assert.That(saga, Is.Not.Null);
```

### 2. TemporalOptions defaults and overrides

```csharp
var options = new TemporalOptions();

Assert.That(options.ServerAddress, Is.EqualTo("localhost:15233"));
Assert.That(options.Namespace, Is.EqualTo("default"));
Assert.That(options.TaskQueue, Is.EqualTo("integration-workflows"));
Assert.That(TemporalOptions.SectionName, Is.EqualTo("Temporal"));

options = new TemporalOptions
{
    ServerAddress = "temporal.prod.internal:7233",
    Namespace = "production",
    TaskQueue = "prod-integration",
};

Assert.That(options.ServerAddress, Is.EqualTo("temporal.prod.internal:7233"));
Assert.That(options.Namespace, Is.EqualTo("production"));
Assert.That(options.TaskQueue, Is.EqualTo("prod-integration"));
```

### 3. Verify activity classes expose expected methods

```csharp
var assembly = typeof(TemporalOptions).Assembly;

var integrationActivities = assembly.GetTypes()
    .FirstOrDefault(t => t.Name == "IntegrationActivities");
Assert.That(integrationActivities, Is.Not.Null);
Assert.That(integrationActivities!.GetMethod("ValidateMessageAsync"), Is.Not.Null);
Assert.That(integrationActivities.GetMethod("LogProcessingStageAsync"), Is.Not.Null);

var pipelineActivities = assembly.GetTypes()
    .FirstOrDefault(t => t.Name == "PipelineActivities");
Assert.That(pipelineActivities, Is.Not.Null);

var methodNames = pipelineActivities!
    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
    .Select(m => m.Name).ToList();

Assert.That(methodNames, Does.Contain("PersistMessageAsync"));
Assert.That(methodNames, Does.Contain("UpdateDeliveryStatusAsync"));
Assert.That(methodNames, Does.Contain("PublishAckAsync"));
Assert.That(methodNames, Does.Contain("PublishNackAsync"));
Assert.That(methodNames, Does.Contain("LogStageAsync"));
```

### 4. Mock workflow activity chain: Validate → Log

```csharp
var validationService = Substitute.For<IMessageValidationService>();
var loggingService = Substitute.For<IMessageLoggingService>();

var messageId = Guid.NewGuid();
const string messageType = "order.created";
const string payloadJson = "{\"orderId\": \"ORD-001\"}";

validationService.ValidateAsync(messageType, payloadJson)
    .Returns(MessageValidationResult.Success);
loggingService.LogAsync(messageId, messageType, Arg.Any<string>())
    .Returns(Task.CompletedTask);

var validationResult = await validationService.ValidateAsync(messageType, payloadJson);
Assert.That(validationResult.IsValid, Is.True);

await loggingService.LogAsync(messageId, messageType, "Validated");

await validationService.Received(1).ValidateAsync(messageType, payloadJson);
await loggingService.Received(1).LogAsync(messageId, messageType, "Validated");
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial07/Lab.cs`](../tests/TutorialLabs/Tutorial07/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial07.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial07/Exam.cs`](../tests/TutorialLabs/Tutorial07/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial07.Exam"
```

---

**Previous: [← Tutorial 06 — Messaging Channels](06-messaging-channels.md)** | **Next: [Tutorial 08 — Activities and the Pipeline →](08-activities-pipeline.md)**
