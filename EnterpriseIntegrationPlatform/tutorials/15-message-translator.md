# Tutorial 15 — Message Translator

## What You'll Learn

- The EIP Message Translator pattern for payload format conversion
- How `IMessageTranslator<TIn,TOut>` transforms an entire envelope
- How `IPayloadTransform<TIn,TOut>` encapsulates the actual mapping logic
- Built-in transforms: `FuncPayloadTransform`, `JsonFieldMappingTransform`
- The `FieldMapping` model for declarative JSON field mapping
- `TranslationResult` that carries the translated envelope and target topic

---

## EIP Pattern: Message Translator

> *"Use a Message Translator, a special filter, between other filters or applications to translate one data format into another."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────────┐    ┌────────────────────┐    ┌──────────────┐
  │  Source App   │───▶│ Message Translator │───▶│  Target App  │
  │  (Format A)   │    │   A  ──▶  B        │    │  (Format B)  │
  └──────────────┘    └────────────────────┘    └──────────────┘
```

When two systems speak different data formats (JSON vs XML, different JSON schemas), the translator converts the payload without changing the messaging infrastructure.

---

## Platform Implementation

### IMessageTranslator<TIn, TOut>

```csharp
// src/Processing.Translator/IMessageTranslator.cs
public interface IMessageTranslator<TIn, TOut>
{
    Task<TranslationResult<TOut>> TranslateAsync(
        IntegrationEnvelope<TIn> source,
        CancellationToken cancellationToken = default);
}
```

### IPayloadTransform<TIn, TOut>

```csharp
// src/Processing.Translator/IPayloadTransform.cs
public interface IPayloadTransform<TIn, TOut>
{
    TOut Transform(TIn source);
}
```

The translator delegates to an `IPayloadTransform` for the actual mapping, then wraps the result in a new `IntegrationEnvelope<TOut>` preserving `CorrelationId`, `MessageId`, and metadata.

### FuncPayloadTransform

A convenience implementation that accepts a `Func<TIn, TOut>` delegate — useful for simple inline transformations without creating a full class.

### JsonFieldMappingTransform

Declarative field mapping driven by a list of `FieldMapping` records:

```csharp
// src/Processing.Translator/FieldMapping.cs
public sealed record FieldMapping
{
    public required string SourcePath { get; init; }   // e.g. "order.id"
    public required string TargetPath { get; init; }   // e.g. "orderId"
    public string? StaticValue { get; init; }          // inject fixed values
}
```

Source paths use dot notation (`customer.address.city`). The transform reads from the source JSON document at `SourcePath` and writes to the target document at `TargetPath`. When `StaticValue` is set, it is injected regardless of source content.

### TranslationResult

```csharp
// src/Processing.Translator/TranslationResult.cs
public sealed record TranslationResult<TOut>(
    IntegrationEnvelope<TOut> TranslatedEnvelope,
    Guid SourceMessageId,
    string TargetTopic);
```

---

## Scalability Dimension

Translators are **stateless pure functions** — given the same input, they produce the same output. This makes them ideal for horizontal scaling. Deploy as many translator replicas as needed behind a competing-consumer group. CPU-intensive transformations (large XML parsing) benefit directly from additional replicas.

---

## Atomicity Dimension

The translator publishes the translated envelope to the target topic **before** acknowledging the source. If the publish fails, the source message is Nacked and redelivered. The `SourceMessageId` in `TranslationResult` enables end-to-end tracing from the original message through the translation to the target topic.

---

## Lab

**Objective:** Build field mappings for cross-system data transformation, analyze how the Message Translator pattern preserves message **atomicity** through immutable transformations, and design a multi-format translation strategy.

### Step 1: Build a Field Mapping Configuration

Write a `FieldMapping` list that transforms this input:

```json
{ "first_name": "Alice", "last_name": "Smith", "email": "alice@example.com" }
```

Into this output:

```json
{ "fullName": "Alice Smith", "contactEmail": "alice@example.com", "source": "CRM" }
```

Identify: which mapping uses `SourcePath`, which uses `StaticValue`, and how would you combine `first_name` + `last_name` into `fullName`? Open `src/Processing.Translator/JsonFieldMappingTransform.cs` to verify the mapping mechanics.

### Step 2: Trace Immutability Through Translation

Open `src/Processing.Translator/MessageTranslator.cs`. When a message is translated:

1. Is the original `IntegrationEnvelope<T>` mutated, or is a new envelope created?
2. How does the `CausationId` of the translated message link back to the original?
3. If translation fails (e.g., missing required field), what happens to the original message?

Explain why **immutable transformation** is critical for atomicity: if translation fails, the original message is untouched and can be retried or routed to the DLQ.

### Step 3: Design a Multi-Format Translation Pipeline

A partner sends data in XML, but your downstream systems expect JSON. Another partner sends CSV. Design a translation strategy:

| Source Format | Translator Step | Output |
|--------------|----------------|--------|
| XML → JSON | `XmlToJsonStep` | Canonical JSON |
| CSV → JSON | Custom `IPayloadTransform` | Canonical JSON |
| JSON → Canonical | `JsonFieldMappingTransform` | Normalized envelope |

How does the **Canonical Data Model** (Tutorial 17 — Normalizer) relate to the Message Translator? Why is normalizing to a canonical format essential for **scalability** — what happens when you add a 5th source format?

## Exam

1. Why does the Message Translator create a **new envelope** rather than modifying the original?
   - A) .NET records are always immutable
   - B) Immutable transformation preserves the original for retry, DLQ routing, and audit — if translation fails, the untouched original maintains atomicity of the processing pipeline
   - C) The broker rejects modified messages
   - D) Creating new envelopes uses less memory

2. When would you use `FuncPayloadTransform` (code-based) vs. `JsonFieldMappingTransform` (configuration-based)?
   - A) They are interchangeable
   - B) `JsonFieldMappingTransform` for simple field renaming/mapping that non-developers can configure; `FuncPayloadTransform` for complex logic like format conversion, calculations, or API enrichment that requires code
   - C) `FuncPayloadTransform` is faster in all cases
   - D) `JsonFieldMappingTransform` only works with XML

3. How does the Canonical Data Model concept support **integration scalability**?
   - A) It reduces message size for faster transport
   - B) All message sources translate to one canonical format — adding a new source system requires only one new translator, not N translators for N downstream consumers
   - C) Canonical models encrypt data for security
   - D) It eliminates the need for a message broker

---

**Previous: [← Tutorial 14 — Process Manager](14-process-manager.md)** | **Next: [Tutorial 16 — Transform Pipeline →](16-transform-pipeline.md)**
