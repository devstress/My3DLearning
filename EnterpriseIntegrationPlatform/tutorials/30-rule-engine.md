# Tutorial 30 — Rule Engine

Evaluate business rules with AND/OR condition logic against message fields.

---

## Learning Objectives

1. Understand the Rule Engine pattern for evaluating business rules against messages
2. Use `BusinessRuleEngine` to evaluate conditions and produce routing actions
3. Configure rules with `Equals`, `Contains`, `In`, and other condition operators
4. Verify AND/OR logic operators for multi-condition rules
5. Confirm priority ordering and `StopOnMatch` behavior across multiple rules
6. Validate metadata field matching and disabled rule skipping

---

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

## Lab — Guided Practice

> 💻 Run the lab tests to see each Rule Engine concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Evaluate_MatchingRule_ReturnsMatch` | Matching rule returns match with action |
| 2 | `Evaluate_NoMatch_ReturnsEmpty` | No matching rule returns empty result |
| 3 | `Evaluate_ContainsOperator_MatchesSubstring` | Contains operator matches substring |
| 4 | `Evaluate_MetadataCondition_MatchesMetadataField` | Metadata field condition matching |
| 5 | `Evaluate_DisabledRule_IsSkipped` | Disabled rules are skipped |
| 6 | `Evaluate_PriorityOrder_HigherPriorityWins` | Higher priority rule wins with StopOnMatch |
| 7 | `Evaluate_OrLogic_MatchesAnyCondition` | OR logic matches any condition |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial30.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_MultiRuleEvaluation_CollectsAllMatches` | 🟢 Starter | MultiRuleEvaluation — CollectsAllMatches |
| 2 | `Intermediate_RejectAction_NoRouting` | 🟡 Intermediate | RejectAction — NoRouting |
| 3 | `Advanced_InOperator_MatchesCommaList` | 🔴 Advanced | InOperator — MatchesCommaList |

> 💻 [`tests/TutorialLabs/Tutorial30/Exam.cs`](../tests/TutorialLabs/Tutorial30/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial30.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial30.ExamAnswers"
```
---

**Previous: [← Tutorial 29 — Throttle & Rate Limiting](29-throttle-rate-limiting.md)** | **Next: [Tutorial 31 — Event Sourcing →](31-event-sourcing.md)**
