# Tutorial 09 — Content-Based Router

Priority-ordered routing rules with `IContentBasedRouter`, `RoutingRule`, `RoutingOperator`, and `RoutingDecision`.

## Learning Objectives

After completing this tutorial you will be able to:

1. Configure routing rules with Equals, Contains, StartsWith, and Regex operators
2. Route messages to specific topics based on MessageType, Source, or Metadata fields
3. Fall back to a default topic when no routing rule matches
4. Understand priority ordering — lower priority numbers are evaluated first
5. Inspect `RoutingDecision` metadata including the matched rule details

## Key Types

```csharp
// src/Processing.Routing/IContentBasedRouter.cs
public interface IContentBasedRouter
{
    Task<RoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Routing/RoutingRule.cs
public sealed record RoutingRule
{
    public required int Priority { get; init; }
    public required string FieldName { get; init; }
    public required RoutingOperator Operator { get; init; }
    public required string Value { get; init; }
    public required string TargetTopic { get; init; }
    public string? Name { get; init; }
}
```

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

```csharp
// src/Processing.Routing/RoutingDecision.cs
public sealed record RoutingDecision(
    string TargetTopic,
    RoutingRule? MatchedRule,
    bool IsDefault);
```

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how the Content-Based Router evaluates
> routing rules with different operators through real NATS JetStream via Aspire.
> Read the code and comments to understand each concept before moving to the Exam.

| # | Test | Concept |
|---|------|---------|
| 1 | `Route_Equals_MatchesMessageType` | Equals operator matches MessageType exactly |
| 2 | `Route_Contains_MatchesMetadataSubstring` | Contains operator matches metadata substring |
| 3 | `Route_StartsWith_MatchesSourcePrefix` | StartsWith operator matches Source prefix |
| 4 | `Route_Regex_MatchesPattern` | Regex operator matches MessageType pattern |
| 5 | `Route_NoMatch_FallsToDefaultTopic` | No match falls back to default topic |
| 6 | `Route_Priority_LowerNumberEvaluatedFirst` | Priority ordering — lower number wins |
| 7 | `Route_MatchedRule_ContainsAllRuleDetails` | RoutingDecision exposes full matched rule |

> 💻 [`tests/TutorialLabs/Tutorial09/Lab.cs`](../tests/TutorialLabs/Tutorial09/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial09.Lab"
```

---

## Exam — Assessment Challenges

> **Purpose:** Prove you can apply content-based routing in realistic scenarios —
> regional routing, payload-based routing with JSON, and batch verification.

| Difficulty | Challenge | What you prove |
|------------|-----------|---------------|
| 🟢 Starter | `Starter_RegionalRouting_MatchesAndFallsBack` | Multi-rule regional routing with fallback to global |
| 🟡 Intermediate | `Intermediate_PayloadRouting_JsonElementField` | Route by JSON payload fields using JsonElement |
| 🔴 Advanced | `Advanced_BatchRouting_MultipleMessagesVerifyTopics` | Batch routing with per-topic count verification |

> 💻 [`tests/TutorialLabs/Tutorial09/Exam.cs`](../tests/TutorialLabs/Tutorial09/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial09.Exam"
```

---

**Previous: [← Tutorial 08 — Activities and the Pipeline](08-activities-pipeline.md)** | **Next: [Tutorial 10 — Message Filter →](10-message-filter.md)**
