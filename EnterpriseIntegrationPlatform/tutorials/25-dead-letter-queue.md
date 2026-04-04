# Tutorial 25 — Dead Letter Queue

## What You'll Learn

- The EIP Dead Letter Channel pattern as the safety net for unprocessable messages
- How `IDeadLetterPublisher<T>` wraps failed messages in a `DeadLetterEnvelope`
- The `DeadLetterReason` enum: MaxRetriesExceeded, PoisonMessage, ProcessingTimeout, ValidationFailed, UnroutableMessage, MessageExpired
- How `MessageExpirationChecker` detects and routes expired messages
- Admin API capabilities: inspect, replay, and discard dead-lettered messages

---

## EIP Pattern: Dead Letter Channel

> *"When a messaging system determines that it cannot or should not deliver a message, it may elect to move the message to a Dead Letter Channel."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌────────────┐     ┌─────────────────┐
  │ Processing │──X──│ Dead Letter      │
  │ Pipeline   │     │ Publisher        │
  └────────────┘     └────────┬────────┘
       │ (failed)             │
       │                      ▼
       │              ┌───────────────────┐
       │              │ Dead Letter Queue │
       │              │ (inspect / replay │
       │              │  / discard)       │
       │              └───────────────────┘
       │
       ▼ (success)
  Downstream Topic
```

Every message that cannot be processed — after retries, due to validation failure, expiration, or poison content — is captured with full diagnostic context so it can be inspected, fixed, and replayed.

---

## Platform Implementation

### IDeadLetterPublisher<T>

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
```

### DeadLetterEnvelope<T>

```csharp
// src/Processing.DeadLetter/DeadLetterEnvelope.cs
public record DeadLetterEnvelope<T>
{
    public required IntegrationEnvelope<T> OriginalEnvelope { get; init; }
    public required DeadLetterReason Reason { get; init; }
    public required string ErrorMessage { get; init; }
    public required DateTimeOffset FailedAt { get; init; }
    public required int AttemptCount { get; init; }
}
```

The `DeadLetterEnvelope` preserves the **complete original message** alongside fault details. Nothing is lost — an operator can inspect the exact payload and headers that caused the failure.

### DeadLetterReason

```csharp
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
```

| Reason | Trigger |
|--------|---------|
| `MaxRetriesExceeded` | All retry attempts exhausted (Tutorial 24) |
| `PoisonMessage` | Message causes repeated crashes — immediate DLQ |
| `ProcessingTimeout` | Processing exceeded the allowed time window |
| `ValidationFailed` | Schema or business rule validation failed |
| `UnroutableMessage` | No routing rule matched and no default topic configured |
| `MessageExpired` | `IntegrationEnvelope.ExpiresAt` is in the past |

### MessageExpirationChecker

```csharp
// src/Processing.DeadLetter/MessageExpirationChecker.cs
public sealed class MessageExpirationChecker<T> : IMessageExpirationChecker<T>
{
    public async Task<bool> CheckAndRouteIfExpiredAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        if (!envelope.ExpiresAt.HasValue) return false;
        if (_timeProvider.GetUtcNow() <= envelope.ExpiresAt.Value) return false;

        await _deadLetterPublisher.PublishAsync(
            envelope, DeadLetterReason.MessageExpired,
            $"Message expired at {envelope.ExpiresAt.Value:O}.", 0, cancellationToken);
        return true;
    }
}
```

Uses `TimeProvider` for testability — unit tests can inject a fake clock.

---

## Scalability Dimension

The DLQ publisher is **stateless** — it wraps the envelope and publishes to the dead-letter topic. Any replica can dead-letter any message. The dead-letter topic itself is a standard broker topic that can be partitioned and replicated for high availability. The Admin API reads from this topic to support **inspect** (view dead-lettered messages), **replay** (re-publish to the original topic), and **discard** (acknowledge and remove).

---

## Atomicity Dimension

Dead-lettering is the **last resort** — it runs only after all retries are exhausted or the error is non-retryable. The publisher writes the `DeadLetterEnvelope` to the DLQ topic **before** Acking the source message. If the DLQ publish itself fails, the source message is Nacked and redelivered. This means a message can only leave the system through two paths: successful processing or the DLQ. **No message is ever silently lost.**

---

## Exercises

1. A message fails validation (`DeadLetterReason.ValidationFailed`). An operator fixes the schema and wants to reprocess it. Describe the replay flow through the Admin API.

2. A message has `ExpiresAt = 2024-01-15T10:00:00Z` and the current time is `2024-01-15T10:00:01Z`. Trace the path through `MessageExpirationChecker` and `IDeadLetterPublisher`.

3. Why does the platform preserve the **complete original envelope** in `DeadLetterEnvelope` rather than just the error details? What operational benefit does this provide?

---

**Previous: [← Tutorial 24 — Retry Framework](24-retry-framework.md)** | **Next: [Tutorial 26 →](26-next-topic.md)**
