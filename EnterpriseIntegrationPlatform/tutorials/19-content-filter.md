# Tutorial 19 — Content Filter

Strip payloads down to only the fields downstream consumers need using `IContentFilter` and `JsonPathFilterStep`.

---

## Learning Objectives

1. Understand the Content Filter pattern and how it strips payloads to only needed fields
2. Use `ContentFilter` and `JsonPathFilterStep` to retain specified JSON paths
3. Verify that nested paths preserve parent structure while removing unneeded siblings
4. Confirm that missing paths are silently skipped without errors
5. Validate error handling for empty keep-paths and non-JSON-object payloads
6. Integrate `JsonPathFilterStep` as a step in a `TransformPipeline`

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

## Lab — Guided Practice

> 💻 Run the lab tests to see each Content Filter concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Filter_RetainsSpecifiedPaths` | Retain only specified top-level and nested paths |
| 2 | `Filter_MissingPath_SkippedSilently` | Missing paths are silently skipped |
| 3 | `Filter_NestedPaths_PreservesStructure` | Nested paths preserve parent structure |
| 4 | `Filter_EmptyKeepPaths_ThrowsArgumentException` | Empty keep-paths throws ArgumentException |
| 5 | `Filter_NonJsonObject_ThrowsInvalidOperation` | Non-JSON-object payload throws InvalidOperationException |
| 6 | `Filter_E2E_PublishFilteredToNatsEndpoint` | End-to-end filter and publish via real NATS |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial19.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Content Filter pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_JsonPathFilterStep_FiltersInPipeline` | 🟢 Starter |
| 2 | `Intermediate_DeeplyNestedFilter_ExtractsCorrectly` | 🟡 Intermediate |
| 3 | `Advanced_BatchFilter_MultipleMessagesPublished` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial19.Exam"
```

---

**Previous: [← Tutorial 18 — Content Enricher](18-content-enricher.md)** | **Next: [Tutorial 20 — Splitter →](20-splitter.md)**
