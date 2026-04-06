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

### DemoDataSeeder

OpenClaw.Web uses a `DemoDataSeeder` (a `BackgroundService`) to populate the `IObservabilityEventLog` with sample message lifecycle events so operators can test "where is my message?" queries before the Kafka ingestion pipeline is running. It seeds three scenarios:

- **order-02** — Successfully delivered (`Pending → InFlight → Delivered`)
- **shipment-123** — Currently in-flight (`Pending → InFlight`)
- **invoice-001** — Failed with retry (`Pending → InFlight → Failed → Retrying`)

The `/api/health/seeder` endpoint exposes `DemoDataSeeder.IsSeeded` so Playwright tests can poll for readiness.

### Service Registration (Program.cs)

The web app registers services directly through DI — there are no `IMessageSearchService`, `IMessageInspector`, or `IRagChatService` abstractions. Instead, `Program.cs` wires up the concrete services:

```csharp
// Register Ollama AI service
builder.Services.AddOllamaService(ollamaBaseAddress, ollamaModel);

// Register platform observability (Loki-backed IObservabilityEventLog)
builder.Services.AddPlatformObservability(lokiBaseAddress);

// Register RagFlow RAG service
builder.Services.AddRagFlowService(builder.Configuration);

// Seed demo data
builder.Services.AddHostedService<DemoDataSeeder>();
```

### API Endpoints

The `/api/inspect` group provides message search and inspection via `MessageStateInspector`, which queries the `IObservabilityEventLog` and calls `IOllamaService` for AI-powered trace analysis:

```csharp
// Query by business key (e.g. "order-02")
GET /api/inspect/business/{businessKey}

// Query by correlation ID
GET /api/inspect/correlation/{correlationId:guid}

// Free-form natural language query
POST /api/inspect/ask  { "query": "..." }
```

The `/api/generate` group provides RAG-powered context retrieval via `IRagFlowService`:

```csharp
// Retrieve context for integration generation
POST /api/generate/integration

// Chat completion (retrieval + generation)
POST /api/generate/chat

// List available RagFlow datasets
GET  /api/generate/datasets
```

Health endpoints check `IOllamaService.IsHealthyAsync()` and `IRagFlowService.IsHealthyAsync()`.

### Embedded Web UI

OpenClaw.Web serves a single-page Blazor Server UI at `/` with:
- A search box for business key or natural-language queries
- A timeline visualization of message lifecycle events with status badges (`Pending`, `InFlight`, `Delivered`, `Failed`, `Retrying`, `DeadLettered`)
- Ollama health status indicator
- Links to the Aspire dashboard for deeper diagnostics

---

## Scalability Dimension

OpenClaw.Web is a **read-only UI** — it queries the `IObservabilityEventLog` and AI services without modifying pipeline state. Blazor Server maintains a SignalR connection per user, so the number of concurrent operators determines resource needs. For large teams, deploy multiple web instances behind a load balancer with sticky sessions. Search performance depends on the observability event log's indexing (Tutorial 39).

---

## Atomicity Dimension

The web UI provides **eventual consistency** — it shows the latest state from the observability event log, which may be a few seconds behind real-time processing. The lifecycle view uses `IObservabilityEventLog.GetByBusinessKeyAsync` for a consistent snapshot of a single message's history. The AI-powered trace analysis is advisory — its answers are generated by `IOllamaService`, not authoritative. Operators should verify critical decisions against the raw lifecycle data.

---

## Lab

**Objective:** Trace the operational query flow through OpenClaw's inspection APIs, design a "Where is my message?" workflow, and analyze why the UI delegates to Aspire for **scalable** observability.

### Step 1: Trace an Operational Query

An operator searches for "failed orders from PartnerX last week." Trace the query flow:

```
1. Operator enters query in OpenClaw chat → POST /api/inspect/ask
2. MessageStateInspector parses: source="PartnerX", status="Failed", timeRange=7d
3. Query observability event log for matching messages
4. Return: list of failed messages with failure reasons, stages, and timestamps
```

Open `src/OpenClaw.Web/` and trace: How does the `/api/inspect/ask` endpoint delegate to `MessageStateInspector`? What data sources does it query?

### Step 2: Design the "Where Is My Message?" Feature

Design the complete UI flow:

| Input | Source | Purpose |
|-------|--------|---------|
| Message ID or Correlation ID | Operator | Identify the message |
| (optional) Time range | Operator | Narrow the search |

| Output Display | Data Source |
|---------------|-------------|
| Current lifecycle stage | Message state store |
| Processing timeline | Lifecycle events |
| Error details (if failed) | DLQ entry |
| Distributed trace link | OpenTelemetry trace ID |

Why does the platform show a **link** to the Aspire dashboard trace rather than embedding trace visualization directly? (hint: Aspire already provides rich trace visualization — reimplementing it would be a maintenance burden)

### Step 3: Analyze UI Architecture for Operational Scalability

The OpenClaw Web UI queries data directly from multiple backend services (Loki, Ollama, RagFlow). Design the resilience strategy:

| Scenario | Behavior |
|----------|----------|
| All services healthy | Full functionality — lifecycle search, AI analysis, RAG knowledge |
| Loki (observability store) down | In-memory event log fallback; DemoDataSeeder data still available |
| Ollama unreachable | AI-powered trace analysis disabled; basic search still works |
| RagFlow unreachable | RAG knowledge queries unavailable; inspection endpoints unaffected |

How does this resilience architecture support **operational scalability** — the UI must remain useful even during partial infrastructure failures?

## Exam

1. Why does OpenClaw embed links to the Aspire dashboard rather than reimplementing trace visualization?
   - A) Aspire's visualization is faster
   - B) Aspire already provides rich distributed trace, metrics, and log visualization — reimplementing this in OpenClaw would duplicate functionality, increase maintenance burden, and diverge from the platform's standard observability stack
   - C) The Aspire dashboard is required by .NET
   - D) OpenClaw cannot display visual data

2. How does the multi-source resilience pattern in OpenClaw support **operational scalability**?
   - A) It makes the UI faster
   - B) When backend services are degraded, the UI shows graceful fallbacks rather than crashing — operators can still search messages and access partial functionality, maintaining operational capability during infrastructure incidents
   - C) Querying multiple sources reduces network traffic
   - D) The broker provides resilience automatically

3. Why does the "Where is my message?" feature query multiple data sources?
   - A) One data source is always sufficient
   - B) No single system contains the complete picture — the lifecycle store tracks stage transitions, the DLQ contains failure details, and OpenTelemetry provides timing; combining them gives operators a complete and **actionable** view of any message's journey
   - C) Multiple queries improve response time
   - D) Each data source requires a separate API call

---

**Previous: [← Tutorial 40 — RAG with Ollama](40-rag-ollama.md)** | **Next: [Tutorial 42 — Configuration →](42-configuration.md)**
