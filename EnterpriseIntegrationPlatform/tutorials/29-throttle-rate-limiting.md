# Tutorial 29 — Throttle & Rate Limiting

## What You'll Learn

- How `IMessageThrottle` controls message flow rate inside the pipeline
- The `TokenBucketThrottle` algorithm: steady refill rate with burst capacity
- `IThrottleRegistry` for managing multiple throttle policies per partition key
- `ThrottlePolicy` with partition strategies: by Source, Recipient, CorrelationId, or Global
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
public sealed class TokenBucketThrottle : IMessageThrottle
{
    // Each partition key gets its own bucket
    // Tokens refill at policy.RefillRate per second
    // Bucket capacity = policy.BurstSize
}
```

### ThrottleResult

```csharp
// src/Processing.Throttle/ThrottleResult.cs
public sealed record ThrottleResult(
    bool Permitted,
    TimeSpan WaitTime,
    double RemainingTokens,
    string? RejectionReason = null);
```

### ThrottlePartitionKey

```csharp
// src/Processing.Throttle/ThrottlePartitionKey.cs
public sealed record ThrottlePartitionKey(
    string? TenantId = null,
    string? Queue = null,
    string? Endpoint = null);
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
public sealed record ThrottleMetrics(
    long TotalAcquired,
    long TotalRejected,
    double AvailableTokens,
    double BurstCapacity,
    double RefillRate,
    TimeSpan TotalWaitTime);
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

## Exercises

1. Design a `ThrottlePolicy` that allows a partner system to send 50 messages/second with bursts up to 200. Which `ThrottlePartitionKey` fields would you set?

2. The `TokenBucketThrottle` has 0 available tokens and a `MaxWait` of 5 seconds. A message arrives. Describe the sequence of events.

3. Explain why the platform uses per-`TenantId` throttling by default for multi-tenant deployments rather than a single global throttle.

---

**Previous: [← Tutorial 28 — Competing Consumers](28-competing-consumers.md)** | **Next: [Tutorial 30 — Rule Engine →](30-rule-engine.md)**
