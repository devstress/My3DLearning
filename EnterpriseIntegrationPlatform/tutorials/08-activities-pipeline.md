# Tutorial 08 — Activities and the Pipeline

Activity service interfaces, the Pipes-and-Filters pipeline, and end-to-end message orchestration via Temporal.

---

## Key Types

```csharp
// src/Activities/IPersistenceActivityService.cs
public interface IPersistenceActivityService
{
    Task SaveMessageAsync(
        IntegrationPipelineInput input,
        CancellationToken cancellationToken = default);

    Task UpdateDeliveryStatusAsync(
        Guid messageId, Guid correlationId,
        DateTimeOffset recordedAt, string status,
        CancellationToken cancellationToken = default);

    Task SaveFaultAsync(
        Guid messageId, Guid correlationId,
        string messageType, string faultedBy, string reason, int retryCount,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Activities/IMessageValidationService.cs
public interface IMessageValidationService
{
    Task<MessageValidationResult> ValidateAsync(
        string messageType, string payloadJson);
}

public record MessageValidationResult(bool IsValid, string? Reason = null)
{
    public static MessageValidationResult Success { get; } = new(true);
    public static MessageValidationResult Failure(string reason) => new(false, reason);
}
```

```csharp
// src/Activities/INotificationActivityService.cs
public interface INotificationActivityService
{
    Task PublishAckAsync(
        Guid messageId, Guid correlationId, string topic,
        CancellationToken cancellationToken = default);

    Task PublishNackAsync(
        Guid messageId, Guid correlationId, string reason, string topic,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Activities/ICompensationActivityService.cs
public interface ICompensationActivityService
{
    Task<bool> CompensateAsync(Guid correlationId, string stepName);
}
```

```csharp
// src/Contracts/IntegrationPipelineInput.cs
public sealed record IntegrationPipelineInput(
    Guid MessageId, Guid CorrelationId, Guid? CausationId,
    DateTimeOffset Timestamp, string Source, string MessageType,
    string SchemaVersion, int Priority, string PayloadJson,
    string? MetadataJson, string AckSubject, string NackSubject);
```

---

## Exercises

### 1. Verify activity classes exist with expected methods

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
Assert.That(methodNames, Does.Contain("SaveFaultAsync"));
Assert.That(methodNames, Does.Contain("PublishAckAsync"));
Assert.That(methodNames, Does.Contain("PublishNackAsync"));
Assert.That(methodNames, Does.Contain("LogStageAsync"));

var sagaActivities = assembly.GetTypes()
    .FirstOrDefault(t => t.Name == "SagaCompensationActivities");
Assert.That(sagaActivities, Is.Not.Null);
Assert.That(sagaActivities!.GetMethod("CompensateStepAsync"), Is.Not.Null);
```

### 2. Pipeline: Create → Validate → Transform → Route

```csharp
var validationService = Substitute.For<IMessageValidationService>();
var loggingService = Substitute.For<IMessageLoggingService>();
var producer = Substitute.For<IMessageBrokerProducer>();

const string messageType = "order.created";
const string payloadJson = "{\"orderId\": \"ORD-500\"}";

// Step 1: Create envelope
var envelope = IntegrationEnvelope<string>.Create(
    payloadJson, "OrderService", messageType) with
{
    Intent = MessageIntent.Command,
};
Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));

// Step 2: Validate
validationService.ValidateAsync(messageType, payloadJson)
    .Returns(MessageValidationResult.Success);
var validationResult = await validationService.ValidateAsync(messageType, payloadJson);
Assert.That(validationResult.IsValid, Is.True);

// Step 3: Transform — enrich metadata
envelope = envelope with
{
    Metadata = new Dictionary<string, string>(envelope.Metadata)
    {
        ["region"] = "us-east",
        ["validated"] = "true",
    },
};
Assert.That(envelope.Metadata["region"], Is.EqualTo("us-east"));

// Step 4: Route — publish to destination
await producer.PublishAsync(envelope, "orders.us-east");

await producer.Received(1).PublishAsync(
    Arg.Is<IntegrationEnvelope<string>>(
        e => e.Metadata.ContainsKey("region") && e.Metadata["region"] == "us-east"),
    Arg.Is("orders.us-east"),
    Arg.Any<CancellationToken>());
```

### 3. Chained activities: Persist → Log → Validate → Log

```csharp
var persistenceService = Substitute.For<IPersistenceActivityService>();
var loggingService = Substitute.For<IMessageLoggingService>();
var validationService = Substitute.For<IMessageValidationService>();

var input = new IntegrationPipelineInput(
    MessageId: Guid.NewGuid(), CorrelationId: Guid.NewGuid(),
    CausationId: null, Timestamp: DateTimeOffset.UtcNow,
    Source: "Lab08", MessageType: "lab.pipeline",
    SchemaVersion: "1.0", Priority: 1,
    PayloadJson: "{\"item\": \"widget\"}", MetadataJson: null,
    AckSubject: "ack.lab08", NackSubject: "nack.lab08");

persistenceService.SaveMessageAsync(input, Arg.Any<CancellationToken>())
    .Returns(Task.CompletedTask);
loggingService.LogAsync(input.MessageId, input.MessageType, Arg.Any<string>())
    .Returns(Task.CompletedTask);
validationService.ValidateAsync(input.MessageType, input.PayloadJson)
    .Returns(MessageValidationResult.Success);

await persistenceService.SaveMessageAsync(input);
await loggingService.LogAsync(input.MessageId, input.MessageType, "Received");
var result = await validationService.ValidateAsync(input.MessageType, input.PayloadJson);
await loggingService.LogAsync(input.MessageId, input.MessageType,
    result.IsValid ? "Validated" : "ValidationFailed");

Received.InOrder(() =>
{
    persistenceService.SaveMessageAsync(input, Arg.Any<CancellationToken>());
    loggingService.LogAsync(input.MessageId, input.MessageType, "Received");
    validationService.ValidateAsync(input.MessageType, input.PayloadJson);
    loggingService.LogAsync(input.MessageId, input.MessageType, "Validated");
});
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial08/Lab.cs`](../tests/TutorialLabs/Tutorial08/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial08.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial08/Exam.cs`](../tests/TutorialLabs/Tutorial08/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial08.Exam"
```

---

**Previous: [← Tutorial 07 — Temporal Workflows](07-temporal-workflows.md)** | **Next: [Tutorial 09 — Content-Based Router →](09-content-based-router.md)**
