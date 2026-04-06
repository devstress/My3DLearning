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

## Lab

**Objective:** Configure routing rules with priorities, trace how the Content-Based Router dispatches messages, and analyze routing **scalability** under high-throughput conditions.

### Step 1: Configure a Multi-Rule Routing Table

Open `src/Processing.Routing/ContentBasedRouter.cs`. Create a routing configuration for an e-commerce platform:

| Priority | Field | Operator | Value | Output Topic |
|----------|-------|----------|-------|-------------|
| 1 | `Payload.customer.tier` | Equals | `"platinum"` | `priority-processing` |
| 5 | `MessageType` | Equals | `"OrderCreated"` | `orders.standard` |
| 10 | `MessageType` | Regex | `"Return.*"` | `returns.processing` |
| 100 | (default) | — | — | `general.inbox` |

A message arrives with `MessageType = "OrderCreated"` and `Payload.customer.tier = "platinum"`. Which topic does it route to? Explain how priority ordering ensures deterministic routing.

### Step 2: Trace the Routing Decision Path

Using the `RoutingDecision` record, trace the router's decision path for a message that matches rules at priorities 1 and 5. Open the router implementation and identify:

- How does the router evaluate rules? (sequential scan vs. sorted by priority?)
- Does evaluation stop at the first match, or are all rules evaluated?
- What `RoutingDecision` is returned — does it include the matched rule for auditing?

### Step 3: Design for Routing Scalability

Consider a Content-Based Router processing 50,000 messages/second with 200 routing rules:

- What is the computational cost per message? (hint: O(n) for n rules)
- How does pre-compiling regex patterns (`RoutingOperator.Regex`) improve throughput?
- If you need to route to different brokers (Kafka for audit, NATS for real-time), how would the router's output topic abstraction enable this without code changes?

## Exam

1. You have routing rules with priorities 10, 5, and 1. A message matches rules at priorities 5 and 1. Which topic receives the message?
   - A) Both topics receive the message (fan-out)
   - B) Priority 1 — the router selects the lowest priority number (highest precedence) among matches
   - C) Priority 10 — the router always uses the first rule defined
   - D) Priority 5 — the router stops at the first match in definition order

2. How does the Content-Based Router pattern support **atomic message routing**?
   - A) It copies the message to all matching topics simultaneously
   - B) Each message is routed to exactly one output topic — the routing decision is deterministic and idempotent, so replaying the same message always produces the same routing outcome
   - C) It wraps the routing decision in a database transaction
   - D) The router buffers messages until a batch is complete

3. Why is pre-compiling regex patterns critical for **routing scalability** at high throughput?
   - A) Pre-compilation reduces memory allocation per evaluation — without it, each message creates and discards regex objects, causing GC pressure that degrades throughput under load
   - B) Pre-compilation is required by the .NET regex API
   - C) Pre-compilation allows patterns to match across multiple lines
   - D) Pre-compilation enables case-insensitive matching

---

**Previous: [← Tutorial 08 — Activities and the Pipeline](08-activities-pipeline.md)** | **Next: [Tutorial 10 — Message Filter →](10-message-filter.md)**
