# Tutorial 17 — Normalizer

## What You'll Learn

- The EIP Normalizer pattern for converting diverse formats to a canonical model
- How `INormalizer` / `MessageNormalizer` auto-detects JSON, XML, and CSV
- The `NormalizationResult` with `DetectedFormat` and `WasTransformed`
- `NormalizerOptions` for CSV delimiter, header mode, and strict content-type handling
- Why a canonical model simplifies downstream processing

---

## EIP Pattern: Normalizer

> *"Use a Normalizer to route each message type through a custom Message Translator so that the resulting messages match a common format."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  JSON  ──────▶ ┌──────────────┐
  XML   ──────▶ │  Normalizer  │──▶ Canonical JSON
  CSV   ──────▶ └──────────────┘
```

External systems send data in many formats. The Normalizer detects the incoming format and converts it to the platform's canonical representation (JSON), so every downstream component only needs to understand one format.

---

## Platform Implementation

### INormalizer

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

### MessageNormalizer (concrete)

The `MessageNormalizer` class implements `INormalizer`. It:
1. Inspects the `contentType` parameter (or payload content if `StrictContentType = false`)
2. Detects whether the payload is JSON, XML, or CSV
3. Applies the appropriate conversion to produce canonical JSON
4. Returns the result with the detected format

### NormalizationResult

```csharp
// src/Processing.Transform/NormalizationResult.cs
public sealed record NormalizationResult(
    string Payload,
    string OriginalContentType,
    string DetectedFormat,       // "JSON", "XML", "CSV"
    bool WasTransformed);
```

When the payload is already JSON, `WasTransformed = false` and the payload passes through unchanged.

### NormalizerOptions

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

| Option | Purpose |
|--------|---------|
| `StrictContentType` | When `true`, unknown content types throw. When `false`, the normalizer sniffs the payload. |
| `CsvDelimiter` | Delimiter character for CSV parsing (default `,`). |
| `CsvHasHeaders` | When `true`, the first CSV row becomes JSON property names. |
| `XmlRootName` | Root element name used when converting non-XML formats to XML (not used for XML→JSON). |

---

## Scalability Dimension

The normalizer is **stateless and CPU-bound** — each invocation depends only on the payload and options. Horizontal scaling is straightforward: add more consumer replicas. XML parsing and CSV parsing are the most CPU-intensive paths; JSON pass-through is near zero-cost. Profiling with `Processing.Profiling` can identify which format conversions dominate and guide replica sizing.

---

## Atomicity Dimension

Normalization happens **before** any downstream processing. If normalization fails (e.g. malformed XML), the message is Nacked and can be routed to the DLQ with `DeadLetterReason.ValidationFailed`. The `OriginalContentType` and `DetectedFormat` fields in the result provide full traceability of what the normalizer received and what it detected — essential for diagnosing format mismatches in production.

---

## Exercises

1. A partner sends CSV with `|` as delimiter and no header row. Write the `NormalizerOptions` configuration for this case.

2. A payload arrives with `contentType = "application/json"` but contains invalid JSON. What happens when `StrictContentType = true`? What about `false`?

3. Why does the platform choose JSON as the canonical format rather than XML or a binary format like Protocol Buffers?

---

**Previous: [← Tutorial 16 — Transform Pipeline](16-transform-pipeline.md)** | **Next: [Tutorial 18 — Content Enricher →](18-content-enricher.md)**
