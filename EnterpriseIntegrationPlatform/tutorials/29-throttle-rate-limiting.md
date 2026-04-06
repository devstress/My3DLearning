# Tutorial 29 — Throttle & Rate Limiting

## What You'll Learn

- How `IMessageThrottle` controls message flow rate inside the pipeline
- The `TokenBucketThrottle` algorithm: steady refill rate with burst capacity
- `IThrottleRegistry` for managing multiple throttle policies per partition key
- `ThrottlePolicy` with partition strategies: by TenantId, Queue, Endpoint, or Global
- `ThrottleMetrics` for monitoring throttle pressure and wait times
- The difference between **rate limiting** (HTTP 429) and **throttling** (pipeline delays)
- Per-tenant partitioning for fair resource sharing

---

## EIP Pattern: Throttler

> *"A Throttler limits the rate at which messages flow through a channel, protecting downstream systems from overload."*

```
  Producer ──▶ ┌──────────────────┐ ──▶ Consumer
               │   Token Bucket   │
               │  ┌─────────────┐ │
               │  │ ●●●○○○○○○○ │ │  ← tokens (● = available)
               │  └─────────────┘ │
               │  refill: 100/sec │
               │  burst:  200     │
               └──────────────────┘
                       │
               No token? → Delay or 429
```

The token bucket allows short bursts above the steady-state rate while enforcing a long-term average. Tokens are consumed per message and refilled at a constant rate.

---

## Platform Implementation

### IMessageThrottle

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

`AcquireAsync` returns a `ThrottleResult` indicating whether the message can proceed immediately, must wait, or should be rejected.

### TokenBucketThrottle (concrete)

```csharp
// src/Processing.Throttle/TokenBucketThrottle.cs
public sealed class TokenBucketThrottle : IMessageThrottle, IDisposable
{
    // Each partition key gets its own bucket
    // Tokens refill at policy.RefillRate per second
    // Bucket capacity = policy.BurstSize
}
```

### ThrottleResult

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

### ThrottlePartitionKey

```csharp
// src/Processing.Throttle/ThrottlePartitionKey.cs
public sealed record ThrottlePartitionKey
{
    public string? TenantId { get; init; }
    public string? Queue { get; init; }
    public string? Endpoint { get; init; }
}
```

| Key Property | Use Case |
|-------------|----------|
| `TenantId` | Fair share per tenant |
| `Queue` | Protect a specific queue |
| `Endpoint` | Throttle per downstream endpoint |

### IThrottleRegistry

```csharp
// src/Processing.Throttle/IThrottleRegistry.cs
public interface IThrottleRegistry
{
    IMessageThrottle Resolve(ThrottlePartitionKey key);
    void SetPolicy(ThrottlePolicy policy);
    bool RemovePolicy(string policyId);
    IReadOnlyList<ThrottlePolicyStatus> GetAllPolicies();
    ThrottlePolicyStatus? GetPolicy(string policyId);
}
```

### ThrottleMetrics

```csharp
// src/Processing.Throttle/ThrottleMetrics.cs
public sealed record ThrottleMetrics
{
    public required long TotalAcquired { get; init; }
    public required long TotalRejected { get; init; }
    public required int AvailableTokens { get; init; }
    public required int BurstCapacity { get; init; }
    public required int RefillRate { get; init; }
    public required TimeSpan TotalWaitTime { get; init; }
}
```

### Rate Limiting vs Throttling

| Aspect | Rate Limiting (Gateway) | Throttling (Pipeline) |
|--------|------------------------|----------------------|
| Location | HTTP ingress (Gateway.Api) | Internal pipeline stage |
| Response | HTTP 429 Too Many Requests | Delay + delivery |
| Effect | Rejects message | Slows message down |
| Visibility | Client sees rejection | Client sees slower response |

Rate limiting protects the **platform** from external overload. Throttling protects **downstream systems** from internal overload.

---

## Scalability Dimension

Per-tenant partitioning (via `ThrottlePartitionKey.TenantId`) ensures one noisy tenant cannot consume all throughput. Each tenant's bucket is independent. The `ThrottleMetrics` feed into the competing consumers orchestrator (Tutorial 28): if wait times climb, the orchestrator adds consumers.

---

## Atomicity Dimension

When `AcquireAsync` delays a message, the message remains **uncommitted** — not Acked until it passes the throttle. If the service restarts during a wait, the broker redelivers. The `MaxWait` timeout prevents indefinite blocking; if exceeded, the message is Nacked and retried (Tutorial 24).

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial29/Lab.cs`](../tests/TutorialLabs/Tutorial29/Lab.cs)

**Objective:** Design throttle policies for multi-tenant rate limiting, trace the token bucket algorithm, and analyze why per-tenant throttling is essential for **fair scalability**.

### Step 1: Design a Multi-Tenant Throttle Policy

Design a `ThrottlePolicy` for a partner system:

| Parameter | Value | Purpose |
|-----------|-------|---------|
| Rate | 50 messages/second | Sustained throughput |
| Burst | 200 messages | Peak absorption |
| `PartitionKey.TenantId` | `"partner-x"` | Per-tenant isolation |
| `MaxWait` | 5 seconds | Max time to wait for a token |

Open `src/Processing.Throttle/` and verify: How does the `TokenBucketThrottle` implement this? What happens when all 200 burst tokens are consumed?

### Step 2: Trace Token Exhaustion

The `TokenBucketThrottle` has 0 available tokens and `MaxWait = 5s`. A message arrives:

1. The throttle checks: 0 tokens available
2. Waits for token replenishment (50 tokens/second = 1 token every 20ms)
3. After ~20ms, 1 token becomes available → message proceeds
4. If no token is available after 5 seconds → what happens?

What is the maximum queuing depth during a sustained burst? How does `MaxWait` prevent unbounded queue growth?

### Step 3: Analyze Per-Tenant vs. Global Throttling

Why does the platform use per-`TenantId` throttling by default?

| Scenario | Global Throttle | Per-Tenant Throttle |
|----------|----------------|-------------------|
| Tenant A sends 10,000 msg/s | Blocks Tenant B too | Only Tenant A is throttled |
| Tenant B sends 10 msg/s | May be blocked by A | Always gets through |
| Fair resource allocation | No guarantee | Each tenant gets its quota |

How does per-tenant throttling prevent the **noisy neighbor** problem? Why is this critical for **multi-tenant scalability**?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial29/Exam.cs`](../tests/TutorialLabs/Tutorial29/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 28 — Competing Consumers](28-competing-consumers.md)** | **Next: [Tutorial 30 — Rule Engine →](30-rule-engine.md)**
