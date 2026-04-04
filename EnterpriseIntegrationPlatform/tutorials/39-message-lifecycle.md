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

## Exercises

1. A message was received 30 minutes ago but never reached "Delivered" status. Use `ITraceAnalyzer.WhereIsMessageAsync` to identify which stage it is stuck in.

2. Design a retention policy for the message state store that keeps 7 days of detailed events and 90 days of summary events.

3. Why does the platform record lifecycle events separately from OpenTelemetry traces? What does each system provide that the other does not?

---

**Previous: [← Tutorial 38 — OpenTelemetry](38-opentelemetry.md)** | **Next: [Tutorial 40 — RAG with Ollama →](40-rag-ollama.md)**
