# Tutorial 49 — Testing Integrations

Write unit, contract, integration, and load tests for integration pipelines.

## Exercises

### 1. IntegrationEnvelope — Create SetsAllMandatoryFields

```csharp
var envelope = IntegrationEnvelope<string>.Create(
    "payload", "OrderService", "order.created");

Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
Assert.That(envelope.Source, Is.EqualTo("OrderService"));
Assert.That(envelope.MessageType, Is.EqualTo("order.created"));
Assert.That(envelope.Payload, Is.EqualTo("payload"));
Assert.That(envelope.SchemaVersion, Is.EqualTo("1.0"));
```

### 2. IntegrationEnvelope — CausationId TracksDerivedMessages

```csharp
var parent = IntegrationEnvelope<string>.Create(
    "parent-data", "ParentService", "parent.event");

var child = IntegrationEnvelope<string>.Create(
    "child-data", "ChildService", "child.event",
    correlationId: parent.CorrelationId,
    causationId: parent.MessageId);

Assert.That(child.CorrelationId, Is.EqualTo(parent.CorrelationId));
Assert.That(child.CausationId, Is.EqualTo(parent.MessageId));
```

### 3. FaultEnvelope — Create CapturesOriginalMessageDetails

```csharp
var original = IntegrationEnvelope<string>.Create(
    "data", "OrderService", "order.created");

var fault = FaultEnvelope.Create(
    original, "ValidationStep", "Invalid schema", 3);

Assert.That(fault.OriginalMessageId, Is.EqualTo(original.MessageId));
Assert.That(fault.CorrelationId, Is.EqualTo(original.CorrelationId));
Assert.That(fault.FaultedBy, Is.EqualTo("ValidationStep"));
Assert.That(fault.FaultReason, Is.EqualTo("Invalid schema"));
Assert.That(fault.RetryCount, Is.EqualTo(3));
```

### 4. MessagePriority — EnumValues

```csharp
Assert.That(Enum.GetValues<MessagePriority>(), Has.Length.GreaterThanOrEqualTo(4));
Assert.That((int)MessagePriority.Low, Is.EqualTo(0));
Assert.That((int)MessagePriority.Normal, Is.EqualTo(1));
Assert.That((int)MessagePriority.High, Is.EqualTo(2));
Assert.That((int)MessagePriority.Critical, Is.EqualTo(3));
```

### 5. MessageIntent — EnumValues

```csharp
Assert.That(Enum.GetValues<MessageIntent>(), Has.Length.GreaterThanOrEqualTo(3));
Assert.That((int)MessageIntent.Command, Is.EqualTo(0));
Assert.That((int)MessageIntent.Document, Is.EqualTo(1));
Assert.That((int)MessageIntent.Event, Is.EqualTo(2));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial49/Lab.cs`](../tests/TutorialLabs/Tutorial49/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial49.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial49/Exam.cs`](../tests/TutorialLabs/Tutorial49/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial49.Exam"
```

---

**Previous: [← Tutorial 48](48-notification-use-cases.md)** | **Next: [Tutorial 50 →](50-best-practices.md)**
