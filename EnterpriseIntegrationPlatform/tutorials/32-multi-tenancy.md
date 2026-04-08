# Tutorial 32 — Multi-Tenancy

Isolate message processing per tenant with tenant resolution and context propagation.

## Learning Objectives

After completing this tutorial you will be able to:

1. Resolve a `TenantContext` from envelope metadata using `ITenantResolver`
2. Handle missing or null tenant identifiers and receive the `Anonymous` sentinel
3. Enforce cross-tenant isolation with `ITenantIsolationGuard`
4. Detect tenant mismatches that throw `TenantIsolationException` with diagnostic properties
5. Propagate tenant identity through integration message metadata

## Key Types

```csharp
// src/MultiTenancy/ITenantResolver.cs
public interface ITenantResolver
{
    TenantContext Resolve(IReadOnlyDictionary<string, string> metadata);
    TenantContext Resolve(string? tenantId);
}
```

```csharp
// src/MultiTenancy/TenantContext.cs
public sealed class TenantContext
{
    public required string TenantId { get; init; }
    public string? TenantName { get; init; }
    public bool IsResolved { get; init; }

    public static readonly TenantContext Anonymous = new()
    {
        TenantId = "anonymous",
        IsResolved = false,
    };
}
```

```csharp
// src/MultiTenancy/ITenantIsolationGuard.cs
public interface ITenantIsolationGuard
{
    void Enforce<T>(IntegrationEnvelope<T> envelope, string expectedTenantId);
}
```

```csharp
// src/MultiTenancy/TenantIsolationException.cs
public sealed class TenantIsolationException : Exception
{
    public Guid MessageId { get; }
    public string? ActualTenantId { get; }
    public string ExpectedTenantId { get; }
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `ResolveFromMetadata_ReturnsTenantContext` | Resolve tenant from metadata dictionary |
| 2 | `ResolveFromMetadata_MissingKey_ReturnsAnonymous` | Missing key returns Anonymous sentinel |
| 3 | `ResolveFromString_ReturnsTenantContext` | Resolve tenant from explicit string |
| 4 | `ResolveFromString_NullOrWhitespace_ReturnsAnonymous` | Null/whitespace string returns Anonymous |
| 5 | `IsolationGuard_MatchingTenant_DoesNotThrow` | Guard passes when tenant matches |
| 6 | `IsolationGuard_MismatchedTenant_ThrowsAndPublishesAlert` | Guard throws on mismatch and publishes alert |

> 💻 [`tests/TutorialLabs/Tutorial32/Lab.cs`](../tests/TutorialLabs/Tutorial32/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial32.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_MultiTenantRouting_IsolatesPerTenant` | 🟢 Starter | Multi-tenant routing with per-tenant isolation |
| 2 | `Challenge2_CrossTenantAccess_Rejected` | 🟡 Intermediate | Cross-tenant access rejection |
| 3 | `Challenge3_AnonymousTenant_GuardRejects` | 🔴 Advanced | Anonymous tenant guard rejection logic |

> 💻 [`tests/TutorialLabs/Tutorial32/Exam.cs`](../tests/TutorialLabs/Tutorial32/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial32.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial32.ExamAnswers"
```

---

**Previous: [← Tutorial 31 — Event Sourcing](31-event-sourcing.md)** | **Next: [Tutorial 33 — Security →](33-security.md)**
