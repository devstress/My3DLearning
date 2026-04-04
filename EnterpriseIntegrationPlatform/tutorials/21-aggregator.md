# Tutorial 21 — Aggregator

## What You'll Learn

- The EIP Aggregator pattern for combining related messages into one
- How `IMessageAggregator<TItem,TAggregate>` collects and releases groups
- `ICompletionStrategy` and `CountCompletionStrategy` for deciding when a group is ready
- `IAggregationStrategy` for combining items into the aggregate payload
- `IMessageAggregateStore` for persisting in-flight groups
- The `AggregateResult` with `IsComplete`, `ReceivedCount`, and `CorrelationId`

---

## EIP Pattern: Aggregator

> *"Use a stateful filter, an Aggregator, to collect and store individual messages until a complete set of related messages has been received. Then, the Aggregator publishes a single message distilled from the individual messages."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  Item A (corr=X, seq=0) ──▶ ┌────────────┐
  Item B (corr=X, seq=1) ──▶ │ Aggregator │──▶ Aggregate [A,B,C]
  Item C (corr=X, seq=2) ──▶ └────────────┘    (published when complete)
```

The Aggregator is the counterpart to the Splitter (Tutorial 20). It collects individual messages sharing the same `CorrelationId` and, when the group is complete, combines them into a single aggregate message.

---

## Platform Implementation

### IMessageAggregator<TItem, TAggregate>

```csharp
// src/Processing.Aggregator/IMessageAggregator.cs
public interface IMessageAggregator<TItem, TAggregate>
{
    Task<AggregateResult<TAggregate>> AggregateAsync(
        IntegrationEnvelope<TItem> envelope,
        CancellationToken cancellationToken = default);
}
```

### ICompletionStrategy<T>

```csharp
// src/Processing.Aggregator/ICompletionStrategy.cs
public interface ICompletionStrategy<T>
{
    bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group);
}
```

### CountCompletionStrategy<T>

```csharp
// src/Processing.Aggregator/CountCompletionStrategy.cs
public sealed class CountCompletionStrategy<T> : ICompletionStrategy<T>
{
    public CountCompletionStrategy(int expectedCount) { ... }
    public bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group) =>
        group.Count >= _expectedCount;
}
```

Reads `TotalCount` from the envelope metadata (set by the Splitter) to know the expected group size.

### IAggregationStrategy<TItem, TAggregate>

```csharp
// src/Processing.Aggregator/IAggregationStrategy.cs
public interface IAggregationStrategy<TItem, TAggregate>
{
    TAggregate Aggregate(IReadOnlyList<TItem> items);
}
```

### IMessageAggregateStore<T>

```csharp
// src/Processing.Aggregator/IMessageAggregateStore.cs
public interface IMessageAggregateStore<T>
{
    Task<IReadOnlyList<IntegrationEnvelope<T>>> AddAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);

    Task RemoveGroupAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
```

### AggregateResult

```csharp
// src/Processing.Aggregator/AggregateResult.cs
public sealed record AggregateResult<TAggregate>(
    bool IsComplete,
    IntegrationEnvelope<TAggregate>? AggregateEnvelope,
    Guid CorrelationId,
    int ReceivedCount);
```

---

## Scalability Dimension

The Aggregator is **stateful** — it must collect items across multiple messages. The `IMessageAggregateStore` abstracts the storage (in-memory, Redis, Cassandra). For horizontal scaling, the store must be **shared** across replicas or messages must be **partitioned by CorrelationId** so all items in a group land on the same replica. Partition-based routing is the preferred approach as it avoids distributed locking.

---

## Atomicity Dimension

Each `AggregateAsync` call atomically adds the item to the store and checks completion. If the group becomes complete, the aggregate is published and the group is removed from the store — all within a single logical operation. If the process crashes after adding but before publishing, the item is re-added on redelivery (the store must be idempotent on `MessageId`). The aggregate is published before the final item's source message is Acked.

---

## Exercises

1. A Splitter produces 5 items with `TotalCount = 5`. After receiving items 0, 1, 2, 3, what does `AggregateResult.ReceivedCount` return? What is `IsComplete`?

2. Design a `TimeoutCompletionStrategy` that completes a group if 30 seconds pass since the first item arrived. What challenges does this introduce?

3. Why must the `IMessageAggregateStore` be idempotent on `MessageId`? What happens without idempotency if a message is redelivered?

---

**Previous: [← Tutorial 20 — Splitter](20-splitter.md)** | **Next: [Tutorial 22 — Scatter-Gather →](22-scatter-gather.md)**
