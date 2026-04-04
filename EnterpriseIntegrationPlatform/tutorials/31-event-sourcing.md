# Tutorial 31 — Event Sourcing

## What You'll Learn

- How `IEventStore` provides an append-only log of domain events
- `ISnapshotStore` for periodic snapshots to speed up replay
- `IEventProjection` and `EventProjectionEngine` for building read-side views
- `EventEnvelope` as the standard wrapper for stored events
- Optimistic concurrency via `OptimisticConcurrencyException`
- `TemporalQuery` for time-range queries and `InMemoryEventStore`

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
    Task AppendAsync(
        string streamId,
        IReadOnlyList<EventEnvelope> events,
        long expectedVersion,
        CancellationToken ct);

    Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
        string streamId,
        long fromVersion,
        CancellationToken ct);

    Task<IReadOnlyList<EventEnvelope>> QueryAsync(
        TemporalQuery query,
        CancellationToken ct);
}
```

### EventEnvelope

```csharp
// src/EventSourcing/EventEnvelope.cs
public sealed record EventEnvelope
{
    public required string StreamId { get; init; }
    public required long Version { get; init; }
    public required string EventType { get; init; }
    public required string Payload { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public IDictionary<string, string>? Metadata { get; init; }
}
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

When `AppendAsync` is called with an `expectedVersion` that does not match the stream's current version, the store throws `OptimisticConcurrencyException`. The caller must reload the stream, re-apply the command, and retry. This prevents lost updates without pessimistic locks.

### TemporalQuery

```csharp
// src/EventSourcing/TemporalQuery.cs
public sealed record TemporalQuery(DateTimeOffset From, DateTimeOffset To, string? StreamId = null, string? EventType = null);
```

### ISnapshotStore

```csharp
// src/EventSourcing/ISnapshotStore.cs
public interface ISnapshotStore
{
    Task SaveAsync(string streamId, long version, string snapshot, CancellationToken ct);
    Task<(string Snapshot, long Version)?> LoadAsync(string streamId, CancellationToken ct);
}
```

### IEventProjection and EventProjectionEngine

```csharp
// src/EventSourcing/IEventProjection.cs
public interface IEventProjection
{
    string ProjectionName { get; }
    Task ProjectAsync(EventEnvelope envelope, CancellationToken ct);
}
```

The `EventProjectionEngine` reads new events from the store, dispatches each to registered `IEventProjection` implementations, and tracks the last processed version per projection. `InMemoryEventStore` implements `IEventStore` using a `ConcurrentDictionary` with full optimistic concurrency support.

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

3. Design a `TemporalQuery` that retrieves all `"OrderPlaced"` events from the last 24 hours across all streams.

---

**Previous: [← Tutorial 30 — Rule Engine](30-rule-engine.md)** | **Next: [Tutorial 32 — Multi-Tenancy →](32-multi-tenancy.md)**
