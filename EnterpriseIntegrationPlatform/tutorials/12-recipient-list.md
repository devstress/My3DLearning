# Tutorial 12 — Recipient List

## What You'll Learn

- The EIP Recipient List pattern and how it enables fan-out messaging
- How `IRecipientList` / `RecipientListRouter` resolves and publishes to ALL destinations
- Rule-based and metadata-based recipient resolution
- How parallel publishing avoids blocking on slow consumers
- The `RecipientListResult` with deduplication reporting

---

## EIP Pattern: Recipient List

> *"Route a message to a list of dynamically specified recipients."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
                      ┌─────────────────┐
                      │ Recipient List  │──▶ Topic A
  ──Message──▶        │   (fan-out)     │──▶ Topic B
                      │                 │──▶ Topic C
                      └─────────────────┘
      Same unmodified message sent to every resolved destination.
```

Unlike the Content-Based Router (one winner), the Recipient List sends the **same message to every resolved destination**. This is pure fan-out.

---

## Platform Implementation

### IRecipientList

```csharp
// src/Processing.Routing/IRecipientList.cs
public interface IRecipientList
{
    Task<RecipientListResult> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

### RecipientListRouter (concrete)

```csharp
// src/Processing.Routing/RecipientListRouter.cs
public sealed class RecipientListRouter : IRecipientList
{
    // Resolves destinations from:
    // 1. Rule-based: ALL matching RecipientListRule rules contribute destinations
    // 2. Metadata-based: comma-separated value from envelope metadata key

    public async Task<RecipientListResult> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default) { ... }
}
```

The router pre-compiles regex patterns at construction time (`RegexOptions.Compiled`) and caches them per rule. Duplicate destinations are removed (case-insensitive) before publishing.

### RecipientListResult

```csharp
// src/Processing.Routing/RecipientListResult.cs
public sealed record RecipientListResult(
    IReadOnlyList<string> Destinations,
    int ResolvedCount,
    int DuplicatesRemoved);
```

Publishing is done **concurrently** using `Task.WhenAll` — the router does not wait for Topic A to complete before publishing to Topic B. This prevents one slow consumer from blocking the entire fan-out.

---

## Scalability Dimension

The `RecipientListRouter` is **stateless** — destination resolution depends only on the immutable rule set and the envelope content. Multiple replicas can fan-out independently. The fan-out multiplies downstream load by the number of recipients, so capacity planning must account for the amplification factor. If a message resolves to 5 destinations, each replica generates 5 publishes per inbound message.

---

## Atomicity Dimension

Fan-out introduces an **atomicity challenge**: what if 3 of 5 publishes succeed but 2 fail? The platform strategy is:

1. Attempt all publishes concurrently.
2. If any publish fails, the entire operation is considered failed and the source message is Nacked.
3. The broker redelivers the message, and the router retries all publishes (downstream consumers must be idempotent on `MessageId`).

This ensures either all recipients get the message or the source is redelivered.

---

## Lab

**Objective:** Analyze how the Recipient List pattern enables **scalable fan-out** to multiple destinations, design duplicate-safe publishing, and measure the performance impact of parallel vs. sequential delivery.

### Step 1: Trace a Recipient List Resolution

A message matches two routing rules that produce destinations `["audit", "billing", "audit"]`. Open `src/Processing.Routing/RecipientListRouter.cs` and trace:

1. How are duplicate destinations handled? What does `RecipientListResult.DuplicatesRemoved` report?
2. What is the final `ResolvedCount`?
3. How does the router publish to each destination — sequentially or in parallel?

### Step 2: Design a Metadata-Driven Recipient List

Some integration scenarios require the **sender** to specify recipients dynamically via envelope metadata:

```csharp
envelope.Metadata["recipients"] = "audit,billing,compliance";
```

Design this approach and compare trade-offs:

| Approach | Pros | Cons |
|----------|------|------|
| Rule-based (server-side) | Centralized control, auditable | ? |
| Metadata-based (sender-specified) | ? | Sender must know all destinations |

Which approach provides better **atomicity** guarantees? (hint: what if the sender specifies a non-existent topic?)

### Step 3: Analyze Fan-Out Scalability

With 10 recipients and one slow destination (3-second latency):

- How does parallel publishing (platform's default) compare to sequential publishing?
- What is the total latency for parallel vs. sequential? (hint: parallel ≈ max latency, sequential ≈ sum)
- If the slow destination fails, should the message be Ack'd or Nack'd for the other 9 successful deliveries? Design your atomicity strategy.

## Exam

1. A Recipient List resolves 5 destinations. Publishing to destination 3 fails. What should the platform do to maintain **atomicity**?
   - A) Silently skip destination 3 and Ack the remaining 4
   - B) Log the failure and track partial delivery — the message enters a compensable state where the failed destination can be retried independently without re-publishing to the successful 4
   - C) Retry all 5 destinations from the beginning
   - D) Route the entire message to the Dead Letter Queue

2. Why does the Recipient List remove duplicate destinations before publishing?
   - A) Duplicates are not supported by the NATS protocol
   - B) Publishing the same message to the same topic multiple times creates duplicate processing downstream — de-duplication ensures **idempotent fan-out** at the routing layer
   - C) Duplicate topics cause build errors
   - D) The broker ignores duplicate publishes automatically

3. How does parallel publishing to multiple recipients improve **throughput scalability**?
   - A) It reduces the total message size
   - B) Total fan-out latency equals the slowest recipient (not the sum of all) — this is critical when scaling to dozens of recipients, as sequential publishing would create unacceptable pipeline latency
   - C) Parallel publishing uses less memory than sequential
   - D) The broker handles parallelism internally regardless of how the producer publishes

---

**Previous: [← Tutorial 11 — Dynamic Router](11-dynamic-router.md)** | **Next: [Tutorial 13 — Routing Slip →](13-routing-slip.md)**
