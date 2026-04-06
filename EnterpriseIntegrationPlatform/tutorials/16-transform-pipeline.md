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

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial16/Lab.cs`](../tests/TutorialLabs/Tutorial16/Lab.cs)

**Objective:** Design a multi-step transform pipeline, trace how immutable `TransformContext` preserves **atomicity** through each stage, and analyze pipeline **scalability** under failure conditions.

### Step 1: Design a Transform Pipeline

Design a 3-step pipeline for PCI-compliant order processing:

| Step | Transform | Class | Purpose |
|------|-----------|-------|---------|
| 1 | XML → JSON | `XmlToJsonStep` | Convert partner XML to canonical JSON |
| 2 | Redact PII | `RegexReplaceStep` | Mask email addresses with `***@***` |
| 3 | Filter fields | `JsonPathFilterStep` | Keep only `$.order.id` and `$.order.total` |

Open `src/Processing.Transform/` and verify each step class exists. Write the `TransformOptions` configuration for this pipeline.

### Step 2: Trace Failure Recovery with StepsApplied

After step 2 of 4, the pipeline fails (e.g., `JsonPathFilterStep` encounters malformed JSON):

1. What is `TransformPipelineResult.StepsApplied`? (answer: 2)
2. Is the original source message modified? (hint: `TransformContext.WithPayload` creates copies)
3. How does the pipeline decide whether to retry vs. route to DLQ?

Explain why `TransformContext` uses `WithPayload` (immutable updates) instead of mutable setters — what **concurrency** benefit does this provide when multiple messages are being transformed in parallel?

### Step 3: Evaluate Pipeline Scalability

A pipeline processes 10,000 messages/second. Step 2 (regex redaction) is 5x slower than the other steps:

- Can you scale Step 2 independently? (hint: in Temporal, each step is an activity)
- What happens to pipeline throughput if you add a 4th step?
- How does the Pipes and Filters architecture prevent a slow step from blocking the entire system?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial16/Exam.cs`](../tests/TutorialLabs/Tutorial16/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 15 — Message Translator](15-message-translator.md)** | **Next: [Tutorial 17 — Normalizer →](17-normalizer.md)**
