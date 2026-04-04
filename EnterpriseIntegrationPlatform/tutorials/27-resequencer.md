# Tutorial 27 — Resequencer

## What You'll Learn

- The EIP Resequencer pattern for restoring message order from out-of-order delivery
- How `IResequencer` and `MessageResequencer` buffer and release messages in sequence
- Grouping by `CorrelationId` with ordering by `SequenceNumber`
- Timeout-based release to prevent indefinite buffering
- The `ActiveSequenceCount` metric for monitoring open sequences
- `ResequencerOptions` for concurrent-sequence limits and release timeout

---

## EIP Pattern: Resequencer

> *"A Resequencer can receive a stream of messages that may not arrive in order and reorder them so that they are published to the output channel in order."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  Msg #3 ──▶ ┌──────────────────┐
  Msg #1 ──▶ │   Resequencer    │──▶ Msg #1, Msg #2, Msg #3
  Msg #2 ──▶ │   (buffer +      │
             │    sort + release)│
             └──────────────────┘
                     │
              Timeout triggers
              partial release
```

Distributed systems deliver messages out of order. The Resequencer collects messages sharing a `CorrelationId`, sorts them by `SequenceNumber`, and releases them in order once the sequence is complete — or when a timeout fires.

---

## Platform Implementation

### IResequencer

```csharp
// src/Processing.Resequencer/IResequencer.cs
public interface IResequencer
{
    IReadOnlyList<IntegrationEnvelope<T>> Accept<T>(IntegrationEnvelope<T> envelope);
    IReadOnlyList<IntegrationEnvelope<T>> ReleaseOnTimeout<T>(Guid correlationId);
    int ActiveSequenceCount { get; }
}
```

`Accept` is synchronous — it accepts a single message and returns zero or more messages ready for release. If the submitted message completes a contiguous run, the entire run is returned. If gaps remain, an empty list is returned and the message is buffered. `ReleaseOnTimeout` forces release of all buffered messages for a given `CorrelationId` when the timeout fires.

### MessageResequencer (concrete)

The `MessageResequencer` class implements `IResequencer`. Internally it maintains a `ConcurrentDictionary<string, SortedList<int, IntegrationEnvelope<string>>>` keyed by `CorrelationId`. Each entry tracks:

1. **Expected next sequence number** — starts at 1
2. **Buffered messages** — sorted by `SequenceNumber`
3. **Last activity timestamp** — for timeout detection

When a message arrives whose `SequenceNumber` equals the expected value, the resequencer releases it and any contiguous successors.

### ResequencerOptions

```csharp
// src/Processing.Resequencer/ResequencerOptions.cs
public sealed class ResequencerOptions
{
    public TimeSpan ReleaseTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentSequences { get; set; } = 10_000;
}
```

| Option | Purpose |
|--------|---------|
| `ReleaseTimeout` | How long to wait for missing sequence numbers before releasing buffered messages (default 30 s) |
| `MaxConcurrentSequences` | Maximum number of distinct sequences tracked concurrently (default 10,000) |

### Timeout-Based Release

A background timer scans active sequences. When `ReleaseTimeout` elapses since the last message arrived for a correlation, the resequencer calls `ReleaseOnTimeout` to release whatever is buffered in sequence order.

---

## Scalability Dimension

The resequencer is **stateful** — it must buffer messages until a sequence is complete. This limits horizontal scaling because all messages for a given `CorrelationId` must reach the **same instance**. Use broker-level partition affinity (partition by `CorrelationId`) to ensure this. `ActiveSequenceCount` exposes the current memory pressure so auto-scalers can react before buffers overflow.

---

## Atomicity Dimension

Messages are **Acked only after successful release** to the downstream topic. If the resequencer crashes, buffered messages are redelivered by the broker (they were never Acked). On restart, the resequencer rebuilds its buffer from redelivered messages. The `ReleaseTimeout` acts as a safety valve — it ensures no message is buffered indefinitely, preventing memory leaks and silent message loss.

---

## Exercises

1. Three messages arrive for `CorrelationId = "order-42"` in this order: #3, #1, #2. Trace the calls to `Accept` and describe the return value for each call.

2. A sequence has messages #1, #2, #4 buffered and `ReleaseTimeout` fires. Describe what `ReleaseOnTimeout` returns and what happens to the gap at #3.

3. Why must all messages for a `CorrelationId` be routed to the same resequencer instance? What broker feature enables this?

---

**Previous: [← Tutorial 26 — Message Replay](26-message-replay.md)** | **Next: [Tutorial 28 — Competing Consumers →](28-competing-consumers.md)**
