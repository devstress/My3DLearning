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

**Objective:** Configure the Normalizer for multi-format input handling, analyze how the Canonical Data Model pattern enables **scalable** integration with diverse source systems, and design normalization strategies for edge cases.

### Step 1: Configure a CSV Normalizer

A partner sends CSV files with `|` as delimiter and no header row. Open `src/Processing.Normalizer/` and configure `NormalizerOptions`:

```csharp
var options = new NormalizerOptions
{
    CsvDelimiter = '|',
    CsvHasHeader = false,
    CsvColumnNames = ["orderId", "customerId", "amount", "currency"],
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

1. Why does the platform normalize all messages to a **Canonical Data Model** (JSON)?
   - A) JSON is faster to parse than all other formats
   - B) A single canonical format means adding a new source system requires only one new translator — not one for every downstream consumer — making the integration platform scale linearly with the number of systems
   - C) JSON is required by the NATS protocol
   - D) The .NET runtime only supports JSON serialization

2. What is the risk of setting `StrictContentType = false` in a production environment?
   - A) No risk — lenient mode is always preferred
   - B) A message could be misinterpreted — e.g., XML interpreted as JSON due to format sniffing — leading to corrupt data flowing through the pipeline undetected, violating **data atomicity**
   - C) Lenient mode disables all content validation
   - D) Strict mode is slower than lenient mode

3. How does the Normalizer pattern reduce **integration complexity** when scaling from 5 to 50 connected systems?
   - A) It doesn't — complexity grows equally regardless
   - B) Without normalization, N sources × M consumers = N×M translators; with normalization, only N + M translators are needed — this is the difference between O(N²) and O(N) scaling
   - C) The Normalizer caches all messages, reducing duplicate processing
   - D) The Normalizer compresses messages to reduce broker storage

---

**Previous: [← Tutorial 16 — Transform Pipeline](16-transform-pipeline.md)** | **Next: [Tutorial 18 — Content Enricher →](18-content-enricher.md)**
