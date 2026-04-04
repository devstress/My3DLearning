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

## Exercises

1. A developer asks "Why did order 12345 fail?" Design the RAG flow: what gets embedded, what is retrieved, and what context is passed to the model.

2. The Ollama container runs out of GPU memory. What happens to the RAG API? How should the platform degrade gracefully?

3. Why does the platform default to self-hosted Ollama rather than a cloud AI provider? What trade-offs are involved?

---

**Previous: [← Tutorial 39 — Message Lifecycle](39-message-lifecycle.md)** | **Next: [Tutorial 41 — OpenClaw Web UI →](41-openclaw-web.md)**
