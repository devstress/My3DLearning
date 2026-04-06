# Tutorial 40 — RAG with Ollama

Query platform knowledge using Retrieval-Augmented Generation with Ollama embeddings.

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

## Exercises

### 1. IOllamaService — InterfaceShape HasExpectedMethods

```csharp
var type = typeof(IOllamaService);

Assert.That(type.GetMethod("GenerateAsync"), Is.Not.Null);
Assert.That(type.GetMethod("AnalyseAsync"), Is.Not.Null);
Assert.That(type.GetMethod("IsHealthyAsync"), Is.Not.Null);
```

### 2. IRagFlowService — InterfaceShape HasExpectedMethods

```csharp
var type = typeof(IRagFlowService);

Assert.That(type.GetMethod("RetrieveAsync"), Is.Not.Null);
Assert.That(type.GetMethod("ChatAsync"), Is.Not.Null);
Assert.That(type.GetMethod("ListDatasetsAsync"), Is.Not.Null);
Assert.That(type.GetMethod("IsHealthyAsync"), Is.Not.Null);
```

### 3. Mock — IOllamaService GenerateAsync ReturnsExpected

```csharp
var ollama = Substitute.For<IOllamaService>();
ollama.GenerateAsync(
        "What is EIP?",
        Arg.Any<string>(),
        Arg.Any<CancellationToken>())
    .Returns("Enterprise Integration Patterns");

var result = await ollama.GenerateAsync("What is EIP?");

Assert.That(result, Is.EqualTo("Enterprise Integration Patterns"));
```

### 4. Mock — IRagFlowService ChatAsync ReturnsChatResponse

```csharp
var ragFlow = Substitute.For<IRagFlowService>();
var expectedResponse = new RagFlowChatResponse(
    Answer: "The answer is 42",
    ConversationId: "conv-123",
    References: new List<RagFlowReference>
    {
        new("Relevant passage", "doc.pdf", 0.95),
    });

ragFlow.ChatAsync("What is the answer?", null, Arg.Any<CancellationToken>())
    .Returns(expectedResponse);

var result = await ragFlow.ChatAsync("What is the answer?");

Assert.That(result.Answer, Is.EqualTo("The answer is 42"));
Assert.That(result.ConversationId, Is.EqualTo("conv-123"));
Assert.That(result.References, Has.Count.EqualTo(1));
```

### 5. RagFlowChatResponse — RecordShape

```csharp
var refs = new List<RagFlowReference>
{
    new("passage 1", "file1.pdf", 0.9),
    new("passage 2", "file2.pdf", 0.8),
};

var response = new RagFlowChatResponse("Answer text", "conv-1", refs);

Assert.That(response.Answer, Is.EqualTo("Answer text"));
Assert.That(response.ConversationId, Is.EqualTo("conv-1"));
Assert.That(response.References, Has.Count.EqualTo(2));
Assert.That(response.References[0].DocumentName, Is.EqualTo("file1.pdf"));
Assert.That(response.References[1].Score, Is.EqualTo(0.8));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial40/Lab.cs`](../tests/TutorialLabs/Tutorial40/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial40.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial40/Exam.cs`](../tests/TutorialLabs/Tutorial40/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial40.Exam"
```

---

**Previous: [← Tutorial 39 — Message Lifecycle](39-message-lifecycle.md)** | **Next: [Tutorial 41 — OpenClaw Web UI →](41-openclaw-web.md)**
