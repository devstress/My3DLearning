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

## Exercises

1. Write a `FieldMapping` list that maps `{ "first_name": "Alice", "last_name": "Smith" }` to `{ "fullName": "Alice Smith", "source": "CRM" }`. Hint: one mapping uses `StaticValue`.

2. When would you use `FuncPayloadTransform` vs `JsonFieldMappingTransform`? Give an example of each.

3. A translator receives a JSON message but the target system expects XML. Which platform components would you combine to achieve this?

---

**Previous: [← Tutorial 14 — Process Manager](14-process-manager.md)** | **Next: [Tutorial 16 — Transform Pipeline →](16-transform-pipeline.md)**
