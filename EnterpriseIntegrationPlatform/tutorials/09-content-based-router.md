# Tutorial 09 — Content-Based Router

## What You'll Learn

- The EIP Content-Based Router pattern and when to apply it
- How `IContentBasedRouter` evaluates routing rules by priority
- The `RoutingRule` model with Field / Operator / Value / TargetTopic / Priority
- The `RoutingOperator` enum and pre-compiled regex support
- Why a stateless router scales horizontally without coordination

---

## EIP Pattern: Content-Based Router

> *"Use a Content-Based Router to route each message to the correct recipient based on message content."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
              ┌──────────────┐
              │ Content-Based│
 ──Message──▶ │    Router     │──▶ Topic A  (rule 1 matched)
              │              │──▶ Topic B  (rule 2 matched)
              │              │──▶ Default  (no rule matched)
              └──────────────┘
```

The router inspects a field inside the message (header, metadata key, or JSON payload path) and selects the output topic by evaluating rules in priority order. The first rule that matches wins.

---

## Platform Implementation

### IContentBasedRouter

```csharp
// src/Processing.Routing/IContentBasedRouter.cs
public interface IContentBasedRouter
{
    Task<RoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

### RoutingRule

```csharp
// src/Processing.Routing/RoutingRule.cs
public sealed record RoutingRule
{
    public required int Priority { get; init; }
    public required string FieldName { get; init; }   // e.g. "MessageType", "Payload.order.region"
    public required RoutingOperator Operator { get; init; }
    public required string Value { get; init; }
    public required string TargetTopic { get; init; }
    public string? Name { get; init; }
}
```

### RoutingOperator

```csharp
// src/Processing.Routing/RoutingOperator.cs
public enum RoutingOperator
{
    Equals,
    Contains,
    StartsWith,
    Regex
}
```

Rules are sorted by ascending `Priority`. Regex patterns are compiled once at startup (`RegexOptions.Compiled`) and cached in a dictionary keyed by rule, avoiding per-message compilation overhead.

### RoutingDecision

```csharp
public sealed record RoutingDecision(
    string TargetTopic,
    RoutingRule? MatchedRule,
    bool IsDefault);
```

When no rule matches and a default topic is configured, `IsDefault = true`. When no rule matches and no default exists, the router throws `InvalidOperationException`.

---

## Scalability Dimension

The `ContentBasedRouter` is **stateless** — it holds no per-message state between invocations. Routing rules are loaded once from configuration and shared read-only across all requests. This means you can run N replicas behind a competing-consumer group and every replica makes identical routing decisions. Horizontal scaling is limited only by broker throughput, not by router coordination.

---

## Atomicity Dimension

The router publishes to the selected topic via the broker producer **before** acknowledging the source message. If the publish fails, the source message is Nacked and the broker redelivers it. If the process crashes after publish but before Ack, the message may be routed twice — downstream consumers must be idempotent (the `IntegrationEnvelope.MessageId` enables deduplication). Combined with Temporal workflow orchestration, the platform guarantees **zero message loss**.

---

## Exercises

1. You have three routing rules with priorities 10, 5, and 1. A message matches rules at priorities 5 and 1. Which topic does the message go to and why?

2. A new requirement says messages with `Payload.customer.tier = "platinum"` must go to `priority-processing`. Write the `RoutingRule` record for this requirement.

3. Why is pre-compiling regex patterns important for a high-throughput router? What happens if you skip compilation?

---

**Previous: [← Tutorial 08 — Activities and the Pipeline](08-activities-pipeline.md)** | **Next: [Tutorial 10 — Message Filter →](10-message-filter.md)**
