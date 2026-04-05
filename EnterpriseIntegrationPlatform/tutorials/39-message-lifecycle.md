# Tutorial 39 — Message Lifecycle

## What You'll Learn

- How `IMessageStateStore` tracks every state transition of a message
- The `MessageEvent` record for capturing lifecycle events
- `MessageLifecycleRecorder` for recording transitions as messages flow through the pipeline
- `InMemoryMessageStateStore` for development and testing
- Querying lifecycle by `CorrelationId`, `BusinessKey`, or `MessageId`
- `ITraceAnalyzer` and `TraceAnalyzer` for diagnosing slow or stuck messages
- `IObservabilityEventLog` and `LokiObservabilityEventLog` for centralized event logging

---

## EIP Pattern: Message History

> *"Attach a Message History to a message to record all components it has passed through. The lifecycle store extends this concept to persist the full state machine of each message."*

```
  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
  │ Pending  │──▶│ InFlight │──▶│Delivered │   │  Failed  │
  └──────────┘   └──────────┘   └──────────┘   └──────────┘
       │              │              │              │
       ▼              ▼              ▼              ▼
  ┌──────────────────────────────────────────────────────┐
  │              Message State Store                     │
  │  (queryable by CorrelationId / BusinessKey / MsgId)  │
  └──────────────────────────────────────────────────────┘
```

`DeliveryStatus` enum: `Pending`, `InFlight`, `Delivered`, `Failed`, `Retrying`, `DeadLettered`.

Every pipeline stage records a `MessageEvent` when a message transitions between states. The full lifecycle is queryable for operational visibility, debugging, and compliance.

---

## Platform Implementation

### MessageEvent

```csharp
// src/Observability/MessageEvent.cs
public sealed record MessageEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public required Guid MessageId { get; init; }
    public required Guid CorrelationId { get; init; }
    public required string MessageType { get; init; }
    public required string Source { get; init; }
    public required string Stage { get; init; }
    public required DeliveryStatus Status { get; init; }
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? Details { get; init; }
    public string? BusinessKey { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
```

IDs are `Guid`, not `string`. `Stage` names the processing step (e.g. "Ingestion", "Routing", "Delivery"). `Status` is a `DeliveryStatus` enum value (`Pending`, `InFlight`, `Delivered`, `Failed`, `Retrying`, `DeadLettered`). `TraceId` and `SpanId` link each event to the corresponding OpenTelemetry span (Tutorial 38).

### IMessageStateStore

```csharp
// src/Observability/IMessageStateStore.cs
public interface IMessageStateStore
{
    Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByMessageIdAsync(
        Guid messageId, CancellationToken cancellationToken = default);

    Task<MessageEvent?> GetLatestByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);
}
```

All ID parameters are `Guid`. `GetLatestByCorrelationIdAsync` returns the most recent event, representing the current known state of a message.

### MessageLifecycleRecorder

The `MessageLifecycleRecorder` records events to **both** the production `IMessageStateStore` and the isolated `IObservabilityEventLog`, and emits OpenTelemetry traces and metrics. It provides convenience methods — `RecordReceivedAsync`, `RecordProcessingAsync`, `RecordDeliveredAsync`, `RecordFailedAsync`, `RecordRetryAsync`, `RecordDeadLetteredAsync` — that create `MessageEvent` records with the correct `Stage` and `DeliveryStatus` values. Each method captures the envelope's identity, the pipeline stage name, and optional details (e.g. delivery duration, dead-letter reason).

### ITraceAnalyzer and TraceAnalyzer

```csharp
// src/Observability/ITraceAnalyzer.cs
public interface ITraceAnalyzer
{
    Task<string> AnalyseTraceAsync(
        string traceContextJson, CancellationToken cancellationToken = default);

    Task<string> WhereIsMessageAsync(
        Guid correlationId, string knownState, CancellationToken cancellationToken = default);
}
```

Both methods return AI-generated natural-language strings (not structured records). `AnalyseTraceAsync` accepts a JSON trace snapshot and returns a diagnostic summary. `WhereIsMessageAsync` answers "where is my message?" given a correlation ID and known state payload. The `TraceAnalyzer` implementation delegates to `IOllamaService` (Tutorial 40) for LLM-powered analysis.

### IObservabilityEventLog and LokiObservabilityEventLog

