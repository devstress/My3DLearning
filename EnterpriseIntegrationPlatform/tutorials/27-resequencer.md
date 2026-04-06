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

The `MessageResequencer` class implements `IResequencer`. Internally it maintains a `ConcurrentDictionary<Guid, SequenceBuffer>` keyed by `CorrelationId`. `SequenceBuffer` is a private inner class that uses a `ConcurrentDictionary<int, object>` for thread-safe storage and `OrderBy` for sequenced release. Each entry tracks:

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

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial27/Lab.cs`](../tests/TutorialLabs/Tutorial27/Lab.cs)

**Objective:** Trace the Resequencer's buffering and release logic, analyze ordering guarantees for **atomic** batch processing, and design for partition-aware scaling.

### Step 1: Trace Out-of-Order Arrival

Three messages arrive for `CorrelationId = "order-42"` in this order: #3, #1, #2. Open `src/Processing.Resequencer/` and trace each `Accept` call:

| Arrival | SequenceNumber | Buffered? | Released? | Why? |
|---------|---------------|-----------|-----------|------|
| 1st | 3 | Yes | No | Waiting for #1 |
| 2nd | 1 | — | Released: #1 | Next expected |
| 3rd | 2 | — | Released: #2, then #3 | Completes the sequence |

Verify your trace against the actual implementation.

### Step 2: Handle Gaps with Timeout

A sequence has messages #1, #2, #4 buffered, but #3 never arrives. After `ReleaseTimeout` fires:

1. What does `ReleaseOnTimeout` return? (hint: #1 and #2 are released, #4 is released with a gap marker)
2. Is the gap reported for downstream awareness?
3. How does this design prevent indefinite buffering — critical for **system scalability** under high message volumes?

Design an alerting strategy for gap detection: when should the operations team be notified?

### Step 3: Partition-Aware Resequencing

All messages for a `CorrelationId` must be routed to the same resequencer instance. Explain:

- What broker feature enables this? (hint: Kafka partition keys, NATS subject-based routing)
- What happens if messages for the same `CorrelationId` land on different resequencer instances?
- How does partition-key routing enable **horizontal scaling** — each instance handles a subset of `CorrelationId`s independently?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial27/Exam.cs`](../tests/TutorialLabs/Tutorial27/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 26 — Message Replay](26-message-replay.md)** | **Next: [Tutorial 28 — Competing Consumers →](28-competing-consumers.md)**
