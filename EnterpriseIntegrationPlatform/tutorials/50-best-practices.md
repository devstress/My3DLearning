# Tutorial 50 — Best Practices & Design Guidelines

Apply design guidelines, avoid anti-patterns, and verify production readiness.

## Learning Objectives

After completing this tutorial you will be able to:

1. Enforce message expiration and skip expired messages during publishing
2. Sanitise input idempotently so repeated calls yield the same result
3. Resolve tenant identity and handle null/anonymous tenants
4. Round-trip metadata through published envelopes
5. Verify default schema versioning on integration envelopes

---

## Key Types

```csharp
// src/Contracts/IntegrationEnvelope.cs — the universal message wrapper
public record IntegrationEnvelope<T>
{
    public Guid MessageId { get; init; }
    public string Source { get; init; }
    public string MessageType { get; init; }
    public T Payload { get; init; }
    public string SchemaVersion { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}

// src/Security/InputSanitizer.cs — strips dangerous content
// src/MultiTenancy/TenantResolver.cs — resolves tenant from context
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `ExpiredMessage_NotPublished` | Expired message is not published |
| 2 | `ValidMessage_Published` | Valid (non-expired) message is published |
| 3 | `InputSanitizer_Idempotent` | Sanitiser is idempotent |
| 4 | `TenantResolver_NullTenantId_ReturnsAnonymous` | Null tenant returns Anonymous |
| 5 | `MessageHeaders_ReplayId_ConstantExists` | ReplayId header constant exists |
| 6 | `Metadata_RoundTrip_PublishedWithEnvelope` | Metadata round-trip through broker |
| 7 | `SchemaVersion_DefaultsTo1` | Schema version defaults to 1.0 |

> 💻 [`tests/TutorialLabs/Tutorial50/Lab.cs`](../tests/TutorialLabs/Tutorial50/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial50.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_SecurityTenancyFlow_EndToEnd` | 🟢 Starter | Security + tenancy flow end-to-end |
| 2 | `Challenge2_ExpirationPriority_ProcessesOnlyValid` | 🟡 Intermediate | Expiration + priority — process only valid |
| 3 | `Challenge3_CrossCuttingFlow_SanitizeTenantPublish` | 🔴 Advanced | Cross-cutting flow: sanitise → tenant → publish |

> 💻 [`tests/TutorialLabs/Tutorial50/Exam.cs`](../tests/TutorialLabs/Tutorial50/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial50.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial50.ExamAnswers"
```

---

**Previous: [← Tutorial 49](49-testing-integrations.md)** | **[Back to Course Overview →](README.md)**