```csharp
// src/Observability/IObservabilityEventLog.cs
public interface IObservabilityEventLog
{
    Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);
}
```

`IObservabilityEventLog` is **separate** from `IMessageStateStore` — it provides isolated observability storage backed by Grafana Loki. The method is `RecordAsync` (not `WriteAsync`). `GetByCorrelationIdAsync` accepts a `Guid`. Combined with OpenTelemetry traces (Tutorial 38), this provides full observability across the platform.

---

## Scalability Dimension

The state store is **write-heavy** — every pipeline stage writes an event per message. Production deployments should use a time-series or append-optimized store (Loki, ClickHouse). `InMemoryMessageStateStore` uses a `ConcurrentDictionary<Guid, List<MessageEvent>>` with secondary indexes on `CorrelationId` and `BusinessKey`.

---

## Atomicity Dimension

Lifecycle recording is a **best-effort side effect** — it must not block or fail the main pipeline. If the state store is unavailable, the message continues processing and the gap is logged. However, the `RecordDeadLetteredAsync` event is critical for audit compliance and should use a local fallback buffer when the store is unreachable.

---

## Lab

**Objective:** Use the message lifecycle tracking system to diagnose stuck messages, design retention policies for **scalable** storage, and compare lifecycle tracking with OpenTelemetry tracing.

### Step 1: Diagnose a Stuck Message

A message was received 30 minutes ago but never reached "Delivered" status. Use `ITraceAnalyzer.WhereIsMessageAsync` to investigate:

```csharp
var location = await traceAnalyzer.WhereIsMessageAsync(messageId);
// Returns: { Stage: "Transform", Status: "InProgress", SinceUtc: "30 min ago" }
```

Open `src/Observability/TraceAnalyzer.cs` and trace: How does the analyzer query the message state store? What lifecycle states are tracked (Received, Routing, Transforming, Delivering, Delivered, Failed)?

Design an alerting rule: any message in "InProgress" for > 5 minutes should trigger an alert. How does this support **operational scalability**?

### Step 2: Design a Retention Policy

Design a retention strategy for the message state store handling 10 million messages/day:

| Retention Tier | Data | Duration | Storage |
|---------------|------|----------|---------|
| Hot (detailed) | All lifecycle events, full envelope | 7 days | ~140GB |
| Warm (summary) | Stage transitions, message ID, status | 90 days | ~27GB |
| Cold (archive) | Message ID, final status, timestamp | 1 year | ~3.6GB |

How does tiered retention balance **operational visibility** with **storage scalability**?

### Step 3: Compare Lifecycle Tracking vs. OpenTelemetry

| Aspect | Message Lifecycle | OpenTelemetry Tracing |
|--------|------------------|----------------------|
| Purpose | Business-level message tracking | Technical span timing |
| Query model | "Where is message X?" | "Show me the trace for request Y" |
| Retention | Days to months | Hours to days |
| Audience | Operations team | Developers |

Why does the platform maintain both systems? What does each provide that the other cannot?

## Exam

1. A message is stuck in "Transforming" state for 15 minutes. What does this indicate?
   - A) The message was successfully delivered
   - B) The transformation activity is either blocked (deadlock, external dependency), has failed without updating state, or the worker processing it has crashed — the lifecycle tracking enables targeted investigation of the exact stuck stage
   - C) The message was routed to the DLQ
   - D) The lifecycle store has a bug

2. Why does the platform record lifecycle events separately from OpenTelemetry traces?
   - A) They serve the same purpose
   - B) Lifecycle tracking provides business-level "where is my message?" visibility with longer retention; OpenTelemetry provides technical performance metrics with shorter retention — together they serve both operators and developers
   - C) OpenTelemetry cannot track message state
   - D) Lifecycle events are faster to query

3. How does tiered retention support **storage scalability** for lifecycle data?
   - A) All data is kept forever at full detail
   - B) Recent data is kept at full detail for debugging; older data is summarized to reduce storage — this balances operational needs (recent incidents require full detail) with cost (years of data at full detail would be prohibitively expensive)
   - C) Retention policies are only needed for compliance
   - D) The message broker handles retention automatically

---

**Previous: [← Tutorial 38 — OpenTelemetry](38-opentelemetry.md)** | **Next: [Tutorial 40 — RAG with Ollama →](40-rag-ollama.md)**
