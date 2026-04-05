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

## Lab

**Objective:** Split composite messages into individual items, trace how `SequenceNumber` and `TotalCount` enable the Aggregator to reassemble split messages, and analyze **atomicity** when a split item fails.

### Step 1: Split a Composite Message

A message `{ "orders": [{ "id": 1, "total": 50 }, { "id": 2, "total": 150 }, { "id": 3, "total": 75 }] }` is split using `JsonArraySplitStrategy` with `ArrayPropertyName = "orders"`. Open `src/Processing.Splitter/` and trace:

1. How many envelopes are in `SplitResult.SplitEnvelopes`?
2. What is `ItemCount`?
3. What `SequenceNumber` and `TotalCount` does each split envelope carry?
4. Do all split envelopes share the same `CorrelationId` as the original?

### Step 2: Trace Atomicity When a Split Item Fails

After splitting, the 3 items are processed independently. Item 2 (sequence 1) fails delivery:

| Item | SequenceNumber | Status |
|------|---------------|--------|
| `{ "id": 1 }` | 0 | ✅ Delivered |
| `{ "id": 2 }` | 1 | ❌ Failed |
| `{ "id": 3 }` | 2 | ✅ Delivered |

Questions:
- How does the Aggregator (Tutorial 21) detect that item 2 is missing? (hint: `TotalCount = 3` but only 2 arrived)
- Should the Aggregator wait indefinitely or timeout? What timeout strategy preserves **atomicity**?
- Should items 1 and 3 be rolled back (saga compensation), or should only item 2 be retried?

### Step 3: Evaluate Splitter Scalability

Splitting a message with 1,000 items creates 1,000 individual messages. Analyze:

- Each split message is independently processed — what parallelism level is achievable?
- What is the memory impact of cloning 1,000 JSON elements? (hint: `JsonSerializer.SerializeToElement` creates deep copies)
- Why does `JsonArraySplitStrategy` clone each element rather than using references? What **concurrency** bug would occur without cloning?

## Exam

1. After splitting, why does each split envelope carry `SequenceNumber` and `TotalCount`?
   - A) For sorting messages alphabetically
   - B) These fields enable the downstream Aggregator to detect missing items and reassemble the complete set — without them, the Aggregator cannot determine when all pieces have arrived or which pieces are missing
   - C) The broker requires sequence numbers for storage
   - D) They are used for message deduplication

2. Why does the Splitter clone each array element rather than using references to the original?
   - A) .NET doesn't support object references in records
   - B) Cloning ensures each split message is independently serializable and processable — without cloning, concurrent modifications by downstream consumers could corrupt the shared source data, violating processing **atomicity**
   - C) Cloning is faster than referencing
   - D) The broker serializer requires cloned objects

3. A batch message with 100 items is split. Item 47 fails after items 1-46 and 48-100 succeed. What is the **scalable** recovery strategy?
   - A) Retry all 100 items from the beginning
   - B) Retry only item 47 using its `CorrelationId` and `SequenceNumber` — the other 99 items are already committed and don't need reprocessing, enabling efficient partial recovery
   - C) Route all 100 items to the Dead Letter Queue
   - D) Wait for item 47 to auto-heal

---

**Previous: [← Tutorial 19 — Content Filter](19-content-filter.md)** | **Next: [Tutorial 21 — Aggregator →](21-aggregator.md)**
