# Tutorial 26 — Message Replay

## What You'll Learn

- How `IMessageReplayer` enables selective re-processing of historical messages
- How `IMessageReplayStore` persists replay-eligible messages for audit and reprocessing
- The `ReplayFilter` value object for targeting messages by timestamp range, CorrelationId, or MessageType
- The `ReplayResult` record with replayed, skipped, and failed counts
- The `ReplayId` header added to every replayed message for audit-trail separation

---

## EIP Pattern: Message Replay

> *"Replay allows an operator to re-inject previously processed messages into the pipeline — essential for disaster recovery, reprocessing after bug fixes, and audit verification."*

```
  ┌────────────────┐       ┌──────────────────┐
  │ Replay Store   │◀──────│  Original        │
  │ (persisted     │       │  Processing      │
  │  messages)     │       └──────────────────┘
  └───────┬────────┘
          │  ReplayFilter
          ▼
  ┌────────────────┐       ┌──────────────────┐
  │ Message        │──────▶│  Pipeline        │
  │ Replayer       │       │  (re-ingested)   │
  └────────────────┘       └──────────────────┘
          │
          ▼
   ReplayId header injected
```

Messages are stored as they flow through the pipeline. When a replay is requested, the `ReplayFilter` selects a subset, and each message is re-published with a unique `ReplayId` header so downstream consumers can distinguish replayed messages from originals.

---

## Platform Implementation

### IMessageReplayer

```csharp
// src/Processing.Replay/IMessageReplayer.cs
public interface IMessageReplayer
{
    Task<ReplayResult> ReplayAsync(ReplayFilter filter, CancellationToken ct);
}
```

### IMessageReplayStore

```csharp
// src/Processing.Replay/IMessageReplayStore.cs
public interface IMessageReplayStore
{
    Task StoreForReplayAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct);
    IAsyncEnumerable<IntegrationEnvelope<object>> GetMessagesForReplayAsync(string topic, ReplayFilter filter, int maxMessages, CancellationToken ct);
}
```

### ReplayFilter

```csharp
// src/Processing.Replay/ReplayFilter.cs
public record ReplayFilter
{
    public Guid? CorrelationId { get; init; }
    public string? MessageType { get; init; }
    public DateTimeOffset? FromTimestamp { get; init; }
    public DateTimeOffset? ToTimestamp { get; init; }
}
```

| Filter Property | Usage |
|-----------------|-------|
| `FromTimestamp` / `ToTimestamp` | Date-range replay — e.g. replay all messages from the last hour |
| `CorrelationId` | Replay a single business transaction (typed as `Guid?`) |
| `MessageType` | Replay all messages of a specific type after a schema fix |

### ReplayResult

```csharp
// src/Processing.Replay/ReplayResult.cs
public record ReplayResult
{
    public required int ReplayedCount { get; init; }
    public required int SkippedCount { get; init; }
    public required int FailedCount { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}
```

Every replayed message receives a `ReplayId` header (a GUID) linking it back to the replay operation. This separates replayed traffic from live traffic in dashboards and audit logs.

---

## Scalability Dimension

The replay store is **read-heavy** — writes happen once per message, but replays can query millions of records. Production stores should support indexed queries on `CorrelationId`, `MessageType`, and `CreatedAt`. The replayer itself is stateless: it reads from the store, publishes to the broker, and records the `ReplayResult`. Multiple replay operations can run concurrently because each gets a unique `ReplayId`.

---

## Atomicity Dimension

Replay re-publishes messages to the **same ingress topic** they originally entered. This means all validation, routing, and transformation rules apply again — the message is not injected halfway through the pipeline. If a replay fails mid-batch, `ReplayResult.ReplayedCount`, `SkippedCount`, and `FailedCount` together account for every message matched by the filter. The `ReplayId` header ensures idempotent consumers can detect and deduplicate replayed messages.

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial26/Lab.cs`](../tests/TutorialLabs/Tutorial26/Lab.cs)

**Objective:** Design a message replay operation for a production incident, analyze how the `ReplayId` header prevents duplicate processing, and evaluate replay store **scalability** requirements.

### Step 1: Design a Time-Window Replay

An operator discovers a bug in the content enricher that corrupted messages between 09:00 and 09:30 UTC on January 15th. Write the `ReplayFilter`:

```csharp
var filter = new ReplayFilter
{
    FromTimestamp = DateTimeOffset.Parse("2024-01-15T09:00:00Z"),
    ToTimestamp = DateTimeOffset.Parse("2024-01-15T09:30:00Z")
};
// Topic is passed as a separate parameter to the replay store:
// await replayStore.GetMessagesForReplayAsync("eip.orders.enriched", filter, ...);
```

Open `src/Processing.Replay/MessageReplayer.cs` and trace: How does the replayer iterate over stored messages? What happens to messages that don't match the filter?

### Step 2: Analyze the ReplayId Header for Atomicity

The platform injects a `ReplayId` header into replayed messages. Explain why:

1. Without `ReplayId` — downstream consumers process the message as if it's new → **duplicate side effects** (e.g., double billing)
2. With `ReplayId` — consumers can detect replays and apply **idempotent** processing
3. How does `ReplayId` interact with `MessageId`? (the original `MessageId` is preserved for correlation)

Design a consumer that checks for `ReplayId` and skips already-processed messages using a deduplication store.

### Step 3: Evaluate Replay Store Scalability

A production system processes 10 million messages/day. Design the replay store requirements:

| Requirement | Value | Justification |
|------------|-------|---------------|
| Storage per message | ~2KB (envelope) | Full envelope for accurate replay |
| Daily storage | ~20GB | 10M × 2KB |
| Retention period | 30 days | Regulatory and operational needs |
| Total storage | ~600GB | 30 × 20GB |
| Query performance | < 100ms for time-range | Fast incident response |

What storage technology would you recommend? (hint: time-series databases, object storage with indexing)

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial26/Exam.cs`](../tests/TutorialLabs/Tutorial26/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 25 — Dead Letter Queue](25-dead-letter-queue.md)** | **Next: [Tutorial 27 — Resequencer →](27-resequencer.md)**
