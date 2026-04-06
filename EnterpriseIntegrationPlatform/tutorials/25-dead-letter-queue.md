# Tutorial 25 — Dead Letter Queue

Capture unprocessable messages with full diagnostic context so they can be inspected, replayed, or discarded.

---

## Key Types

```csharp
// src/Processing.DeadLetter/IDeadLetterPublisher.cs
public interface IDeadLetterPublisher<T>
{
    Task PublishAsync(
        IntegrationEnvelope<T> envelope,
        DeadLetterReason reason,
        string errorMessage,
        int attemptCount,
        CancellationToken ct);
}

// src/Processing.DeadLetter/DeadLetterEnvelope.cs
public record DeadLetterEnvelope<T>
{
    public required IntegrationEnvelope<T> OriginalEnvelope { get; init; }
    public required DeadLetterReason Reason { get; init; }
    public required string ErrorMessage { get; init; }
    public required DateTimeOffset FailedAt { get; init; }
    public required int AttemptCount { get; init; }
}

// src/Processing.DeadLetter/DeadLetterReason.cs
public enum DeadLetterReason
{
    MaxRetriesExceeded,
    PoisonMessage,
    ProcessingTimeout,
    ValidationFailed,
    UnroutableMessage,
    MessageExpired
}

// src/Processing.DeadLetter/MessageExpirationChecker.cs
public sealed class MessageExpirationChecker<T> : IMessageExpirationChecker<T>
{
    public async Task<bool> CheckAndRouteIfExpiredAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

---

## Exercises

### Exercise 1: Publish routes to configured dead-letter topic

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var options = Options.Create(new DeadLetterOptions
{
    DeadLetterTopic = "dlq-topic",
});

var publisher = new DeadLetterPublisher<string>(producer, options);

var envelope = IntegrationEnvelope<string>.Create(
    "bad-payload", "OrderSvc", "order.created");

await publisher.PublishAsync(
    envelope,
    DeadLetterReason.MaxRetriesExceeded,
    "Failed after 3 retries",
    attemptCount: 3,
    CancellationToken.None);

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<DeadLetterEnvelope<string>>>(),
    "dlq-topic",
    Arg.Any<CancellationToken>());
```

### Exercise 2: Empty topic throws InvalidOperationException

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var options = Options.Create(new DeadLetterOptions
{
    DeadLetterTopic = "",
});

var publisher = new DeadLetterPublisher<string>(producer, options);
var envelope = IntegrationEnvelope<string>.Create(
    "data", "Svc", "type");

Assert.ThrowsAsync<InvalidOperationException>(() =>
    publisher.PublishAsync(
        envelope,
        DeadLetterReason.PoisonMessage,
        "error",
        1,
        CancellationToken.None));
```

### Exercise 3: DeadLetterEnvelope record construction

```csharp
var original = IntegrationEnvelope<string>.Create(
    "payload", "Svc", "type");

var dlEnvelope = new DeadLetterEnvelope<string>
{
    OriginalEnvelope = original,
    Reason = DeadLetterReason.ValidationFailed,
    ErrorMessage = "Schema mismatch",
    FailedAt = DateTimeOffset.UtcNow,
    AttemptCount = 2,
};

Assert.That(dlEnvelope.OriginalEnvelope.Payload, Is.EqualTo("payload"));
Assert.That(dlEnvelope.Reason, Is.EqualTo(DeadLetterReason.ValidationFailed));
Assert.That(dlEnvelope.ErrorMessage, Is.EqualTo("Schema mismatch"));
Assert.That(dlEnvelope.AttemptCount, Is.EqualTo(2));
```

### Exercise 4: Publisher preserves CorrelationId on wrapper

```csharp
IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
var producer = Substitute.For<IMessageBrokerProducer>();
producer
    .PublishAsync(
        Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
        Arg.Any<string>(),
        Arg.Any<CancellationToken>())
    .Returns(Task.CompletedTask);

var options = Options.Create(new DeadLetterOptions
{
    DeadLetterTopic = "dlq",
});

var publisher = new DeadLetterPublisher<string>(producer, options);

var originalCorrelationId = Guid.NewGuid();
var envelope = IntegrationEnvelope<string>.Create(
    "data", "Svc", "type", correlationId: originalCorrelationId);

await publisher.PublishAsync(
    envelope, DeadLetterReason.MessageExpired, "expired", 0, CancellationToken.None);

Assert.That(captured, Is.Not.Null);
Assert.That(captured!.CorrelationId, Is.EqualTo(originalCorrelationId));
```

### Exercise 5: Publisher uses custom source when configured

```csharp
IntegrationEnvelope<DeadLetterEnvelope<string>>? captured = null;
var producer = Substitute.For<IMessageBrokerProducer>();
producer
    .PublishAsync(
        Arg.Do<IntegrationEnvelope<DeadLetterEnvelope<string>>>(e => captured = e),
        Arg.Any<string>(),
        Arg.Any<CancellationToken>())
    .Returns(Task.CompletedTask);

var options = Options.Create(new DeadLetterOptions
{
    DeadLetterTopic = "dlq",
    Source = "DLQ-Publisher",
});

var publisher = new DeadLetterPublisher<string>(producer, options);

var envelope = IntegrationEnvelope<string>.Create(
    "data", "OriginalSvc", "type");

await publisher.PublishAsync(
    envelope, DeadLetterReason.UnroutableMessage, "no route", 1, CancellationToken.None);

Assert.That(captured, Is.Not.Null);
Assert.That(captured!.Source, Is.EqualTo("DLQ-Publisher"));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial25/Lab.cs`](../tests/TutorialLabs/Tutorial25/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial25.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial25/Exam.cs`](../tests/TutorialLabs/Tutorial25/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial25.Exam"
```

---

**Previous: [← Tutorial 24 — Retry Framework](24-retry-framework.md)** | **Next: [Tutorial 26 — Message Replay →](26-message-replay.md)**
