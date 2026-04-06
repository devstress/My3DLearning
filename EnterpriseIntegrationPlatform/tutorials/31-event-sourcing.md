# Tutorial 31 — Event Sourcing

Store domain events as an append-only log and rebuild state by replaying them.

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

## Exercises

### 1. AppendAsync — AndReadStreamAsync Roundtrip

```csharp
var envelope = new EventEnvelope(
    Guid.NewGuid(), "stream-1", "OrderCreated",
    """{"total":42}""", 0, DateTimeOffset.UtcNow, []);

await _store.AppendAsync("stream-1", [envelope], expectedVersion: 0);

var events = await _store.ReadStreamAsync("stream-1", fromVersion: 1, count: 100);

Assert.That(events, Has.Count.EqualTo(1));
Assert.That(events[0].StreamId, Is.EqualTo("stream-1"));
Assert.That(events[0].EventType, Is.EqualTo("OrderCreated"));
Assert.That(events[0].Version, Is.EqualTo(1));
```

### 2. AppendMultiple — ReadAllBack InOrder

```csharp
var e1 = new EventEnvelope(Guid.NewGuid(), "s", "A", "d1", 0, DateTimeOffset.UtcNow, []);
var e2 = new EventEnvelope(Guid.NewGuid(), "s", "B", "d2", 0, DateTimeOffset.UtcNow, []);
var e3 = new EventEnvelope(Guid.NewGuid(), "s", "C", "d3", 0, DateTimeOffset.UtcNow, []);

await _store.AppendAsync("s", [e1], expectedVersion: 0);
await _store.AppendAsync("s", [e2], expectedVersion: 1);
await _store.AppendAsync("s", [e3], expectedVersion: 2);

var events = await _store.ReadStreamAsync("s", fromVersion: 1, count: 100);

Assert.That(events, Has.Count.EqualTo(3));
Assert.That(events[0].Version, Is.EqualTo(1));
Assert.That(events[1].Version, Is.EqualTo(2));
Assert.That(events[2].Version, Is.EqualTo(3));
Assert.That(events[0].EventType, Is.EqualTo("A"));
Assert.That(events[2].EventType, Is.EqualTo("C"));
```

### 3. AppendAsync — VersionConflict ThrowsOptimisticConcurrencyException

```csharp
var e = new EventEnvelope(Guid.NewGuid(), "s", "E", "d", 0, DateTimeOffset.UtcNow, []);
await _store.AppendAsync("s", [e], expectedVersion: 0);

var e2 = new EventEnvelope(Guid.NewGuid(), "s", "E2", "d2", 0, DateTimeOffset.UtcNow, []);

var ex = Assert.ThrowsAsync<OptimisticConcurrencyException>(
    () => _store.AppendAsync("s", [e2], expectedVersion: 0));

Assert.That(ex!.StreamId, Is.EqualTo("s"));
Assert.That(ex.ExpectedVersion, Is.EqualTo(0));
Assert.That(ex.ActualVersion, Is.EqualTo(1));
```

### 4. ReadStreamBackwardAsync — ReturnsReversedOrder

```csharp
var e1 = new EventEnvelope(Guid.NewGuid(), "s", "A", "d1", 0, DateTimeOffset.UtcNow, []);
var e2 = new EventEnvelope(Guid.NewGuid(), "s", "B", "d2", 0, DateTimeOffset.UtcNow, []);
var e3 = new EventEnvelope(Guid.NewGuid(), "s", "C", "d3", 0, DateTimeOffset.UtcNow, []);

await _store.AppendAsync("s", [e1, e2, e3], expectedVersion: 0);

var events = await _store.ReadStreamBackwardAsync("s", fromVersion: 3, count: 100);

Assert.That(events, Has.Count.EqualTo(3));
Assert.That(events[0].Version, Is.EqualTo(3));
Assert.That(events[1].Version, Is.EqualTo(2));
Assert.That(events[2].Version, Is.EqualTo(1));
```

### 5. SnapshotStore — SaveAndLoad Roundtrip

```csharp
var snapshots = new InMemorySnapshotStore<int>();

await snapshots.SaveAsync("stream-1", 42, 5);
var (state, version) = await snapshots.LoadAsync("stream-1");

Assert.That(state, Is.EqualTo(42));
Assert.That(version, Is.EqualTo(5));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial31/Lab.cs`](../tests/TutorialLabs/Tutorial31/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial31.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial31/Exam.cs`](../tests/TutorialLabs/Tutorial31/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial31.Exam"
```

---

**Previous: [← Tutorial 30 — Rule Engine](30-rule-engine.md)** | **Next: [Tutorial 32 — Multi-Tenancy →](32-multi-tenancy.md)**
