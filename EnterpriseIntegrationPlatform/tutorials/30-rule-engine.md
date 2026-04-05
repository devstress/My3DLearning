# Tutorial 30 — Rule Engine

## What You'll Learn

- How `IRuleEngine` evaluates business rules against integration messages
- `BusinessRuleEngine` with priority-ordered evaluation
- `IRuleStore` for persisting and querying rules at runtime
- `BusinessRule` with conditions (AND/OR) and actions (Route, Transform, Reject, DeadLetter)
- `RuleConditionOperator` enum for flexible condition matching
- `RuleEvaluationResult` and `InMemoryRuleStore`

---

## EIP Pattern: Message Router (Rule-Based)

> *"A Message Router inspects an incoming message, determines what channel to send it to, and forwards it to that channel. A rule-based router externalizes routing logic into a set of configurable rules."*

```
  ┌──────────────┐     ┌───────────────┐
  │  Incoming    │────▶│  Rule Engine   │
  │  Message     │     │               │
  └──────────────┘     │  Rule 1 (P=1) │──▶ Route to Topic A
                       │  Rule 2 (P=2) │──▶ Transform
                       │  Rule 3 (P=3) │──▶ Reject
                       │  Default      │──▶ DeadLetter
                       └───────────────┘
```

Rules are evaluated in priority order. The first match determines the action, decoupling business logic from code.

---

## Platform Implementation

### IRuleEngine

```csharp
// src/RuleEngine/IRuleEngine.cs
public interface IRuleEngine
{
    Task<RuleEvaluationResult> EvaluateAsync(
        IntegrationEnvelope<string> envelope,
        CancellationToken cancellationToken = default);
}
```

### BusinessRule

```csharp
// src/RuleEngine/BusinessRule.cs
public sealed record BusinessRule
{
    public required string Name { get; init; }
    public required int Priority { get; init; }
    public RuleLogicOperator LogicOperator { get; init; } = RuleLogicOperator.And;
    public required IReadOnlyList<RuleCondition> Conditions { get; init; }
    public required RuleAction Action { get; init; }
    public bool StopOnMatch { get; init; } = true;
    public bool Enabled { get; init; } = true;
}

public enum RuleLogicOperator { And, Or }
```

### RuleCondition and RuleConditionOperator

```csharp
// src/RuleEngine/RuleCondition.cs
public sealed record RuleCondition
{
    public required string FieldName { get; init; }
    public required RuleConditionOperator Operator { get; init; }
    public required string Value { get; init; }
}

public enum RuleConditionOperator
{
    Equals, Contains, Regex, In, GreaterThan
}
```

### RuleAction

```csharp
// src/RuleEngine/RuleAction.cs
public sealed record RuleAction
{
    public required RuleActionType ActionType { get; init; }
    public string? TargetTopic { get; init; }
    public string? TransformName { get; init; }
    public string? Reason { get; init; }
}

public enum RuleActionType
{
    Route,
    Transform,
    Reject,
    DeadLetter
}
```

### RuleEvaluationResult

```csharp
// src/RuleEngine/RuleEvaluationResult.cs
public sealed record RuleEvaluationResult(
    IReadOnlyList<BusinessRule> MatchedRules,
    IReadOnlyList<RuleAction> Actions,
    bool HasMatch,
    int RulesEvaluated);
```

### IRuleStore

```csharp
// src/RuleEngine/IRuleStore.cs
public interface IRuleStore
{
    Task<IReadOnlyList<BusinessRule>> GetAllAsync(CancellationToken ct = default);
    Task<BusinessRule?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddOrUpdateAsync(BusinessRule rule, CancellationToken ct = default);
    Task<bool> RemoveAsync(string name, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
```

`InMemoryRuleStore` holds rules in a `ConcurrentDictionary` sorted by `Priority`, suitable for development and tutorials.

---

## Scalability Dimension

The rule engine is **stateless** — it loads rules from the store and evaluates each message independently. Rules are cached in memory and refreshed periodically. Horizontal scaling is straightforward: add more consumer replicas sharing the same cached rule set.

---

## Atomicity Dimension

Rule evaluation happens **within the pipeline transaction**. If the selected action fails, the message is Nacked and retried. Rules are versioned — updating a rule does not affect in-flight messages. The `RulesEvaluated` counter provides observability into evaluation depth.

---

## Exercises

1. Write a `BusinessRule` that routes all messages from source `"PartnerX"` with `MessageType` containing `"order"` to topic `"orders-priority"`.

2. A rule has `LogicOperator = RuleLogicOperator.Or` with two conditions. Explain how evaluation differs from `And`.

3. Why does the platform evaluate rules in priority order and stop at the first match rather than evaluating all rules?

---

**Previous: [← Tutorial 29 — Throttle & Rate Limiting](29-throttle-rate-limiting.md)** | **Next: [Tutorial 31 — Event Sourcing →](31-event-sourcing.md)**
