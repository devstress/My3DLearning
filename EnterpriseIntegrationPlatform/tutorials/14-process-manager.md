# Tutorial 14 — Process Manager

Centralised stateful orchestration via Temporal workflows — decides the next step based on intermediate results and compensates on failure.

---

## Key Types

```csharp
// src/Contracts/IntegrationPipelineInput.cs
public sealed record IntegrationPipelineInput(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset Timestamp,
    string Source,
    string MessageType,
    string SchemaVersion,
    int Priority,
    string PayloadJson,
    string? MetadataJson,
    string AckSubject,
    string NackSubject)
{
    public bool NotificationsEnabled { get; init; }
}

// src/Contracts/IntegrationPipelineResult.cs
public sealed record IntegrationPipelineResult(
    Guid MessageId,
    bool IsSuccess,
    string? FailureReason = null);

// src/Demo.Pipeline/PipelineOrchestrator.cs
public sealed class PipelineOrchestrator
{
    public Task ProcessAsync<T>(IntegrationEnvelope<T> envelope) { ... }
}

// src/Demo.Pipeline/ITemporalWorkflowDispatcher.cs
public interface ITemporalWorkflowDispatcher
{
    Task<IntegrationPipelineResult> DispatchAsync(
        IntegrationPipelineInput input,
        string workflowId,
        CancellationToken cancellationToken = default);
}
```

---

## Exercises

### 1. Successful dispatch — workflow completes without error

```csharp
var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
dispatcher.DispatchAsync(
    Arg.Any<IntegrationPipelineInput>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>())
    .Returns(ci => new IntegrationPipelineResult(
        ci.ArgAt<IntegrationPipelineInput>(0).MessageId,
        IsSuccess: true));

var options = Options.Create(new PipelineOptions
{
    AckSubject = "integration.ack",
    NackSubject = "integration.nack",
});

var orchestrator = new PipelineOrchestrator(
    dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

var json = JsonSerializer.Deserialize<JsonElement>(
    """{"orderId": "ORD-1", "amount": 100}""");

var envelope = IntegrationEnvelope<JsonElement>.Create(
    json, "OrderService", "order.created");

await orchestrator.ProcessAsync(envelope);

await dispatcher.Received(1).DispatchAsync(
    Arg.Any<IntegrationPipelineInput>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>());
```

### 2. Input mapping — envelope fields map to pipeline input

```csharp
IntegrationPipelineInput? capturedInput = null;

dispatcher.DispatchAsync(
    Arg.Any<IntegrationPipelineInput>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>())
    .Returns(ci =>
    {
        capturedInput = ci.ArgAt<IntegrationPipelineInput>(0);
        return new IntegrationPipelineResult(capturedInput.MessageId, IsSuccess: true);
    });

var envelope = IntegrationEnvelope<JsonElement>.Create(
    json, "TestService", "test.event") with
{
    Priority = MessagePriority.High,
    SchemaVersion = "2.0",
    Metadata = new Dictionary<string, string> { ["tenant"] = "acme" },
};

await orchestrator.ProcessAsync(envelope);

Assert.That(capturedInput!.MessageId, Is.EqualTo(envelope.MessageId));
Assert.That(capturedInput.CorrelationId, Is.EqualTo(envelope.CorrelationId));
Assert.That(capturedInput.Source, Is.EqualTo("TestService"));
Assert.That(capturedInput.MessageType, Is.EqualTo("test.event"));
Assert.That(capturedInput.SchemaVersion, Is.EqualTo("2.0"));
Assert.That(capturedInput.Priority, Is.EqualTo((int)MessagePriority.High));
Assert.That(capturedInput.AckSubject, Is.EqualTo("test.ack"));
Assert.That(capturedInput.NackSubject, Is.EqualTo("test.nack"));
Assert.That(capturedInput.PayloadJson, Does.Contain("value"));
Assert.That(capturedInput.MetadataJson, Does.Contain("acme"));
```

### 3. Workflow ID derived from MessageId

```csharp
string? capturedWorkflowId = null;

dispatcher.DispatchAsync(
    Arg.Any<IntegrationPipelineInput>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>())
    .Returns(ci =>
    {
        capturedWorkflowId = ci.ArgAt<string>(1);
        var input = ci.ArgAt<IntegrationPipelineInput>(0);
        return new IntegrationPipelineResult(input.MessageId, IsSuccess: true);
    });

var envelope = IntegrationEnvelope<JsonElement>.Create(
    json, "Service", "event.type");

await orchestrator.ProcessAsync(envelope);

Assert.That(capturedWorkflowId, Is.Not.Null);
Assert.That(capturedWorkflowId, Is.EqualTo($"integration-{envelope.MessageId}"));
```

### 4. Failed workflow completes without throwing

```csharp
dispatcher.DispatchAsync(
    Arg.Any<IntegrationPipelineInput>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>())
    .Returns(ci => new IntegrationPipelineResult(
        ci.ArgAt<IntegrationPipelineInput>(0).MessageId,
        IsSuccess: false,
        FailureReason: "Validation failed"));

var envelope = IntegrationEnvelope<JsonElement>.Create(
    json, "Service", "event.type");

Assert.DoesNotThrowAsync(() => orchestrator.ProcessAsync(envelope));
```

### 5. IntegrationPipelineResult record shape

```csharp
var messageId = Guid.NewGuid();

var success = new IntegrationPipelineResult(messageId, IsSuccess: true);
Assert.That(success.MessageId, Is.EqualTo(messageId));
Assert.That(success.IsSuccess, Is.True);
Assert.That(success.FailureReason, Is.Null);

var failure = new IntegrationPipelineResult(
    messageId, IsSuccess: false, FailureReason: "Timeout exceeded");
Assert.That(failure.IsSuccess, Is.False);
Assert.That(failure.FailureReason, Is.EqualTo("Timeout exceeded"));
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial14/Lab.cs`](../tests/TutorialLabs/Tutorial14/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial14.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial14/Exam.cs`](../tests/TutorialLabs/Tutorial14/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial14.Exam"
```

---

**Previous: [← Tutorial 13 — Routing Slip](13-routing-slip.md)** | **Next: [Tutorial 15 — Message Translator →](15-message-translator.md)**
