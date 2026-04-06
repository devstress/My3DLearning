# Tutorial 19 — Content Filter

Strip payloads down to only the fields downstream consumers need using `IContentFilter` and `JsonPathFilterStep`.

---

## Key Types

```csharp
// src/Processing.Transform/IContentFilter.cs
public interface IContentFilter
{
    Task<string> FilterAsync(
        string payload,
        IReadOnlyList<string> keepPaths,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Transform/JsonPathFilterStep.cs  (implements ITransformStep)
public class JsonPathFilterStep : ITransformStep
{
    public JsonPathFilterStep(IEnumerable<string> keepPaths);
    public string Name => "JsonPathFilter";
    public Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default);
}
```

---

## Exercises

### Exercise 1: Retain only specified top-level paths

```csharp
var step = new JsonPathFilterStep(new[] { "name", "age" });
var context = new TransformContext(
    """{"name":"Alice","age":30,"email":"a@b.com","role":"admin"}""",
    "application/json");

var result = await step.ExecuteAsync(context);

using var doc = JsonDocument.Parse(result.Payload);
Assert.That(doc.RootElement.TryGetProperty("name", out _), Is.True);
Assert.That(doc.RootElement.TryGetProperty("age", out _), Is.True);
Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.False);
Assert.That(doc.RootElement.TryGetProperty("role", out _), Is.False);
```

### Exercise 2: Extract nested properties

```csharp
var step = new JsonPathFilterStep(new[] { "order.id", "customer.name" });
var payload = """
    {
        "order": {"id": "ORD-1", "total": 100},
        "customer": {"name": "Bob", "email": "bob@test.com"},
        "internal": "secret"
    }
    """;

var context = new TransformContext(payload, "application/json");
var result = await step.ExecuteAsync(context);

using var doc = JsonDocument.Parse(result.Payload);
Assert.That(doc.RootElement.GetProperty("order").GetProperty("id").GetString(),
    Is.EqualTo("ORD-1"));
Assert.That(doc.RootElement.GetProperty("customer").GetProperty("name").GetString(),
    Is.EqualTo("Bob"));
Assert.That(doc.RootElement.TryGetProperty("internal", out _), Is.False);
```

### Exercise 3: Missing paths are silently skipped

```csharp
var step = new JsonPathFilterStep(new[] { "name", "nonexistent" });
var context = new TransformContext(
    """{"name":"Alice","age":30}""", "application/json");

var result = await step.ExecuteAsync(context);

using var doc = JsonDocument.Parse(result.Payload);
Assert.That(doc.RootElement.TryGetProperty("name", out _), Is.True);
Assert.That(doc.RootElement.TryGetProperty("nonexistent", out _), Is.False);
```

### Exercise 4: Filter step inside a pipeline

```csharp
var filterStep = new JsonPathFilterStep(new[] { "order.id", "order.total" });
var options = Options.Create(new TransformOptions());
var pipeline = new TransformPipeline(
    new ITransformStep[] { filterStep }, options,
    NullLogger<TransformPipeline>.Instance);

var payload = """
    {"order":{"id":"ORD-5","total":250,"items":3},"customer":{"name":"Eve"}}
    """.Trim();

var result = await pipeline.ExecuteAsync(payload, "application/json");

using var doc = JsonDocument.Parse(result.Payload);
Assert.That(doc.RootElement.GetProperty("order").GetProperty("id").GetString(),
    Is.EqualTo("ORD-5"));
Assert.That(doc.RootElement.GetProperty("order").GetProperty("total").GetInt32(),
    Is.EqualTo(250));
Assert.That(doc.RootElement.TryGetProperty("customer", out _), Is.False);
Assert.That(result.StepsApplied, Is.EqualTo(1));
```

### Exercise 5: ContentFilter retains only keep paths

```csharp
var filter = new ContentFilter(NullLogger<ContentFilter>.Instance);

var payload = """
    {"user":"Alice","age":30,"email":"a@b.com","role":"admin","secret":"x"}
    """.Trim();

var result = await filter.FilterAsync(payload, new[] { "user", "age" });

using var doc = JsonDocument.Parse(result);
Assert.That(doc.RootElement.TryGetProperty("user", out _), Is.True);
Assert.That(doc.RootElement.TryGetProperty("age", out _), Is.True);
Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.False);
Assert.That(doc.RootElement.TryGetProperty("secret", out _), Is.False);
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial19/Lab.cs`](../tests/TutorialLabs/Tutorial19/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial19.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial19/Exam.cs`](../tests/TutorialLabs/Tutorial19/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial19.Exam"
```

---

**Previous: [← Tutorial 18 — Content Enricher](18-content-enricher.md)** | **Next: [Tutorial 20 — Splitter →](20-splitter.md)**
