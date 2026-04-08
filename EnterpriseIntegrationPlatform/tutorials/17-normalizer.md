# Tutorial 17 — Normalizer

Auto-detect JSON, XML, and CSV payloads and convert them to canonical JSON using `INormalizer`.

---

## Learning Objectives

1. Understand the Normalizer pattern and how it converts heterogeneous formats into canonical JSON
2. Use `MessageNormalizer` to auto-detect JSON, XML, and CSV payloads by content type
3. Verify that JSON payloads pass through unchanged while XML and CSV are transformed
4. Configure `NormalizerOptions` for strict vs. non-strict content-type enforcement
5. Customize CSV parsing with delimiter and header options
6. Inspect `NormalizationResult` fields: `DetectedFormat`, `WasTransformed`, and `OriginalContentType`

---

## Key Types

```csharp
// src/Processing.Transform/INormalizer.cs
public interface INormalizer
{
    Task<NormalizationResult> NormalizeAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Transform/NormalizationResult.cs
public sealed record NormalizationResult(
    string Payload,
    string OriginalContentType,
    string DetectedFormat,       // "JSON", "XML", "CSV"
    bool WasTransformed);
```

```csharp
// src/Processing.Transform/NormalizerOptions.cs
public sealed class NormalizerOptions
{
    public bool StrictContentType { get; init; } = true;
    public char CsvDelimiter { get; init; } = ',';
    public bool CsvHasHeaders { get; init; } = true;
    public string XmlRootName { get; init; } = "Root";
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Normalizer concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Normalize_Json_PassesThroughUnchanged` | JSON payload passes through without transformation |
| 2 | `Normalize_Xml_ConvertsToJson` | XML payload converts to canonical JSON |
| 3 | `Normalize_Csv_ConvertsToJsonArray` | CSV payload converts to JSON array |
| 4 | `Normalize_StrictContentType_ThrowsForUnknown` | Strict mode throws for unknown content types |
| 5 | `Normalize_NonStrict_DetectsJsonByPayload` | Non-strict mode detects format by payload inspection |
| 6 | `Normalize_E2E_PublishNormalizedToNatsEndpoint` | End-to-end normalize and publish via real NATS |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial17.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Normalizer pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_XmlRepeatedElements_ProducesJsonArrays` | 🟢 Starter |
| 2 | `Intermediate_CsvCustomDelimiter_ParsesCorrectly` | 🟡 Intermediate |
| 3 | `Advanced_MultiformatBatch_NormalizeAndPublish` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial17.Exam"
```

---

**Previous: [← Tutorial 16 — Transform Pipeline](16-transform-pipeline.md)** | **Next: [Tutorial 18 — Content Enricher →](18-content-enricher.md)**
