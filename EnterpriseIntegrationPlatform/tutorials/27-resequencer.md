# Tutorial 27 — Resequencer

Reorder out-of-sequence messages back into their original sequence.

---

## Learning Objectives

1. Understand the Resequencer pattern and how it buffers out-of-order messages
2. Use `MessageResequencer` to accept sequenced messages and release them in order
3. Verify that a complete in-order sequence releases all messages on the final accept
4. Confirm that out-of-order arrivals are buffered and released in correct sequence order
5. Validate duplicate sequence numbers are ignored and `ActiveSequenceCount` is tracked
6. Use `ReleaseOnTimeout` to flush incomplete sequences and verify buffered messages are returned in order

---

## Key Types

```csharp
// src/Processing.Resequencer/IResequencer.cs
public interface IResequencer
{
    IReadOnlyList<IntegrationEnvelope<T>> Accept<T>(IntegrationEnvelope<T> envelope);
    IReadOnlyList<IntegrationEnvelope<T>> ReleaseOnTimeout<T>(Guid correlationId);
    int ActiveSequenceCount { get; }
}
```

```csharp
// src/Processing.Resequencer/ResequencerOptions.cs
public sealed class ResequencerOptions
{
    public TimeSpan ReleaseTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentSequences { get; set; } = 10_000;
}
```

## Lab — Guided Practice

> 💻 Run the lab tests to see each Resequencer concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Accept_InOrder_ReleasesAllWhenComplete` | Complete in-order sequence releases all messages |
| 2 | `Accept_OutOfOrder_ReleasesInCorrectSequence` | Out-of-order arrivals are buffered and released in order |
| 3 | `Accept_DuplicateSequenceNumber_IsIgnored` | Duplicate sequence numbers are silently ignored |
| 4 | `Accept_MissingSequenceInfo_ThrowsArgumentException` | Missing sequence metadata throws ArgumentException |
| 5 | `ReleaseOnTimeout_IncompleteSequence_ReleasesBuffered` | Timeout releases buffered messages in sequence order |
| 6 | `ReleaseOnTimeout_UnknownCorrelation_ReturnsEmpty` | Unknown correlation ID returns empty list |
| 7 | `ActiveSequenceCount_TracksBufferedSequences` | ActiveSequenceCount tracks number of open sequences |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial27.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Resequencer pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_LargeOutOfOrderBatch_ReleasedInSequence` | 🟢 Starter |
| 2 | `Intermediate_InterleavedSequences_EachReleasedIndependently` | 🟡 Intermediate |
| 3 | `Advanced_TimeoutPartialRelease_ThenCompleteNewSequence` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial27.Exam"
```

---

**Previous: [← Tutorial 26 — Message Replay](26-message-replay.md)** | **Next: [Tutorial 28 — Competing Consumers →](28-competing-consumers.md)**
