# Tutorial 40 — RAG with Ollama

## What You'll Learn

- How `AI.Ollama` provides self-hosted embeddings for the integration platform
- How `AI.RagFlow` implements retrieval-augmented generation over platform documentation and message history
- Aspire container orchestration for Ollama and vector store services
- How developers connect their own AI provider to the RAG API
- Why all data stays on-premises — no external API calls for AI features

---

## Architecture: Self-Hosted RAG

> *"Retrieval-Augmented Generation (RAG) grounds AI responses in your own data. By self-hosting the embedding model and vector store, sensitive integration data never leaves your infrastructure."*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Developer   │────▶│  RAG API         │────▶│  Vector      │
  │  (question)  │     │  (AI.RagFlow)    │     │  Store       │
  └──────────────┘     └────────┬─────────┘     └──────────────┘
                                │
                       ┌────────▼─────────┐
                       │  Ollama          │
                       │  (embeddings +   │
                       │   generation)    │
                       └──────────────────┘
                                │
                       ┌────────▼─────────┐
                       │  Aspire          │
                       │  (orchestration) │
                       └──────────────────┘
```

The RAG pipeline: (1) embed the question, (2) retrieve relevant documents from the vector store, (3) pass the question + retrieved context to the language model, (4) return a grounded answer.

---

## Platform Implementation

### AI.Ollama — Self-Hosted LLM Service

```csharp
// src/AI.Ollama/IOllamaService.cs
public interface IOllamaService
{
    Task<string> GenerateAsync(
        string prompt,
        string model = "llama3.2",
        CancellationToken cancellationToken = default);

