# Tutorial 20 — Splitter

## What You'll Learn

- The EIP Splitter pattern for breaking composite messages into individual items
- How `IMessageSplitter<T>` and `ISplitStrategy<T>` separate concerns
- `JsonArraySplitStrategy` for splitting JSON arrays
- How each split item gets a shared `CorrelationId`, unique `SequenceNumber`, and `TotalCount`
- The `SplitResult` with item count and published envelopes

---

## EIP Pattern: Splitter

> *"Use a Splitter to break out the composite message into a series of individual messages, each containing data related to one item."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌───────────────────┐    ┌──────────┐    ┌─────┐
  │ Composite Message │───▶│ Splitter │───▶│ A   │ (seq 0, total 3)
  │ [A, B, C]         │    │          │───▶│ B   │ (seq 1, total 3)
  └───────────────────┘    │          │───▶│ C   │ (seq 2, total 3)
                           └──────────┘
         All items share the same CorrelationId
```

A batch message arrives containing multiple items. The Splitter publishes each item as an independent message, tagged with correlation metadata so they can be reassembled later by an Aggregator (Tutorial 21).

---

## Platform Implementation

### IMessageSplitter<T>

```csharp
// src/Processing.Splitter/IMessageSplitter.cs
public interface IMessageSplitter<T>
{
    Task<SplitResult<T>> SplitAsync(
        IntegrationEnvelope<T> source,
        CancellationToken cancellationToken = default);
}
```

### ISplitStrategy<T>

```csharp
// src/Processing.Splitter/ISplitStrategy.cs
public interface ISplitStrategy<T>
{
    IReadOnlyList<T> Split(T composite);
}
```

The splitter delegates to a strategy for the actual splitting logic. This separation allows different strategies (JSON array, XML child elements, line-based) to be swapped without changing the splitter.

### JsonArraySplitStrategy

```csharp
// src/Processing.Splitter/JsonArraySplitStrategy.cs
public IReadOnlyList<JsonElement> Split(JsonElement composite)
{
    // Resolves the target array (root array or named property)
    // Clones each element to decouple from the source JsonDocument
    // Returns individual items as a list
}
```

If the root payload is not an array, set `SplitterOptions.ArrayPropertyName` to specify which property holds the array (e.g. `"items"` for `{ "items": [...] }`).

### SplitResult

```csharp
// src/Processing.Splitter/SplitResult.cs
public sealed record SplitResult<T>(
    IReadOnlyList<IntegrationEnvelope<T>> SplitEnvelopes,
    Guid SourceMessageId,
    string TargetTopic,
    int ItemCount);
```

Each split envelope receives:
- **Same `CorrelationId`** as the source — links all items to the original batch
- **Unique `SequenceNumber`** (0, 1, 2, …) — ordering within the batch
- **`TotalCount`** in metadata — how many items the batch contained

---

## Scalability Dimension

The splitter is **stateless** — it reads one composite message and produces N individual messages. Horizontal scaling via competing consumers is straightforward. Note that splitting **amplifies** message count: a batch of 100 items produces 100 messages. Capacity planning must account for this amplification factor on the target topic and downstream consumers.

---

## Atomicity Dimension

All split items are published to the target topic before the source message is Acked. If any publish fails, the source is Nacked and redelivered. On retry, the entire batch is re-split, producing the same items (splitting is deterministic). Downstream consumers use `MessageId` for deduplication. The `CorrelationId` + `SequenceNumber` + `TotalCount` triplet enables the Aggregator to know exactly when all items have arrived.

---

## Exercises

1. A message `{ "orders": [{ "id": 1 }, { "id": 2 }, { "id": 3 }] }` is split using `JsonArraySplitStrategy` with `ArrayPropertyName = "orders"`. How many envelopes are in `SplitResult.SplitEnvelopes`? What is `ItemCount`?

2. After splitting, item at sequence 1 is lost due to a downstream failure. How does the Aggregator (Tutorial 21) detect this?

3. Why does `JsonArraySplitStrategy` clone each element with `JsonSerializer.SerializeToElement`? What would happen if it didn't?

---

**Previous: [← Tutorial 19 — Content Filter](19-content-filter.md)** | **Next: [Tutorial 21 — Aggregator →](21-aggregator.md)**
