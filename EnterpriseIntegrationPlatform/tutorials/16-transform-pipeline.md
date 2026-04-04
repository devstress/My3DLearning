# Tutorial 16 — Transform Pipeline

## What You'll Learn

- The EIP Pipes and Filters pattern applied to payload transformation
- How `ITransformPipeline` chains `ITransformStep` instances in order
- Built-in steps: `JsonToXmlStep`, `XmlToJsonStep`, `RegexReplaceStep`, `JsonPathFilterStep`
- How `TransformContext` carries payload + content type + metadata through the pipeline
- The `TransformResult` returned after all steps complete

---

## EIP Pattern: Pipes and Filters (Transformation)

> *"Use Pipes and Filters to divide a larger processing task into a sequence of smaller, independent processing steps (Filters) that are connected by channels (Pipes)."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌────────────┐    ┌────────────┐    ┌──────────────┐    ┌──────────────┐
  │ JsonToXml  │───▶│ RegexReplace│───▶│ XmlToJson    │───▶│ JsonPathFilter│
  │   Step     │    │   Step      │    │   Step       │    │   Step        │
  └────────────┘    └────────────┘    └──────────────┘    └──────────────┘
       ▲                                                          │
       │                TransformContext flows through             │
       └──────────────────────────────────────────────────────────┘
```

Each step receives a `TransformContext`, performs one transformation, and returns an updated context. Steps are composed sequentially — the output of one step is the input to the next.

---

## Platform Implementation

### ITransformPipeline

```csharp
// src/Processing.Transform/ITransformPipeline.cs
public interface ITransformPipeline
{
    Task<TransformResult> ExecuteAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default);
}
```

### ITransformStep

```csharp
// src/Processing.Transform/ITransformStep.cs
public interface ITransformStep
{
    string Name { get; }
    Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default);
}
```

### TransformContext

```csharp
// src/Processing.Transform/TransformContext.cs
public sealed class TransformContext
{
    public string Payload { get; }
    public string ContentType { get; }
    public Dictionary<string, string> Metadata { get; init; } = new();

    public TransformContext WithPayload(string payload, string contentType) => ...;
    public TransformContext WithPayload(string payload) => ...;
}
```

The context is **immutable per step** — each step creates a new context via `WithPayload`, preserving metadata across the pipeline.

### Built-in Steps

| Step | Description |
|------|-------------|
| `JsonToXmlStep` | Converts JSON payload to XML, updates `ContentType` to `application/xml` |
| `XmlToJsonStep` | Converts XML payload to JSON, updates `ContentType` to `application/json` |
| `RegexReplaceStep` | Applies a regex find-and-replace on the raw payload string |
| `JsonPathFilterStep` | Filters a JSON payload to keep only specified JSONPath expressions |

### TransformResult

```csharp
// src/Processing.Transform/TransformResult.cs
public sealed record TransformResult(
    string Payload,
    string ContentType,
    int StepsApplied,
    IReadOnlyDictionary<string, string> Metadata);
```

---

## Scalability Dimension

The pipeline runs entirely **in-process** — all steps execute sequentially within a single service instance. To scale, run multiple replicas of the service; each replica processes different messages through its own pipeline instance. Steps are stateless, so there is no coordination between replicas. For CPU-intensive pipelines (large XML conversions), vertical scaling (more CPU per replica) complements horizontal scaling.

---

## Atomicity Dimension

The pipeline is **all-or-nothing** within a single invocation. If any step throws, the entire pipeline fails and the calling code can Nack the source message. Partial results are not published. The `StepsApplied` count in `TransformResult` tells you exactly how far the pipeline progressed before completion (or failure during diagnostics).

---

## Exercises

1. Design a 3-step pipeline that: (a) converts XML to JSON, (b) applies a regex to redact email addresses, (c) filters to keep only `$.order.id` and `$.order.total`. List the steps in order.

2. After step 2 of 4 the pipeline fails. What is `StepsApplied`? What happens to the source message?

3. Why does `TransformContext` use `WithPayload` instead of mutable setters? What concurrency benefit does this provide?

---

**Previous: [← Tutorial 15 — Message Translator](15-message-translator.md)** | **Next: [Tutorial 17 — Normalizer →](17-normalizer.md)**
