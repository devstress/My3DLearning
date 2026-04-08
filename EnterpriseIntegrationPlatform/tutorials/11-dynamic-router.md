# Tutorial 11 — Dynamic Router

A router whose routing table is built at runtime as downstream participants register and unregister through a control channel.

---

## Learning Objectives

1. Understand how a Dynamic Router builds its routing table at runtime via a control channel
2. Register and unregister routes so that messages reach the correct destination topic
3. Verify fallback behaviour when no route matches an incoming message
4. Inspect the routing table snapshot to confirm registration state
5. Apply case-insensitive matching so condition keys are compared without regard to case
6. Combine multiple participants, route replacement, and case-insensitive routing in end-to-end scenarios

---

## Key Types

```csharp
// src/Processing.Routing/IDynamicRouter.cs
public interface IDynamicRouter
{
    Task<DynamicRoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}

// src/Processing.Routing/IRouterControlChannel.cs
public interface IRouterControlChannel
{
    Task RegisterAsync(
        string conditionKey,
        string destination,
        string? participantId = null,
        CancellationToken cancellationToken = default);

    Task<bool> UnregisterAsync(
        string conditionKey,
        CancellationToken cancellationToken = default);

    IReadOnlyDictionary<string, DynamicRouteEntry> GetRoutingTable();
}

// src/Processing.Routing/DynamicRoutingDecision.cs
public sealed record DynamicRoutingDecision(
    string Destination,
    DynamicRouteEntry? MatchedEntry,
    bool IsFallback,
    string? ConditionValue);

// src/Processing.Routing/DynamicRouteEntry.cs
public sealed record DynamicRouteEntry(
    string ConditionKey,
    string Destination,
    string? ParticipantId,
    DateTimeOffset RegisteredAtUtc);
```

---

## Lab — Guided Practice

> 💡 The Lab lets you run pre-written tests one at a time so you can observe each
> Dynamic Router concept in isolation. Read each test, predict the outcome,
> then run it to confirm your understanding.

| # | Test | Concept |
|---|------|---------|
| 1 | `Route_RegisteredKey_RoutesToDestination` | Register a route and verify a matching message reaches the correct topic |
| 2 | `Route_UnregisteredKey_FallsBackToDefault` | Unmatched condition key triggers fallback routing |
| 3 | `Route_NoMatchNoFallback_ThrowsInvalidOperation` | Missing fallback topic causes an exception |
| 4 | `Register_UpdatesExistingRoute` | Re-registering the same key replaces the previous destination |
| 5 | `Unregister_RemovesRoute_FallsBack` | Unregistering a key removes it so subsequent messages fall back |
| 6 | `Unregister_NonExistentKey_ReturnsFalse` | Unregistering a key that was never registered returns false |
| 7 | `GetRoutingTable_ReturnsSnapshot` | Inspecting the routing table after registrations |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial11.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_MultiParticipantTopology_RoutesCorrectly` | 🟢 Starter | MultiParticipantTopology — RoutesCorrectly |
| 2 | `Intermediate_RouteReplacement_NewParticipantOverrides` | 🟡 Intermediate | RouteReplacement — NewParticipantOverrides |
| 3 | `Advanced_CaseInsensitive_MatchesRegardlessOfCase` | 🔴 Advanced | CaseInsensitive — MatchesRegardlessOfCase |

> 💻 [`tests/TutorialLabs/Tutorial11/Exam.cs`](../tests/TutorialLabs/Tutorial11/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial11.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial11.ExamAnswers"
```
---

**Previous: [← Tutorial 10 — Message Filter](10-message-filter.md)** | **Next: [Tutorial 12 — Recipient List →](12-recipient-list.md)**
