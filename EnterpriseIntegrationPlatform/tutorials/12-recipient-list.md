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

## Exercises

1. A message matches two rules contributing destinations `["audit", "billing", "audit"]`. What does `RecipientListResult` report for `ResolvedCount` and `DuplicatesRemoved`?

2. Design a metadata-based recipient list where the sender specifies destinations in `Metadata["recipients"] = "topic-a,topic-b"`. What are the trade-offs vs. rule-based resolution?

3. With 10 recipients and one slow destination (3 s latency), how does parallel publishing help compared to sequential publishing?

---

**Previous: [← Tutorial 11 — Dynamic Router](11-dynamic-router.md)** | **Next: [Tutorial 13 — Routing Slip →](13-routing-slip.md)**
