# Tutorial 29 — Throttle & Rate Limiting

Control message throughput with token-bucket rate limiting and throttling.

---

## Learning Objectives

1. Understand the Throttle pattern and token-bucket rate limiting for message throughput
2. Use `TokenBucketThrottle` to acquire tokens and control per-second message rates
3. Verify that burst capacity limits are enforced and tokens decrement on acquisition
4. Configure reject-on-backpressure mode to immediately reject when tokens are exhausted
5. Inspect `ThrottleMetrics` to track acquired, rejected, and wait-time statistics

---

## Key Types

```csharp
// src/Processing.Throttle/IMessageThrottle.cs
public interface IMessageThrottle
{
    Task<ThrottleResult> AcquireAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken ct = default);

    int AvailableTokens { get; }

    ThrottleMetrics GetMetrics();
}
```

```csharp
// src/Processing.Throttle/TokenBucketThrottle.cs
public sealed class TokenBucketThrottle : IMessageThrottle, IDisposable
{
    // Each partition key gets its own bucket
    // Tokens refill at policy.RefillRate per second
    // Bucket capacity = policy.BurstSize
}
```

```csharp
// src/Processing.Throttle/ThrottleResult.cs
public sealed record ThrottleResult
{
    public required bool Permitted { get; init; }
    public required TimeSpan WaitTime { get; init; }
    public required int RemainingTokens { get; init; }
    public string? RejectionReason { get; init; }
}
```

```csharp
// src/Processing.Throttle/ThrottlePartitionKey.cs
public sealed record ThrottlePartitionKey
{
    public string? TenantId { get; init; }
    public string? Queue { get; init; }
    public string? Endpoint { get; init; }
}
```

## Lab — Guided Practice

> 💻 Run the lab tests to see each Throttle concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Acquire_WithTokens_IsPermitted` | Available tokens permit acquisition |
| 2 | `Acquire_ExhaustsTokens_StillPermittedUntilEmpty` | Tokens exhaust sequentially until empty |
| 3 | `Acquire_RejectOnBackpressure_RejectsWhenEmpty` | Reject mode rejects when tokens exhausted |
| 4 | `AvailableTokens_DecrementsOnAcquire` | Token count decrements on each acquire |
| 5 | `GetMetrics_ReflectsAcquireAndReject` | Metrics track acquires and rejects |
| 6 | `GetMetrics_RefillRate_MatchesConfig` | Refill rate matches configuration |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial29.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_BurstExhaustion_PermittedThenRejected` | 🟢 Starter | BurstExhaustion — PermittedThenRejected |
| 2 | `Intermediate_MetricAccumulation_TracksAllOperations` | 🟡 Intermediate | MetricAccumulation — TracksAllOperations |
| 3 | `Advanced_SingleToken_AlternatePermitReject` | 🔴 Advanced | SingleToken — AlternatePermitReject |

> 💻 [`tests/TutorialLabs/Tutorial29/Exam.cs`](../tests/TutorialLabs/Tutorial29/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial29.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial29.ExamAnswers"
```
---

**Previous: [← Tutorial 28 — Competing Consumers](28-competing-consumers.md)** | **Next: [Tutorial 30 — Rule Engine →](30-rule-engine.md)**
