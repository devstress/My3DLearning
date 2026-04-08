# Tutorial 31 — Event Sourcing

Store domain events as an append-only log and rebuild state by replaying them.

---

## Learning Objectives

1. Understand the Event Sourcing pattern and append-only event storage
2. Use `InMemoryEventStore` to append events and read streams forward and backward
3. Verify version incrementing and optimistic concurrency conflict detection
4. Read subsets of a stream by starting from a specific version
5. Confirm empty streams return empty results gracefully
6. Publish event notifications to an endpoint after reading the event stream

---

## Key Types

```csharp
// src/EventSourcing/IEventStore.cs
public interface IEventStore
{
    Task<long> AppendAsync(
        string streamId,
        IReadOnlyList<EventEnvelope> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
        string streamId,
        long fromVersion,
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventEnvelope>> ReadStreamBackwardAsync(
        string streamId,
        long fromVersion,
        int count,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/EventSourcing/EventEnvelope.cs
public sealed record EventEnvelope(
    Guid EventId,
    string StreamId,
    string EventType,
    string Data,
    long Version,
    DateTimeOffset Timestamp,
    Dictionary<string, string> Metadata);
```

```csharp
// src/EventSourcing/OptimisticConcurrencyException.cs
public sealed class OptimisticConcurrencyException : InvalidOperationException
{
    public string StreamId { get; }
    public long ExpectedVersion { get; }
    public long ActualVersion { get; }
}
```

```csharp
// src/EventSourcing/TemporalQuery.cs
public static class TemporalQuery
{
    public static async Task<(TState State, long Version)> ReplayToPointInTimeAsync<TState>(
        IEventStore eventStore,
        IEventProjection<TState> projection,
        string streamId,
        DateTimeOffset pointInTime,
        TState initialState,
        int maxEventsPerRead = 1000,
        CancellationToken cancellationToken = default);
}
```

## Lab — Guided Practice

> 💻 Run the lab tests to see each Event Sourcing concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `AppendAndReadForward_RoundTrip` | Append and read-forward round trip |
| 2 | `AppendMultipleEvents_VersionsIncrement` | Multiple events increment versions |
| 3 | `ReadStreamBackward_ReturnsDescendingOrder` | Backward read returns descending order |
| 4 | `OptimisticConcurrency_ThrowsOnVersionMismatch` | Version mismatch throws concurrency exception |
| 5 | `ReadFromMiddleOfStream_ReturnsSubset` | Read from middle returns subset |
| 6 | `EmptyStream_ReturnsEmptyList` | Empty stream returns empty list |
| 7 | `PublishAllEventsToMockEndpoint` | Events publish to endpoint |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial31.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_ProjectionEngine_RebuildsSumFromEvents` | 🟢 Starter | ProjectionEngine — RebuildsSumFromEvents |
| 2 | `Intermediate_SnapshotAcceleratesRebuild` | 🟡 Intermediate | SnapshotAcceleratesRebuild |
| 3 | `Advanced_ConcurrentAppend_DetectsConflict` | 🔴 Advanced | ConcurrentAppend — DetectsConflict |

> 💻 [`tests/TutorialLabs/Tutorial31/Exam.cs`](../tests/TutorialLabs/Tutorial31/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial31.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial31.ExamAnswers"
```
---

**Previous: [← Tutorial 30 — Rule Engine](30-rule-engine.md)** | **Next: [Tutorial 32 — Multi-Tenancy →](32-multi-tenancy.md)**
