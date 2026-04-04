# Tutorial 41 — OpenClaw Web UI

## What You'll Learn

- The purpose of `OpenClaw.Web` — the "Where is my message?" web interface
- Natural language search powered by the RAG API (Tutorial 40)
- Message state inspection from the lifecycle store (Tutorial 39)
- Integration with the RAG knowledge API for contextual answers
- Aspire dashboard integration for monitoring and diagnostics

---

## Architecture: OpenClaw Web

> *"Every integration platform needs a single pane of glass where operators can search for messages, inspect their state, and understand why something went wrong — in plain English."*

```
  ┌──────────────────────────────────────────────────┐
  │                OpenClaw.Web                      │
  │  ┌────────────┐  ┌──────────┐  ┌─────────────┐  │
  │  │ Search Bar │  │ Message  │  │ RAG Chat    │  │
  │  │ (natural   │  │ Detail   │  │ (ask about  │  │
  │  │  language)  │  │ View     │  │  messages)  │  │
  │  └─────┬──────┘  └────┬─────┘  └──────┬──────┘  │
  └────────┼──────────────┼───────────────┼──────────┘
           │              │               │
           ▼              ▼               ▼
  ┌──────────────┐ ┌─────────────┐ ┌─────────────┐
  │ Message      │ │ State Store │ │ RAG API     │
  │ State Store  │ │ (lifecycle) │ │ (AI.RagFlow)│
  └──────────────┘ └─────────────┘ └─────────────┘
           │
           ▼
  ┌──────────────┐
  │ Aspire       │
  │ Dashboard    │
  └──────────────┘
```

OpenClaw.Web is a Blazor Server application that aggregates data from the message state store, the RAG knowledge API, and the Aspire dashboard into a unified operator experience.

---

## Platform Implementation

### Natural Language Search

```csharp
// src/OpenClaw.Web/Services/IMessageSearchService.cs
public interface IMessageSearchService
{
    Task<SearchResults> SearchAsync(
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed record SearchResults(
    IReadOnlyList<MessageSearchHit> Hits,
    int TotalCount,
    TimeSpan SearchDuration);

public sealed record MessageSearchHit(
    string MessageId,
    string CorrelationId,
    string CurrentState,
    string Summary,
    float Relevance);
```

The search service supports both structured queries (`messageId:abc-123`) and natural language (`"order from PartnerX that failed yesterday"`). Natural language queries are processed by the RAG API to find relevant messages.

### Message State Inspection

```csharp
// src/OpenClaw.Web/Services/IMessageInspector.cs
public interface IMessageInspector
{
    Task<MessageDetail?> GetDetailAsync(
        string messageId,
        CancellationToken cancellationToken = default);
}

public sealed record MessageDetail(
    string MessageId,
    string CorrelationId,
    string Source,
    string MessageType,
    string CurrentState,
    IReadOnlyList<MessageEvent> Lifecycle,
    TraceAnalysis? Trace,
    string? DeadLetterReason);
```

The detail view displays the complete message lifecycle (Tutorial 39), trace analysis, and dead letter reason if applicable. Operators can see exactly which pipeline stage processed the message and how long each stage took.

### RAG Knowledge Chat

```csharp
// src/OpenClaw.Web/Services/IRagChatService.cs
public interface IRagChatService
{
    Task<ChatResponse> AskAsync(
        string question,
        string? messageContext = null,
        CancellationToken cancellationToken = default);
}

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<string> SourceDocuments,
    float Confidence);
```

The chat panel lets operators ask questions like:
- *"Why did this message fail?"*
- *"What does error code EIP-4012 mean?"*
- *"How do I replay messages from the last hour?"*

The RAG API grounds answers in platform documentation, message history, and dead letter details.

### Aspire Dashboard Integration

OpenClaw.Web links to the Aspire dashboard for deeper diagnostics: **Traces** (click a message to view its distributed trace from Tutorial 38), **Metrics** (pipeline throughput and error rates), and **Logs** (structured logs from Tutorial 39). The `AspireDashboardOptions` class configures the dashboard URL and URL templates for traces and metrics.

---

## Scalability Dimension

OpenClaw.Web is a **read-only UI** — it queries existing stores and APIs without modifying pipeline state. Blazor Server maintains a SignalR connection per user, so the number of concurrent operators determines resource needs. For large teams, deploy multiple web instances behind a load balancer with sticky sessions. Search performance depends on the message state store's indexing (Tutorial 39).

---

## Atomicity Dimension

The web UI provides **eventual consistency** — it shows the latest state from the store, which may be a few seconds behind real-time processing. The lifecycle view uses `GetByMessageIdAsync` for a consistent snapshot of a single message's history. The RAG chat is advisory — its answers are generated, not authoritative. Operators should verify critical decisions against the raw lifecycle data.

---

## Exercises

1. An operator searches for "failed orders from PartnerX last week." Trace the query through `IMessageSearchService` and the RAG API.

2. Design the UI flow for the "Where is my message?" feature: what inputs does the operator provide, and what data is displayed?

3. Why does OpenClaw.Web embed links to the Aspire dashboard rather than reimplementing trace and metric visualization?

---

**Previous: [← Tutorial 40 — RAG with Ollama](40-rag-ollama.md)** | **Next: [Tutorial 42 — Configuration →](42-configuration.md)**
