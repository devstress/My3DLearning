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

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Throttle pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_BurstExhaustion_PermittedThenRejected` | 🟢 Starter |
| 2 | `Intermediate_MetricAccumulation_TracksAllOperations` | 🟡 Intermediate |
| 3 | `Advanced_SingleToken_AlternatePermitReject` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial29.Exam"
```

---

**Previous: [← Tutorial 28 — Competing Consumers](28-competing-consumers.md)** | **Next: [Tutorial 30 — Rule Engine →](30-rule-engine.md)**
