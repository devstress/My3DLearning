# Tutorial 25 — Dead Letter Queue

Capture unprocessable messages with full diagnostic context so they can be inspected, replayed, or discarded.

---

## Learning Objectives

1. Understand the Dead Letter Channel pattern and when messages are routed to a DLQ
2. Use `DeadLetterPublisher<T>` to publish failed messages with diagnostic context
3. Verify that published dead-letter envelopes preserve the original envelope and correlation ID
4. Confirm that `DeadLetterReason`, error message, and attempt count are recorded correctly
5. Validate that the `FailedAt` timestamp is set at publish time
6. Verify configuration guard: an empty `DeadLetterTopic` throws `InvalidOperationException`

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

## Lab — Guided Practice

> 💻 Run the lab tests to see each Dead Letter Queue concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Publish_MaxRetriesExceeded_SendsToDeadLetterTopic` | Message routed to configured DLQ topic |
| 2 | `Publish_PreservesOriginalEnvelope` | Original envelope payload and MessageId are preserved |
| 3 | `Publish_SetsCorrectReason` | DeadLetterReason and error message are recorded |
| 4 | `Publish_TracksAttemptCount` | Attempt count is captured in the dead-letter envelope |
| 5 | `Publish_SetsFailedAtTimestamp` | FailedAt timestamp is set at publish time |
| 6 | `Publish_PreservesCorrelationId` | CorrelationId is preserved on the wrapper envelope |
| 7 | `Publish_AllReasonValues_AreSupported` | All DeadLetterReason enum values are accepted |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial25.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Dead Letter Queue pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_MultipleFailures_AllReachDlq` | 🟢 Starter |
| 2 | `Intermediate_OriginalEnvelope_MetadataPreserved` | 🟡 Intermediate |
| 3 | `Advanced_MissingDeadLetterTopic_Throws` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial25.Exam"
```

---

**Previous: [← Tutorial 24 — Retry Framework](24-retry-framework.md)** | **Next: [Tutorial 26 — Message Replay →](26-message-replay.md)**
