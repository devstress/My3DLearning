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
    Task<RuleEvaluationResult> EvaluateAsync<T>(
        IntegrationEnvelope<T> envelope,
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

## Lab

**Objective:** Write business rules with conditions and logic operators, trace priority-based evaluation, and analyze rule caching for **scalable** high-throughput routing decisions.

### Step 1: Write a Priority-Based Business Rule

Write a `BusinessRule` that routes all messages from source `"PartnerX"` with `MessageType` containing `"order"` to topic `"orders-priority"`:

```csharp
var rule = new BusinessRule
{
    Name = "PartnerX-Orders",
    Priority = 1,
    LogicOperator = RuleLogicOperator.And,
    Conditions = [
        new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "PartnerX" },
        new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "order" }
    ],
    Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "orders-priority" }
};
```

Open `src/RuleEngine/BusinessRuleEngine.cs` and trace: How does `And` vs. `Or` logic change the evaluation?

### Step 2: Trace Priority-Based Evaluation

Rules are evaluated in priority order (lowest number = highest priority):

| Priority | Rule | Conditions |
|----------|------|-----------|
| 1 | Premium orders | Source = "PartnerX" AND Type contains "order" |
| 5 | All orders | Type contains "order" |
| 10 | Default | Always matches |

A message from `PartnerX` with type `"order.created"` matches rules at priorities 1 and 5. Which rule wins? Why does the engine stop at the first match? (hint: deterministic routing for **atomicity**)

### Step 3: Design Rule Caching for Scalability

At 50,000 messages/second with 100 rules, each message evaluates up to 100 conditions. Design a caching strategy:

- Rules change infrequently (hourly) but messages arrive constantly
- How does the platform cache compiled rules? (Open `src/RuleEngine/` to check)
- What is the cache invalidation strategy when rules are updated?
- What is the performance difference between cached vs. uncached rule evaluation?

## Exam

1. A rule engine has 3 rules with priorities 1, 5, 10. A message matches rules at priorities 5 and 10. Which rule is applied?
   - A) Both rules are applied (fan-out)
   - B) Priority 5 — the engine evaluates in priority order and stops at the first match, ensuring deterministic and **atomic** routing to exactly one destination
   - C) Priority 10 — the last match wins
   - D) The engine randomly selects one

2. Why does the rule engine use `And`/`Or` logic operators for conditions?
   - A) They're required by the .NET compiler
   - B) `And` requires all conditions to match (strict targeting); `Or` requires any condition to match (broad targeting) — this enables both precise and flexible routing rules for different business scenarios
   - C) Logic operators improve serialization performance
   - D) They're equivalent — both produce the same result

3. How does rule caching improve **throughput scalability**?
   - A) Caching stores message results, not rules
   - B) Compiled rules are cached in memory — avoiding repeated parsing and compilation of rule definitions for every message; since rules change infrequently but messages arrive at high volume, caching amortizes the compilation cost over millions of evaluations
   - C) Caching is only useful during testing
   - D) Rules are too small to benefit from caching

---

**Previous: [← Tutorial 29 — Throttle & Rate Limiting](29-throttle-rate-limiting.md)** | **Next: [Tutorial 31 — Event Sourcing →](31-event-sourcing.md)**
