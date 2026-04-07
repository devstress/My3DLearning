# Tutorial 12 — Recipient List

Fan-out routing that sends the same message to every resolved destination, with rule-based and metadata-based recipient resolution and deduplication.

---

## Learning Objectives

1. Configure a `RecipientListRouter` with one or more `RecipientListRule` entries to fan-out messages to multiple destinations
2. Understand how multiple matching rules combine their destination lists into a single set
3. Verify that duplicate destinations across rules are automatically deduplicated
4. Use metadata-based recipient resolution via `MetadataRecipientsKey` as an alternative to static rules
5. Apply regex-based routing operators for flexible pattern matching on envelope fields
6. Distinguish between resolved count, destination list, and duplicates-removed in `RecipientListResult`

---

## Key Types

```csharp
// src/Processing.Routing/IRecipientList.cs
public interface IRecipientList
{
    Task<RecipientListResult> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}

// src/Processing.Routing/RecipientListResult.cs
public sealed record RecipientListResult(
    IReadOnlyList<string> Destinations,
    int ResolvedCount,
    int DuplicatesRemoved);

// src/Processing.Routing/RecipientListRule.cs (used in RecipientListOptions)
public sealed class RecipientListRule
{
    public string FieldName { get; set; }
    public RoutingOperator Operator { get; set; }
    public string Value { get; set; }
    public List<string> Destinations { get; set; }
    public string? Name { get; set; }
}
```

---

## Lab — Guided Practice

> 💻 Run each test in order to see how the Recipient List pattern fans out messages to multiple destinations. Each test isolates one concept — read the assertions, predict the outcome, then run.

| # | Test | Concept |
|---|------|---------|
| 1 | `Route_SingleRuleMatch_FansOutToAllDestinations` | Basic single-rule fan-out to three destinations |
| 2 | `Route_NoRuleMatch_ReturnsEmptyResult` | No matching rule produces an empty result |
| 3 | `Route_MultipleRulesMatch_CombinesDestinations` | Multiple matching rules combine their destination lists |
| 4 | `Route_DuplicateDestinations_AreDeduplicated` | Overlapping destinations across rules are deduplicated |
| 5 | `Route_MetadataRecipients_AddsDestinations` | Metadata-based recipient resolution via envelope metadata key |
| 6 | `Route_RegexRule_MatchesPattern` | Regex operator matches message-type patterns |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial12.Lab"
```

---

## Exam — Assessment Challenges

> 💻 Prove you can apply the Recipient List pattern in realistic, multi-constraint scenarios. Each challenge combines concepts from the lab into an end-to-end flow.

| Challenge | Test | Difficulty |
|-----------|------|------------|
| 1 | `Starter_EventNotificationFanOut_RoutesToAllSubscribers` | 🟢 Starter |
| 2 | `Intermediate_RulesAndMetadataCombined_MergesDestinations` | 🟡 Intermediate |
| 3 | `Advanced_CrossRuleDedup_RemovesDuplicateDestinations` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial12.Exam"
```

---

**Previous: [← Tutorial 11 — Dynamic Router](11-dynamic-router.md)** | **Next: [Tutorial 13 — Routing Slip →](13-routing-slip.md)**
