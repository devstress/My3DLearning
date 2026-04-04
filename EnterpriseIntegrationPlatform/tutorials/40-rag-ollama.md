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

### AI.Ollama — Embedding Provider

```csharp
// src/AI.Ollama/IOllamaEmbeddingProvider.cs
public interface IOllamaEmbeddingProvider
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct);
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct);
}

// src/AI.Ollama/OllamaOptions.cs
public sealed class OllamaOptions
{
    public required Uri Endpoint { get; init; }
    public string EmbeddingModel { get; init; } = "nomic-embed-text";
    public string GenerationModel { get; init; } = "llama3";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
}
```

Ollama runs as a container managed by Aspire, serving both embedding and generation endpoints over a local HTTP API.

### AI.RagFlow — Retrieval Pipeline

```csharp
// src/AI.RagFlow/IRagPipeline.cs
public interface IRagPipeline
{
    Task<RagResponse> AskAsync(
        string question,
        RagOptions? options = null,
        CancellationToken cancellationToken = default);

    Task IndexDocumentAsync(
        string documentId,
        string content,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}

// src/AI.RagFlow/RagResponse.cs
public sealed record RagResponse(
    string Answer,
    IReadOnlyList<RetrievedDocument> Sources,
    float Confidence);

public sealed record RetrievedDocument(
    string DocumentId,
    string Excerpt,
    float Score,
    IDictionary<string, string>? Metadata);
```

### RagOptions

```csharp
// src/AI.RagFlow/RagOptions.cs
public sealed class RagOptions
{
    public int TopK { get; init; } = 5;
    public float MinScore { get; init; } = 0.7f;
    public string? TenantFilter { get; init; }
    public bool IncludeMessageHistory { get; init; } = true;
}
```

| Option | Purpose |
|--------|---------|
| `TopK` | Number of documents to retrieve |
| `MinScore` | Minimum similarity score threshold |
| `TenantFilter` | Restrict results to a tenant |
| `IncludeMessageHistory` | Include lifecycle data in corpus |

### Aspire & Developer AI Provider

The platform uses .NET Aspire to manage the Ollama container and vector store (Qdrant). Developers can connect their own AI provider by implementing `IGenerationProvider`:

```csharp
// src/AI.RagFlow/IGenerationProvider.cs
public interface IGenerationProvider
{
    Task<string> GenerateAsync(string prompt, string context, CancellationToken ct);
}
```

---

## Scalability Dimension

Ollama runs on GPU-enabled nodes for fast inference. The vector store (Qdrant) scales horizontally with sharding. Embedding generation is the bottleneck — batch `GenerateEmbeddingsAsync` reduces round-trips. The RAG API is stateless and replicable behind a load balancer.

---

## Atomicity Dimension

RAG is a **read-only, advisory feature** — it does not modify messages or pipeline state. Index updates are eventually consistent. The `Confidence` score helps consumers assess answer reliability. All data stays on-premises — no message content is sent to external AI providers unless the developer explicitly configures a cloud `IGenerationProvider`.

---

## Exercises

1. A developer asks "Why did order 12345 fail?" Design the RAG flow: what gets embedded, what is retrieved, and what context is passed to the model.

2. The Ollama container runs out of GPU memory. What happens to the RAG API? How should the platform degrade gracefully?

3. Why does the platform default to self-hosted Ollama rather than a cloud AI provider? What trade-offs are involved?

---

**Previous: [← Tutorial 39 — Message Lifecycle](39-message-lifecycle.md)** | **Next: [Tutorial 41 — OpenClaw Web UI →](41-openclaw-web.md)**
