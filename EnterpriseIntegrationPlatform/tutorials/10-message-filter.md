# Tutorial 10 — Message Filter

## What You'll Learn

- The EIP Message Filter pattern and how it differs from routing
- How `IMessageFilter` evaluates predicates with AND/OR logic
- The `MessageFilterResult` with Passed / DiscardReason / DiscardTopic
- How `RuleCondition` and `RuleLogicOperator` compose complex predicates
- Why discarded messages are never silently dropped — they go to a DLQ

---

## EIP Pattern: Message Filter

> *"Use a Message Filter to eliminate undesired messages from a channel based on a set of criteria."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
                  ┌────────────────┐
                  │ Message Filter │
  ──Message──▶    │  (predicate)   │──▶ Output Topic   (passed)
                  │                │──▶ Discard / DLQ   (failed predicate)
                  └────────────────┘
```

Unlike the Content-Based Router (which selects *which* topic), the Message Filter decides *whether* the message continues at all. Messages that fail the predicate are discarded — but in this platform, "discarded" does not mean lost.

---

## Platform Implementation

### IMessageFilter

```csharp
// src/Processing.Routing/IMessageFilter.cs
public interface IMessageFilter
{
    Task<MessageFilterResult> FilterAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

### MessageFilterResult

```csharp
// src/Processing.Routing/MessageFilterResult.cs
public sealed record MessageFilterResult(
    bool Passed,
    string? OutputTopic,
    string Reason);
```

`Passed = true` → message published to `OutputTopic`.
`Passed = false` → message routed to the configured `DiscardTopic` (DLQ) or, only if no discard topic is set, silently dropped.

### MessageFilterOptions

```csharp
// src/Processing.Routing/MessageFilterOptions.cs
public sealed class MessageFilterOptions
{
    public IReadOnlyList<RuleCondition> Conditions { get; init; } = [];
    public RuleLogicOperator Logic { get; init; } = RuleLogicOperator.And;
    public required string OutputTopic { get; init; }
    public string? DiscardTopic { get; init; }
    public bool RequireDiscardTopic { get; init; }
}
```

### RuleCondition (from RuleEngine)

```csharp
// src/RuleEngine/RuleCondition.cs
public sealed record RuleCondition
{
    public required string FieldName { get; init; }
    public required RuleConditionOperator Operator { get; init; }
    public required string Value { get; init; }
}
```

Multiple conditions are combined with `RuleLogicOperator.And` (all must match) or `RuleLogicOperator.Or` (at least one must match).

---

## Scalability Dimension

The filter is **stateless** — each evaluation depends only on the envelope content and the immutable predicate configuration. Any number of replicas can evaluate concurrently with no shared state. Scaling out is as simple as adding consumer instances to the competing-consumer group.

---

## Atomicity Dimension

The platform enforces **no silent drops** in production deployments. When a `DiscardTopic` is configured, every filtered-out message is published to that topic with the discard reason before the source message is Acked. If the DLQ publish fails, the source message is Nacked and redelivered. This guarantees every message is accounted for — either it reaches the output topic or it reaches the discard topic with a reason.

---

## Lab

**Objective:** Configure message filter rules, analyze the no-silent-drop guarantee with `RequireDiscardTopic`, and design a filter topology for **scalable** multi-stage message processing.

### Step 1: Configure a Filter with Discard Routing

Write a `MessageFilterOptions` configuration that passes only messages where `MessageType = "OrderCreated"` AND `Payload.total > 100`:

```csharp
var options = new MessageFilterOptions
{
    Conditions = [
        new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" },
        new RuleCondition { FieldName = "Payload.total", Operator = RuleConditionOperator.GreaterThan, Value = "100" }
    ],
    Logic = RuleLogicOperator.And,
    OutputTopic = "high-value-orders",
    DiscardTopic = "filtered-out.orders",
    RequireDiscardTopic = true
};
```

Explain what happens when `RequireDiscardTopic = true` and no `DiscardTopic` is configured — how does this enforce **zero message loss**?

### Step 2: Trace the Filter's Atomicity Guarantee

Open `src/Processing.Routing/MessageFilter.cs`. Trace the code path for a message that fails all conditions:

1. The filter evaluates conditions → all fail → `MessageFilterResult.Passed = false`
2. With `DiscardTopic` set → message is published to the discard topic
3. With `DiscardTopic` null and `RequireDiscardTopic = true` → what exception is thrown?

Draw the decision tree and explain how this guarantees every message is either delivered to `OutputTopic` or explicitly routed to `DiscardTopic` — never silently dropped.

### Step 3: Design a Multi-Stage Filter Pipeline

Design a pipeline with three cascading filters for an insurance claims system:

| Stage | Filter Criteria | Output | Discard |
|-------|----------------|--------|---------|
| 1 | Claim amount > $0 and valid policy number | `claims.validated` | `claims.invalid` |
| 2 | Claim type is "auto" or "home" | `claims.supported` | `claims.unsupported` |
| 3 | Claim amount < $50,000 (auto-approve threshold) | `claims.auto-approve` | `claims.manual-review` |

How does each filter's **discard topic** become a different team's input? How does this design scale — can each filter stage run independently with its own consumer group?

## Exam

1. A message fails all filter conditions but no `DiscardTopic` is configured and `RequireDiscardTopic = false`. What happens?
   - A) The filter throws an `InvalidOperationException`
   - B) The message is silently dropped — the filter logs a warning but takes no further action
   - C) The message is automatically routed to the Dead Letter Queue
   - D) The filter retries evaluation with relaxed conditions

2. How does the Message Filter differ from the Content-Based Router in the EIP pattern catalog?
   - A) They are identical patterns with different names
   - B) The Router selects one of many output channels based on content; the Filter has a binary decision — pass or discard — making it simpler and more efficient for yes/no criteria
   - C) The Filter can route to multiple topics simultaneously
   - D) The Router only works with XML messages

3. Why is `RequireDiscardTopic` essential for **production atomicity** in enterprise integration?
   - A) It improves message throughput by forcing batch processing
   - B) It prevents silent message loss — in production, every message must be accounted for, and throwing an exception forces the team to configure a discard destination before deployment
   - C) It enables faster regex evaluation
   - D) It is required by the NATS JetStream protocol

---

**Previous: [← Tutorial 09 — Content-Based Router](09-content-based-router.md)** | **Next: [Tutorial 11 — Dynamic Router →](11-dynamic-router.md)**
