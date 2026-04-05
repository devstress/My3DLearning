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

## Exercises

1. Write a `MessageFilterOptions` configuration that passes only messages where `MessageType = "OrderCreated"` AND `Payload.total > 100`. Specify a `DiscardTopic`.

2. A message fails all conditions but no `DiscardTopic` is configured. What happens? How would you change the design to prevent silent drops entirely?

3. Compare the Message Filter to the Content-Based Router. When would you use a filter instead of a router?

---

**Previous: [← Tutorial 09 — Content-Based Router](09-content-based-router.md)** | **Next: [Tutorial 11 — Dynamic Router →](11-dynamic-router.md)**
