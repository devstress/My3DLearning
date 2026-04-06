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

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial17/Lab.cs`](../tests/TutorialLabs/Tutorial17/Lab.cs)

**Objective:** Configure the Normalizer for multi-format input handling, analyze how the Canonical Data Model pattern enables **scalable** integration with diverse source systems, and design normalization strategies for edge cases.

### Step 1: Configure a CSV Normalizer

A partner sends CSV files with `|` as delimiter and no header row. Open `src/Processing.Transform/` and configure `NormalizerOptions`:

```csharp
var options = new NormalizerOptions
{
    CsvDelimiter = '|',
    CsvHasHeaders = false,
    StrictContentType = true
};
```

Trace what happens when: (a) a valid CSV arrives, (b) JSON arrives with `contentType = "text/csv"` but `StrictContentType = true`.

### Step 2: Map the Canonical Data Model

The platform normalizes all inputs to JSON. Draw a diagram showing 4 source systems and how they funnel through the Normalizer:

```
Partner A (XML) ─────┐
Partner B (CSV) ─────┤
Partner C (JSON) ────┼──→ Normalizer ──→ Canonical JSON ──→ Router ──→ N consumers
Internal API (JSON) ─┘
```

How many translators are needed for 4 sources and 6 consumers? With a canonical model: **4** (one per source). Without: **24** (4×6). This is the **scalability** argument for normalization.

### Step 3: Handle Format Detection Failures

A payload arrives with `contentType = "application/json"` but contains invalid JSON. Analyze:

- What happens when `StrictContentType = true`? (exception → DLQ)
- What happens when `StrictContentType = false`? (format sniffing attempt)
- Why is strict mode recommended for production **atomicity** — what risks does lenient mode introduce?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial17/Exam.cs`](../tests/TutorialLabs/Tutorial17/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 16 — Transform Pipeline](16-transform-pipeline.md)** | **Next: [Tutorial 18 — Content Enricher →](18-content-enricher.md)**
