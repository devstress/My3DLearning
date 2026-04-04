# Tutorial 26 — Message Replay

## What You'll Learn

- How `IMessageReplayer` enables selective re-processing of historical messages
- How `IMessageReplayStore` persists replay-eligible messages for audit and reprocessing
- The `ReplayFilter` value object for targeting messages by DateRange, CorrelationId, MessageType, or Source
- The `ReplayResult` record with replayed count and status
- The `ReplayId` header added to every replayed message for audit-trail separation
- `InMemoryMessageReplayStore` for development and testing

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
    Task<ReplayResult> ReplayAsync(
        ReplayFilter filter,
        CancellationToken cancellationToken = default);
}
```

### IMessageReplayStore

```csharp
// src/Processing.Replay/IMessageReplayStore.cs
public interface IMessageReplayStore
{
    Task StoreAsync(IntegrationEnvelope<string> envelope, CancellationToken ct);
    Task<IReadOnlyList<IntegrationEnvelope<string>>> QueryAsync(ReplayFilter filter, CancellationToken ct);
}
```

### ReplayFilter

```csharp
// src/Processing.Replay/ReplayFilter.cs
public sealed record ReplayFilter
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? CorrelationId { get; init; }
    public string? MessageType { get; init; }
    public string? Source { get; init; }
}
```

| Filter Property | Usage |
|-----------------|-------|
| `From` / `To` | Date-range replay — e.g. replay all messages from the last hour |
| `CorrelationId` | Replay a single business transaction |
| `MessageType` | Replay all messages of a specific type after a schema fix |
| `Source` | Replay everything from a specific external system |

### ReplayResult

```csharp
// src/Processing.Replay/ReplayResult.cs
public sealed record ReplayResult(
    int ReplayedCount,
    string ReplayId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);
```

Every replayed message receives a `ReplayId` header (a GUID) linking it back to the replay operation. This separates replayed traffic from live traffic in dashboards and audit logs.

### InMemoryMessageReplayStore

The `InMemoryMessageReplayStore` implements `IMessageReplayStore` using a `ConcurrentBag<IntegrationEnvelope<string>>`. It supports all `ReplayFilter` predicates and is intended for development, testing, and tutorials. Production deployments swap in a durable store backed by a database or event log.

---

## Scalability Dimension

The replay store is **read-heavy** — writes happen once per message, but replays can query millions of records. Production stores should support indexed queries on `CorrelationId`, `MessageType`, `Source`, and `CreatedAt`. The replayer itself is stateless: it reads from the store, publishes to the broker, and records the `ReplayResult`. Multiple replay operations can run concurrently because each gets a unique `ReplayId`.

---

## Atomicity Dimension

Replay re-publishes messages to the **same ingress topic** they originally entered. This means all validation, routing, and transformation rules apply again — the message is not injected halfway through the pipeline. If a replay fails mid-batch, the `ReplayResult.ReplayedCount` reflects how many were successfully published. The `ReplayId` header ensures idempotent consumers can detect and deduplicate replayed messages.

---

## Exercises

1. An operator discovers a bug in the content enricher that corrupted messages between 09:00 and 09:30 UTC. Write the `ReplayFilter` to target only those messages.

2. Why does the platform inject a `ReplayId` header instead of simply re-publishing the original message unchanged? What problems could occur without it?

3. Describe how `InMemoryMessageReplayStore` would need to change for a production deployment handling 10 million messages per day.

---

**Previous: [← Tutorial 25 — Dead Letter Queue](25-dead-letter-queue.md)** | **Next: [Tutorial 27 — Resequencer →](27-resequencer.md)**
