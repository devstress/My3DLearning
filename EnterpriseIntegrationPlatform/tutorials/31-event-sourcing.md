# Tutorial 31 — Event Sourcing

## What You'll Learn

- How `IEventStore` provides an append-only log of domain events
- `ISnapshotStore<TState>` for periodic snapshots to speed up replay
- `IEventProjection<TState>` and `EventProjectionEngine` for building read-side views
- `EventEnvelope` as the standard wrapper for stored events
- Optimistic concurrency via `OptimisticConcurrencyException`
- `TemporalQuery` for point-in-time replay and `InMemoryEventStore`

---

## EIP Pattern: Event-Driven Consumer (Event Sourcing)

> *"Instead of storing just the current state, store the full history of state-changing events. Reconstruct current state by replaying events."*

```
  Command ──▶ ┌──────────────┐     ┌─────────────────┐
              │  Aggregate   │────▶│  Event Store     │
              │  (validate)  │     │  (append-only)   │
              └──────────────┘     └────────┬─────────┘
                                            │
                    ┌───────────────────────┤
                    ▼                       ▼
           ┌───────────────┐     ┌───────────────────┐
           │  Snapshot     │     │  Projection       │
           │  Store        │     │  Engine           │
           └───────────────┘     └───────────────────┘
                                            │
                                            ▼
                                   Read-side views
```

The event store is the source of truth. Projections build queryable read models. Snapshots cache state at known positions.

---

## Platform Implementation

### IEventStore

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

### EventEnvelope

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

### Optimistic Concurrency

```csharp
// src/EventSourcing/OptimisticConcurrencyException.cs
public sealed class OptimisticConcurrencyException : Exception
{
    public string StreamId { get; }
    public long ExpectedVersion { get; }
    public long ActualVersion { get; }
}
```

When `AppendAsync` is called with an `expectedVersion` that does not match the stream's current version, the store throws `OptimisticConcurrencyException`. On success, `AppendAsync` returns the new stream version as a `long`. The caller must reload the stream, re-apply the command, and retry on conflict. This prevents lost updates without pessimistic locks.

### TemporalQuery

`TemporalQuery` is a static helper class that replays a stream's events up to a specific point in time, producing the projected state at that moment:

```csharp
// src/EventSourcing/TemporalQuery.cs
public static class TemporalQuery
{
    public static async Task<(TState State, long Version)> ReplayToPointInTimeAsync<TState>(
        IEventStore store,
        IEventProjection<TState> projection,
        string streamId,
        DateTimeOffset pointInTime,
        TState initialState,
        int maxEventsPerRead = 1000,
        CancellationToken cancellationToken = default) where TState : notnull;
}
```

### ISnapshotStore

```csharp
// src/EventSourcing/ISnapshotStore.cs
public interface ISnapshotStore<TState>
{
    Task SaveAsync(string streamId, TState state, long version, CancellationToken cancellationToken = default);
    Task<(TState? State, long Version)> LoadAsync(string streamId, CancellationToken cancellationToken = default);
}
```

### IEventProjection and EventProjectionEngine

```csharp
// src/EventSourcing/IEventProjection.cs
public interface IEventProjection<TState>
{
    Task<TState> ProjectAsync(TState state, EventEnvelope envelope, CancellationToken cancellationToken = default);
}
```

`IEventProjection<TState>` is an async function: given a current state and an event, it returns the new state. The `EventProjectionEngine` reads new events from the store, applies each to the appropriate `IEventProjection<TState>` implementation, and tracks the last processed version per projection. `InMemoryEventStore` implements `IEventStore` using a `ConcurrentDictionary` with full optimistic concurrency support.

---

## Scalability Dimension

The event store is **append-only** — writes never conflict with reads. Multiple projections run in parallel, each maintaining its own checkpoint. Snapshots reduce replay time from O(n) to O(1). The store can be partitioned by `StreamId`.

---

## Atomicity Dimension

Optimistic concurrency ensures **consistency without locks**. The `expectedVersion` acts as a compare-and-swap: if two writers race, one succeeds and the other gets `OptimisticConcurrencyException`. Events are immutable once appended — never modified or deleted — providing a tamper-evident audit trail.

---

## Exercises

1. An aggregate has 10,000 events. Without snapshots, what is the cost of reconstructing current state? With a snapshot at version 9,900?

2. Two commands arrive simultaneously for the same stream at version 5. Both expect version 5. Trace the optimistic concurrency flow.

3. Use `TemporalQuery.ReplayToPointInTimeAsync` to reconstruct an order aggregate's state as of yesterday at noon. What parameters do you need to supply?

---

**Previous: [← Tutorial 30 — Rule Engine](30-rule-engine.md)** | **Next: [Tutorial 32 — Multi-Tenancy →](32-multi-tenancy.md)**
