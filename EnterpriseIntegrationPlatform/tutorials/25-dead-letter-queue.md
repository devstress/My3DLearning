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
            $"Message expired at {envelope.ExpiresAt.Value:O}. Current time: {now:O}.",
            0, cancellationToken);
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

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial25/Lab.cs`](../tests/TutorialLabs/Tutorial25/Lab.cs)

**Objective:** Trace the Dead Letter Queue lifecycle from failure to replay, analyze how the DLQ preserves **zero message loss atomicity**, and design an operational replay workflow.

### Step 1: Trace an Expired Message to the DLQ

A message has `ExpiresAt = 2024-01-15T10:00:00Z` and the current time is `2024-01-15T10:00:01Z`. Open `src/Processing.DeadLetter/MessageExpirationChecker.cs` and trace:

1. `CheckAndRouteIfExpiredAsync` detects expiration — what `DeadLetterReason` is used?
2. What information is logged? (hint: expiry time and current time)
3. Where does the complete original envelope end up?

Verify that the **entire original envelope** is preserved in `DeadLetterEnvelope` — not just error details.

### Step 2: Design an Operational Replay Workflow

A message fails validation (`DeadLetterReason.ValidationFailed`). An operator fixes the downstream schema. Design the replay flow:

```
1. Operator queries DLQ via Admin API: GET /api/deadletter?reason=ValidationFailed
2. Operator reviews the original envelope and error details
3. Operator triggers replay: POST /api/deadletter/{id}/replay
4. Platform re-publishes the original envelope to its original topic
5. Message re-enters the pipeline from the beginning
```

What **atomicity** guarantees must the replay provide? (hint: replay must either fully re-publish or fail cleanly — no partial replays)

### Step 3: Categorize DLQ Reasons and Operational Response

| DLQ Reason | Cause | Operational Response | Can Auto-Replay? |
|-----------|-------|---------------------|-------------------|
| `MessageExpired` | TTL exceeded | Review TTL settings | No — stale data |
| `ValidationFailed` | Schema mismatch | Fix schema → replay | Yes |
| `MaxRetriesExceeded` | Transient failures | Investigate root cause → replay | Maybe |
| `PoisonMessage` | Non-retryable error | Manual intervention | No |

Why is preserving the complete original envelope critical for DLQ operations? What would an operator lose if only the error message was stored?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial25/Exam.cs`](../tests/TutorialLabs/Tutorial25/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 24 — Retry Framework](24-retry-framework.md)** | **Next: [Tutorial 26 — Message Replay →](26-message-replay.md)**
