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

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial21/Lab.cs`](../tests/TutorialLabs/Tutorial21/Lab.cs)

**Objective:** Trace the Aggregator's completion logic, design timeout strategies, and analyze how **idempotent** aggregation ensures **atomic** reassembly of split messages.

### Step 1: Trace Aggregation Completion

A Splitter produces 5 items with `TotalCount = 5`. Items arrive out of order: 3, 0, 4, 1, 2. Open `src/Processing.Aggregator/MessageAggregator.cs` and trace:

1. After receiving items 0, 1, 2, 3 — what does `AggregateResult.ReceivedCount` return? What is `IsComplete`?
2. When item 4 arrives, how does the Aggregator know the group is complete?
3. What `CorrelationId` links all 5 items to the same aggregate group?

### Step 2: Design a Timeout Completion Strategy

Not all split items may arrive (e.g., item 2 fails permanently). Design a timeout strategy:

- After 30 seconds from the first item, complete the aggregate with whatever has arrived
- Mark the result as `IsPartial = true`
- Route the partial aggregate to a `review.incomplete-batches` topic

What **atomicity** decision must you make: should a partial aggregate be considered "successful" or should it trigger compensation for already-delivered items?

### Step 3: Analyze Idempotent Aggregation

A message with `SequenceNumber = 2` is delivered twice (broker redelivery). Without idempotency:

- The aggregate would count 6 items instead of 5
- `IsComplete` would never be true (6 > 5) or would fire prematurely

Open `src/Processing.Aggregator/` and verify: How does `IMessageAggregateStore` handle duplicate `MessageId`s? Why is idempotency critical for **scalable** at-least-once delivery systems?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial21/Exam.cs`](../tests/TutorialLabs/Tutorial21/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 20 — Splitter](20-splitter.md)** | **Next: [Tutorial 22 — Scatter-Gather →](22-scatter-gather.md)**