    Task<string> AnalyseAsync(
        string systemPrompt,
        string context,
        string model = "llama3.2",
        CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

// src/AI.Ollama/OllamaSettings.cs
public sealed class OllamaSettings
{
    public string Model { get; set; } = "llama3.2";
}
```

Ollama runs as a container managed by Aspire, serving generation endpoints over a local HTTP API. `GenerateAsync` sends a prompt and returns the generated text. `AnalyseAsync` accepts a system prompt plus structured context (e.g. JSON trace data) for diagnostic analysis. `OllamaSettings` is bound from the `Ollama` configuration section.

### AI.RagFlow — Retrieval-Augmented Generation

```csharp
// src/AI.RagFlow/IRagFlowService.cs
public interface IRagFlowService
{
    Task<string> RetrieveAsync(
        string query,
        IReadOnlyList<string>? datasetIds = null,
        CancellationToken cancellationToken = default);

    Task<RagFlowChatResponse> ChatAsync(
        string question,
        string? conversationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RagFlowDataset>> ListDatasetsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

// Response records (defined in IRagFlowService.cs)
public sealed record RagFlowChatResponse(
    string Answer,
    string? ConversationId,
    IReadOnlyList<RagFlowReference> References);

public sealed record RagFlowReference(
    string Content,
    string? DocumentName,
    double Score);

public sealed record RagFlowDataset(
    string Id,
    string Name,
    int DocumentCount);
```

`RetrieveAsync` returns relevant context chunks as a plain string. `ChatAsync` combines retrieval and LLM generation in a single call, returning a `RagFlowChatResponse` with the answer, a conversation ID for multi-turn chat, and source references.

### RagFlowOptions

```csharp
// src/AI.RagFlow/RagFlowOptions.cs
public sealed class RagFlowOptions
{
    public const string SectionName = "RagFlow";

    public string BaseAddress { get; set; } = "http://localhost:15380";
    public string? ApiKey { get; set; }
    public string? AssistantId { get; set; }
}
```

| Option | Purpose |
|--------|---------|
| `BaseAddress` | RagFlow API base URL |
| `ApiKey` | API key for authentication |
| `AssistantId` | RagFlow assistant ID for chat completion |

### Aspire Orchestration

The platform uses .NET Aspire to manage the Ollama container and the RagFlow service. Services are registered via extension methods (`AddOllamaService`, `AddRagFlowService`) that read base addresses and configuration from Aspire's environment variables or `appsettings.json`.

---

## Scalability Dimension

Ollama runs on GPU-enabled nodes for fast inference. The RagFlow service manages chunking, indexing, and retrieval internally. `RetrieveAsync` supports scoping queries by dataset IDs to reduce search space. The RAG API is stateless and replicable behind a load balancer.

---

## Atomicity Dimension

RAG is a **read-only, advisory feature** — it does not modify messages or pipeline state. Index updates are eventually consistent. The `RagFlowReference.Score` helps consumers assess source relevance. All data stays on-premises — no message content is sent to external AI providers unless the developer explicitly configures a cloud provider.

---

## Lab

**Objective:** Design a RAG query flow for operational troubleshooting, analyze graceful degradation when AI infrastructure is unavailable, and evaluate self-hosted vs. cloud AI for **scalable** integration platform operations.

### Step 1: Design a RAG Troubleshooting Flow

A developer asks: "Why did order 12345 fail?" Design the complete RAG flow:

```
1. EMBED query → vector representation
2. RETRIEVE relevant context:
   - DLQ entry for order 12345 (error details, reason)
   - Lifecycle events (which stage failed)
   - Recent similar failures (pattern detection)
3. GENERATE response using LLM with retrieved context
4. RETURN: "Order 12345 failed at the Transform stage due to missing 'currency' field.
   This is a recurring issue — 15 similar failures in the last hour from PartnerX.
   Recommended action: check PartnerX's schema version."
```

Open `src/AI.RagFlow/`, `src/AI.Ollama/`, and `src/AI.RagKnowledge/` and trace: How does the platform embed and retrieve context? What data sources are indexed?

### Step 2: Design Graceful Degradation

The Ollama container runs out of GPU memory. Design the degradation strategy:

| Component | Normal Mode | Degraded Mode |
|-----------|------------|---------------|
| RAG API | Full LLM responses | Return raw retrieved context without AI summary |
| Chat interface | AI-powered answers | "AI unavailable — showing raw data" |
| Message processing | Unaffected | Unaffected (AI is never in the critical path) |

Why must the RAG/AI system **never** be in the critical message processing path? How does this architectural decision support **pipeline atomicity**?

### Step 3: Evaluate Self-Hosted vs. Cloud AI

| Factor | Self-Hosted (Ollama) | Cloud (OpenAI/Azure) |
|--------|---------------------|---------------------|
| Data privacy | Payloads never leave your infrastructure | Data sent to external API |
| Latency | Local network (~50ms) | Internet round-trip (~500ms) |
| Cost at scale | Fixed (GPU hardware) | Variable (per-token pricing) |
| Availability | You manage uptime | Provider manages uptime |

Why does the platform default to self-hosted Ollama? Consider: enterprise integration platforms process sensitive business data from multiple tenants.

## Exam

1. Why must the RAG/AI system never be in the critical message processing path?
   - A) AI responses are too slow for real-time processing
   - B) AI infrastructure failures must not impact message processing — the integration platform's primary responsibility is atomic message delivery, and coupling it to AI availability would make GPU outages cascade into integration failures
   - C) AI models cannot process binary data
   - D) The broker doesn't support AI integration

2. Why does the platform default to self-hosted Ollama rather than a cloud AI provider?
   - A) Ollama is faster than cloud providers
   - B) Enterprise integration platforms process sensitive business data from multiple tenants — self-hosting ensures payload data never leaves the organization's infrastructure, meeting data residency and privacy requirements
   - C) Cloud AI providers don't support .NET
   - D) Self-hosting is always cheaper

3. How does RAG improve **operational scalability** for a large integration platform?
   - A) RAG processes messages faster
   - B) RAG enables natural-language troubleshooting across millions of messages — operators can ask "why did this fail?" instead of manually searching DLQ entries, lifecycle events, and logs, dramatically reducing mean-time-to-resolution
   - C) RAG reduces the number of integration patterns needed
   - D) RAG automatically fixes failed messages

---

**Previous: [← Tutorial 39 — Message Lifecycle](39-message-lifecycle.md)** | **Next: [Tutorial 41 — OpenClaw Web UI →](41-openclaw-web.md)**
