# Tutorial 40 — RAG with Ollama

Query platform knowledge using Retrieval-Augmented Generation with Ollama embeddings.

## Learning Objectives

After completing this tutorial you will be able to:

1. Generate text completions with `OllamaClient.GenerateAsync`
2. Run a RAG chat flow through `RagFlowClient.ChatAsync`
3. Understand default settings for `OllamaSettings` and `RagFlowOptions`
4. Inspect the `RagFlowChatResponse` record shape
5. Wire an AI-enriched pipeline end-to-end through a `MockEndpoint`

## Key Types

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

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Ollama_GenerateAsync_ReturnsExpected` | Generate text with Ollama |
| 2 | `RagFlow_ChatAsync_ReturnsChatResponse` | RAG chat flow returns response |
| 3 | `OllamaSettings_Defaults` | OllamaSettings default values |
| 4 | `RagFlowOptions_Defaults` | RagFlowOptions default values |
| 5 | `RagFlowChatResponse_RecordShape` | RagFlowChatResponse record shape |
| 6 | `E2E_MockEndpoint_AiEnrichedPipeline` | End-to-end AI-enriched pipeline |

> 💻 [`tests/TutorialLabs/Tutorial40/Lab.cs`](../tests/TutorialLabs/Tutorial40/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial40.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullRagChatFlow_ThroughMockEndpoint` | 🟢 Starter | Full RAG chat flow through MockEndpoint |
| 2 | `Challenge2_OllamaAnalysis_WithSystemPrompt` | 🟡 Intermediate | Ollama analysis with system prompt |
| 3 | `Challenge3_RagFlowDatasetListing_AndHealthCheck` | 🔴 Advanced | RAG Flow dataset listing and health check |

> 💻 [`tests/TutorialLabs/Tutorial40/Exam.cs`](../tests/TutorialLabs/Tutorial40/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial40.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial40.ExamAnswers"
```

---

**Previous: [← Tutorial 39 — Message Lifecycle](39-message-lifecycle.md)** | **Next: [Tutorial 41 — OpenClaw Web UI →](41-openclaw-web.md)**
