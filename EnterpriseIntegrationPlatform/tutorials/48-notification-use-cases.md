# Tutorial 48 — Notification Use Cases

Implement notification use cases with Ack/Nack, feature flags, and priority routing.

## Key Types

```csharp
public interface INotificationMapper
{
    string MapAck(Guid messageId, Guid correlationId);
    string MapNack(Guid messageId, Guid correlationId, string errorMessage);
}

public sealed class XmlNotificationMapper : INotificationMapper
{
    public string MapAck(Guid messageId, Guid correlationId)
        => "<Ack>ok</Ack>";

    public string MapNack(Guid messageId, Guid correlationId, string errorMessage)
        => $"<Nack>not ok because of {SecurityElement.Escape(errorMessage)}</Nack>";
}
```

## Exercises

### 1. ValidateAsync — ValidMessage ReturnsSuccess

```csharp
var svc = new DefaultMessageValidationService();

var result = await svc.ValidateAsync("order.created", "{\"id\": 1}");

Assert.That(result.IsValid, Is.True);
Assert.That(result.Reason, Is.Null);
```

### 2. MessageValidationResult — Success HasExpectedValues

```csharp
var result = MessageValidationResult.Success;

Assert.That(result.IsValid, Is.True);
Assert.That(result.Reason, Is.Null);
```

### 3. MessageValidationResult — Failure HasReasonAndInvalid

```csharp
var result = MessageValidationResult.Failure("Schema mismatch");

Assert.That(result.IsValid, Is.False);
Assert.That(result.Reason, Is.EqualTo("Schema mismatch"));
```

### 4. LogAsync — Completes WithoutError

```csharp
var svc = new DefaultMessageLoggingService(
    NullLogger<DefaultMessageLoggingService>.Instance);

Assert.DoesNotThrowAsync(() =>
    svc.LogAsync(Guid.NewGuid(), "order.created", "Validated"));
```

### 5. INotificationActivityService — InterfaceShape

```csharp
var type = typeof(INotificationActivityService);

Assert.That(type.IsInterface, Is.True);
Assert.That(type.GetMethod("PublishAckAsync"), Is.Not.Null);
Assert.That(type.GetMethod("PublishNackAsync"), Is.Not.Null);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial48/Lab.cs`](../tests/TutorialLabs/Tutorial48/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial48.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial48/Exam.cs`](../tests/TutorialLabs/Tutorial48/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial48.Exam"
```

---

**Previous: [← Tutorial 47](47-saga-compensation.md)** | **Next: [Tutorial 49 →](49-testing-integrations.md)**
