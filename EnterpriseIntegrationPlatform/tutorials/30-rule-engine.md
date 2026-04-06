# Tutorial 30 — Rule Engine

Evaluate business rules with AND/OR condition logic against message fields.

## Key Types

```csharp
// src/RuleEngine/IRuleEngine.cs
public interface IRuleEngine
{
    Task<RuleEvaluationResult> EvaluateAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

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

## Exercises

### 1. Evaluate — SingleEqualsRule MatchesByMessageType

```csharp
await _store.AddOrUpdateAsync(new BusinessRule
{
    Name = "RouteOrders",
    Priority = 1,
    Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
    Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "orders-topic" },
});

var envelope = IntegrationEnvelope<string>.Create("data", "OrderService", "order.created");
var result = await _engine.EvaluateAsync(envelope);

Assert.That(result.HasMatch, Is.True);
Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("orders-topic"));
```

### 2. Evaluate — NoMatchingRule ReturnsNoMatch

```csharp
await _store.AddOrUpdateAsync(new BusinessRule
{
    Name = "RouteOrders",
    Priority = 1,
    Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
    Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "orders-topic" },
});

var envelope = IntegrationEnvelope<string>.Create("data", "PaymentService", "payment.received");
var result = await _engine.EvaluateAsync(envelope);

Assert.That(result.HasMatch, Is.False);
Assert.That(result.MatchedRules, Is.Empty);
Assert.That(result.Actions, Is.Empty);
```

### 3. Evaluate — ContainsOperator MatchesSubstring

```csharp
await _store.AddOrUpdateAsync(new BusinessRule
{
    Name = "AllOrders",
    Priority = 1,
    Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "order" }],
    Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "all-orders" },
});

var envelope = IntegrationEnvelope<string>.Create("data", "Service", "order.shipped");
var result = await _engine.EvaluateAsync(envelope);

Assert.That(result.HasMatch, Is.True);
Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("all-orders"));
```

### 4. Evaluate — AndLogic AllConditionsMustMatch

```csharp
await _store.AddOrUpdateAsync(new BusinessRule
{
    Name = "HighPriorityOrders",
    Priority = 1,
    LogicOperator = RuleLogicOperator.And,
    Conditions =
    [
        new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" },
        new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "PremiumService" },
    ],
    Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "premium-orders" },
});

// Only MessageType matches, Source doesn't → no match.
var envelope1 = IntegrationEnvelope<string>.Create("data", "BasicService", "order.created");
var result1 = await _engine.EvaluateAsync(envelope1);
Assert.That(result1.HasMatch, Is.False);

// Both match → match.
var envelope2 = IntegrationEnvelope<string>.Create("data", "PremiumService", "order.created");
var result2 = await _engine.EvaluateAsync(envelope2);
Assert.That(result2.HasMatch, Is.True);
```

### 5. Evaluate — OrLogic AnyConditionMatches

```csharp
await _store.AddOrUpdateAsync(new BusinessRule
{
    Name = "OrderOrPayment",
    Priority = 1,
    LogicOperator = RuleLogicOperator.Or,
    Conditions =
    [
        new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" },
        new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "payment.received" },
    ],
    Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "finance" },
});

var orderEnvelope = IntegrationEnvelope<string>.Create("data", "Service", "order.created");
var orderResult = await _engine.EvaluateAsync(orderEnvelope);
Assert.That(orderResult.HasMatch, Is.True);

var paymentEnvelope = IntegrationEnvelope<string>.Create("data", "Service", "payment.received");
var paymentResult = await _engine.EvaluateAsync(paymentEnvelope);
Assert.That(paymentResult.HasMatch, Is.True);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial30/Lab.cs`](../tests/TutorialLabs/Tutorial30/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial30.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial30/Exam.cs`](../tests/TutorialLabs/Tutorial30/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial30.Exam"
```

---

**Previous: [← Tutorial 29 — Throttle & Rate Limiting](29-throttle-rate-limiting.md)** | **Next: [Tutorial 31 — Event Sourcing →](31-event-sourcing.md)**
