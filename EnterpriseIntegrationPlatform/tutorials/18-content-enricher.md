# Tutorial 18 — Content Enricher

Augment messages with external data via `IContentEnricher`, merging fetched fields without overwriting existing payload.

---

## Learning Objectives

1. Understand the Content Enricher pattern and how it augments messages with external data
2. Use `ContentEnricher` with an `IEnrichmentSource` to fetch and merge data at a target path
3. Extract lookup keys from nested JSON paths using `LookupKeyPath` configuration
4. Configure fallback behaviour when the enrichment source returns null or the lookup key is missing
5. Verify that enrichment preserves all existing payload fields without overwriting

---

## Key Types

```csharp
// src/Processing.Transform/IContentEnricher.cs
public interface IContentEnricher
{
    Task<string> EnrichAsync(
        string payload,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Transform/ContentEnricherOptions.cs
public sealed class ContentEnricherOptions
{
    public string EndpointUrlTemplate { get; init; }
    public string LookupKeyPath { get; init; }
    public string MergeTargetPath { get; init; }
    public bool FallbackOnFailure { get; init; }
    public string? FallbackValue { get; init; }
}
```

```csharp
// src/Processing.Transform/IEnrichmentSource.cs
public interface IEnrichmentSource
{
    Task<JsonNode?> FetchAsync(string key, CancellationToken cancellationToken = default);
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Content Enricher concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Enrich_MergesExternalData` | Merge external data at a target path |
| 2 | `Enrich_NestedLookup_ExtractsCorrectKey` | Nested lookup key path extracts correct value |
| 3 | `Enrich_SourceReturnsNull_UsesFallback` | Source returns null — fallback value merged |
| 4 | `Enrich_MissingLookupKey_FallsBack` | Missing lookup key with fallback returns gracefully |
| 5 | `Enrich_MissingLookupKey_ThrowsWhenNoFallback` | Missing lookup key without fallback throws |
| 6 | `Enrich_E2E_PublishEnrichedToNatsEndpoint` | End-to-end enrich and publish via real NATS |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial18.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Content Enricher pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_DeepNestedMerge_EnrichesAtNestedPath` | 🟢 Starter |
| 2 | `Intermediate_NumericLookupKey_ExtractsCorrectly` | 🟡 Intermediate |
| 3 | `Advanced_BatchEnrichment_MultipleMessagesPublished` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial18.Exam"
```

---

**Previous: [← Tutorial 17 — Normalizer](17-normalizer.md)** | **Next: [Tutorial 19 — Content Filter →](19-content-filter.md)**
