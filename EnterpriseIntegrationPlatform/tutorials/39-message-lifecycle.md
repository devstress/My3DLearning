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
  │ Received │──▶│ Routed   │──▶│Delivered │──▶│  Acked   │
  └──────────┘   └──────────┘   └──────────┘   └──────────┘
       │              │              │              │
       ▼              ▼              ▼              ▼
  ┌──────────────────────────────────────────────────────┐
  │              Message State Store                     │
  │  (queryable by CorrelationId / BusinessKey / MsgId)  │
  └──────────────────────────────────────────────────────┘
```

Every pipeline stage records a `MessageEvent` when a message transitions between states. The full lifecycle is queryable for operational visibility, debugging, and compliance.

---

## Platform Implementation

### MessageEvent

```csharp
// src/Observability/MessageEvent.cs
public sealed record MessageEvent
{
    public required string MessageId { get; init; }
    public required string CorrelationId { get; init; }
    public string? BusinessKey { get; init; }
    public required string State { get; init; }       // Received, Routed, Transformed, Delivered, Acked, Nacked, DeadLettered
    public required string Component { get; init; }   // e.g. "Router", "HttpConnector"
    public required DateTimeOffset Timestamp { get; init; }
    public IDictionary<string, string>? Details { get; init; }
}
```

### IMessageStateStore

```csharp
// src/Observability/IMessageStateStore.cs
public interface IMessageStateStore
{
    Task RecordAsync(MessageEvent messageEvent, CancellationToken ct);

    Task<IReadOnlyList<MessageEvent>> GetByMessageIdAsync(
        string messageId, CancellationToken ct);

    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        string correlationId, CancellationToken ct);

    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey, CancellationToken ct);
}
```

### MessageLifecycleRecorder

The `MessageLifecycleRecorder` provides convenience methods — `RecordReceivedAsync`, `RecordRoutedAsync`, `RecordDeliveredAsync`, `RecordDeadLetteredAsync` — that create `MessageEvent` records and persist them via `IMessageStateStore`. Each method captures the envelope's identity, the pipeline component name, and optional details (e.g. routing destination, dead-letter reason).

### ITraceAnalyzer and TraceAnalyzer

```csharp
// src/Observability/ITraceAnalyzer.cs
public interface ITraceAnalyzer
{
    Task<TraceAnalysis> AnalyzeAsync(string messageId, CancellationToken ct);
}

public sealed record TraceAnalysis(
    string MessageId,
    TimeSpan TotalDuration,
    string CurrentState,
    IReadOnlyList<StageTimings> Stages,
    IReadOnlyList<string> Anomalies);

public sealed record StageTimings(
    string Component,
    string State,
    DateTimeOffset Timestamp,
    TimeSpan? DurationSinceLastStage);
```

The `TraceAnalyzer` loads all events for a message, calculates inter-stage durations, and flags anomalies (e.g. a stage taking >10× the average, messages stuck in a state for too long).

### IObservabilityEventLog and LokiObservabilityEventLog

```csharp
// src/Observability/IObservabilityEventLog.cs
public interface IObservabilityEventLog
{
    Task WriteAsync(MessageEvent messageEvent, CancellationToken ct);
    Task<IReadOnlyList<MessageEvent>> QueryAsync(string query, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}
```

`LokiObservabilityEventLog` pushes events to Grafana Loki for centralized, searchable log aggregation. Combined with OpenTelemetry traces (Tutorial 38), this provides full observability across the platform.

---

## Scalability Dimension

The state store is **write-heavy** — every pipeline stage writes an event per message. Production deployments should use a time-series or append-optimized store (Loki, ClickHouse). `InMemoryMessageStateStore` uses a `ConcurrentDictionary<string, List<MessageEvent>>` with secondary indexes on `CorrelationId` and `BusinessKey`.

---

## Atomicity Dimension

Lifecycle recording is a **best-effort side effect** — it must not block or fail the main pipeline. If the state store is unavailable, the message continues processing and the gap is logged. However, the `RecordDeadLetteredAsync` event is critical for audit compliance and should use a local fallback buffer when the store is unreachable.

---

## Exercises

1. A message was received 30 minutes ago but never reached "Delivered" state. Use the `ITraceAnalyzer` to identify which stage it is stuck in.

2. Design a retention policy for the message state store that keeps 7 days of detailed events and 90 days of summary events.

3. Why does the platform record lifecycle events separately from OpenTelemetry traces? What does each system provide that the other does not?

---

**Previous: [← Tutorial 38 — OpenTelemetry](38-opentelemetry.md)** | **Next: [Tutorial 40 — RAG with Ollama →](40-rag-ollama.md)**
