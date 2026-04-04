# Tutorial 30 — Rule Engine

## What You'll Learn

- How `IRuleEngine` evaluates business rules against integration messages
- `BusinessRuleEngine` with priority-ordered evaluation
- `IRuleStore` for persisting and querying rules at runtime
- `BusinessRule` with conditions (AND/OR) and actions (Route, Transform, Enrich, Reject, Notify, Store)
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
                       │  Rule 2 (P=2) │──▶ Transform + Enrich
                       │  Rule 3 (P=3) │──▶ Reject
                       │  Default      │──▶ Store
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
public sealed class BusinessRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int Priority { get; init; }
    public required ConditionGroup Conditions { get; init; }
    public required RuleAction Action { get; init; }
    public bool IsEnabled { get; init; } = true;
}

public sealed class ConditionGroup
{
    public LogicalOperator Operator { get; init; } = LogicalOperator.And;
    public required IReadOnlyList<RuleCondition> Conditions { get; init; }
}

public enum LogicalOperator { And, Or }
```

### RuleCondition and RuleConditionOperator

```csharp
// src/RuleEngine/RuleCondition.cs
public sealed record RuleCondition(
    string Field,
    RuleConditionOperator Operator,
    string Value);

public enum RuleConditionOperator
{
    Equals, NotEquals, Contains, StartsWith,
    EndsWith, GreaterThan, LessThan, Regex, Exists
}
```

### RuleAction

```csharp
// src/RuleEngine/RuleAction.cs
public sealed record RuleAction(RuleActionType Type, IDictionary<string, string>? Parameters = null);

public enum RuleActionType
{
    Route,
    Transform,
    Enrich,
    Reject,
    Notify,
    Store
}
```

### RuleEvaluationResult

```csharp
// src/RuleEngine/RuleEvaluationResult.cs
public sealed record RuleEvaluationResult(bool Matched, BusinessRule? MatchedRule, RuleAction? SelectedAction, int RulesEvaluated);
```

### IRuleStore

```csharp
// src/RuleEngine/IRuleStore.cs
public interface IRuleStore
{
    Task<IReadOnlyList<BusinessRule>> GetActiveRulesAsync(CancellationToken ct);
    Task AddAsync(BusinessRule rule, CancellationToken ct);
    Task RemoveAsync(string ruleId, CancellationToken ct);
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

2. A rule has `ConditionGroup.Operator = Or` with two conditions. Explain how evaluation differs from `And`.

3. Why does the platform evaluate rules in priority order and stop at the first match rather than evaluating all rules?

---

**Previous: [← Tutorial 29 — Throttle & Rate Limiting](29-throttle-rate-limiting.md)** | **Next: [Tutorial 31 — Event Sourcing →](31-event-sourcing.md)**
