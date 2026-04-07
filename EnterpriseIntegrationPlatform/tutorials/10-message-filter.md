# Tutorial 10 — Message Filter

Predicate-based filtering with `IMessageFilter`, `MessageFilterResult`, `MessageFilterOptions`, and `RuleCondition`.

## Learning Objectives

After completing this tutorial you will be able to:

1. Configure accept/reject filter conditions using `RuleCondition` operators
2. Route accepted messages to an output topic and rejected messages to a discard topic
3. Handle pass-through behavior when no conditions are configured
4. Implement silent discard when no discard topic is specified
5. Use the `In` operator to match against comma-separated value lists
6. Combine multiple conditions with `And` / `Or` logic operators

## Key Types

```csharp
// src/Processing.Routing/IMessageFilter.cs
public interface IMessageFilter
{
    Task<MessageFilterResult> FilterAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Routing/MessageFilterResult.cs
public sealed record MessageFilterResult(
    bool Passed,
    string? OutputTopic,
    string Reason);
```

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

```csharp
// src/RuleEngine/RuleCondition.cs
public sealed record RuleCondition
{
    public required string FieldName { get; init; }
    public required RuleConditionOperator Operator { get; init; }
    public required string Value { get; init; }
}
```

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how the Message Filter evaluates
> accept/reject conditions through real NATS JetStream via Aspire. Read the code
> and comments to understand each concept before moving to the Exam.

| # | Test | Concept |
|---|------|---------|
| 1 | `Filter_Accept_PublishesToOutputTopic` | Accepted message published to output topic |
| 2 | `Filter_Reject_PublishesToDiscardTopic` | Rejected message published to discard topic |
| 3 | `Filter_NoConditions_PassThrough` | No conditions — everything passes through |
| 4 | `Filter_SilentDiscard_NoPublishWhenNoDiscardTopic` | Silent discard when no discard topic set |
| 5 | `Filter_BySource_AcceptsAndRejects` | Source-based filtering with accept/reject |
| 6 | `Filter_InOperator_MatchesAnyOfCommaSeparatedValues` | In operator matches comma-separated values |
| 7 | `Filter_OrLogic_EitherConditionSuffices` | Or logic — either condition suffices |

> 💻 [`tests/TutorialLabs/Tutorial10/Lab.cs`](../tests/TutorialLabs/Tutorial10/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial10.Lab"
```

---

## Exam — Assessment Challenges

> **Purpose:** Prove you can apply message filter patterns in realistic scenarios —
> spam filtering, priority-based triage, and multi-condition metadata filtering.

| Difficulty | Challenge | What you prove |
|------------|-----------|---------------|
| 🟢 Starter | `Starter_SpamFilter_AcceptsTrustedRejectsOthers` | Filter trusted partners using In operator, quarantine others |
| 🟡 Intermediate | `Intermediate_PriorityFilter_OnlyHighCriticalPass` | Priority-based triage — only High/Critical pass through |
| 🔴 Advanced | `Advanced_MetadataFilter_AndLogic_BothConditionsRequired` | Multi-condition AND filter on tenant and environment metadata |

> 💻 [`tests/TutorialLabs/Tutorial10/Exam.cs`](../tests/TutorialLabs/Tutorial10/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial10.Exam"
```

---

**Previous: [← Tutorial 09 — Content-Based Router](09-content-based-router.md)** | **Next: [Tutorial 11 — Dynamic Router →](11-dynamic-router.md)**
