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
public sealed class OptimisticConcurrencyException : InvalidOperationException
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
        IEventStore eventStore,
        IEventProjection<TState> projection,
        string streamId,
        DateTimeOffset pointInTime,
        TState initialState,
        int maxEventsPerRead = 1000,
        CancellationToken cancellationToken = default);
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

## Lab

**Objective:** Analyze event sourcing's append-only model for **audit-complete atomicity**, trace optimistic concurrency conflict resolution, and design snapshot strategies for **scalable** aggregate reconstruction.

### Step 1: Calculate Aggregate Reconstruction Cost

An aggregate has 10,000 events. Compare reconstruction approaches:

| Approach | Events Replayed | Cost | Time (est.) |
|----------|----------------|------|-------------|
| Full replay (no snapshots) | 10,000 | High CPU + memory | ~100ms |
| Snapshot at version 9,900 | 100 | Low | ~1ms |
| Snapshot at version 9,999 | 1 | Minimal | ~0.1ms |

Open `src/EventSourcing/` and trace: How does the event store load a snapshot, then replay only subsequent events? What is the **scalability** trade-off between snapshot frequency and storage cost?

### Step 2: Trace Optimistic Concurrency Conflict

Two commands arrive simultaneously for the same stream at version 5. Both expect version 5:

```
Command A: Append event at version 5 → succeeds (stream now at version 6)
Command B: Append event at version 5 → CONFLICT (expected 5, actual 6)
```

Trace the conflict resolution:
1. What exception is thrown?
2. Does Command B retry? With what strategy?
3. How does optimistic concurrency ensure **atomic** state transitions without distributed locks?

### Step 3: Design a Temporal Query for Audit

Use `TemporalQuery.ReplayToPointInTimeAsync` to reconstruct an order aggregate's state as of yesterday at noon:

- What parameters do you supply? (stream ID, point-in-time)
- How does this differ from loading current state?
- Why is this capability essential for **regulatory compliance** and audit trails?

## Exam

1. Why does event sourcing use an append-only log rather than mutable state updates?
   - A) Append-only is faster for write operations
   - B) Every state change is permanently recorded as an immutable event — this provides a complete audit trail, enables temporal queries (reconstructing past state), and guarantees **atomic** state transitions through optimistic concurrency
   - C) Databases don't support mutable updates
   - D) Append-only reduces storage costs

2. How does optimistic concurrency prevent **atomicity** violations in concurrent event sourcing?
   - A) It uses distributed locks to prevent concurrent access
   - B) Each append specifies the expected version — if another command modified the stream first, the version mismatch is detected and the second command fails cleanly, ensuring only one writer succeeds per state transition
   - C) Events are automatically merged when conflicts occur
   - D) The event store queues concurrent commands

3. How do snapshots improve **aggregate reconstruction scalability**?
   - A) Snapshots reduce the number of events stored
   - B) A snapshot captures aggregate state at a point in time — reconstruction replays only events after the snapshot instead of the entire history, reducing reconstruction time from O(N) to O(recent events)
   - C) Snapshots are required by the event store
   - D) Snapshots improve write performance

---

**Previous: [← Tutorial 30 — Rule Engine](30-rule-engine.md)** | **Next: [Tutorial 32 — Multi-Tenancy →](32-multi-tenancy.md)**
