# Tutorial 24 — Retry Framework

## What You'll Learn

- How retries protect against transient failures in distributed systems
- How `IRetryPolicy` and `ExponentialBackoffRetryPolicy` implement retry logic
- `RetryOptions`: MaxAttempts, InitialDelayMs, BackoffMultiplier, MaxDelayMs, UseJitter
- `RetryResult` with success/failure, attempt count, and last exception
- Why non-retryable errors should bypass retries and go straight to the DLQ

---

## EIP Pattern: Retry

> In distributed integration, transient failures (network blips, temporary overloads, leader elections) are the norm, not the exception. A retry policy wraps any operation and re-executes it with increasing delays until it succeeds or the maximum attempts are exhausted.

```
  Attempt 1 ──▶ FAIL ──▶ wait 1 s
  Attempt 2 ──▶ FAIL ──▶ wait 2 s
  Attempt 3 ──▶ FAIL ──▶ wait 4 s
  Attempt 4 ──▶ SUCCESS ✓
         └── exponential backoff with jitter
```

---

## Platform Implementation

### IRetryPolicy

```csharp
// src/Processing.Retry/IRetryPolicy.cs
public interface IRetryPolicy
{
    Task<RetryResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct);

    Task<RetryResult<bool>> ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken ct);
}
```

### ExponentialBackoffRetryPolicy

```csharp
// src/Processing.Retry/ExponentialBackoffRetryPolicy.cs (key logic)
public async Task<RetryResult<T>> ExecuteAsync<T>(
    Func<CancellationToken, Task<T>> operation, CancellationToken ct)
{
    for (int attempt = 1; attempt <= _options.MaxAttempts; attempt++)
    {
        try
        {
            var result = await operation(ct);
            return new RetryResult<T>
                { IsSucceeded = true, Attempts = attempt, Result = result };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // honour cancellation immediately
        }
        catch (Exception ex)
        {
            if (attempt < _options.MaxAttempts)
                await Task.Delay(ComputeDelay(attempt), ct);
        }
    }
    return new RetryResult<T>
        { IsSucceeded = false, Attempts = _options.MaxAttempts, LastException = ... };
}
```

### RetryOptions

```csharp
// src/Processing.Retry/RetryOptions.cs
public sealed class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 30000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseJitter { get; set; } = true;
}
```

### RetryResult

```csharp
// src/Processing.Retry/RetryResult.cs
public record RetryResult<T>
{
    public required bool IsSucceeded { get; init; }
    public required int Attempts { get; init; }
    public Exception? LastException { get; init; }
    public T? Result { get; init; }
}
```

### Delay Calculation

```
delay = min(InitialDelayMs × BackoffMultiplier^(attempt-1), MaxDelayMs)
jitter = ±20% random variation (when UseJitter = true)
```

Jitter prevents the **thundering herd** problem where many retrying clients hit the same service simultaneously at the exact same backoff intervals.

---

## Scalability Dimension

The retry policy is **per-operation, in-process** — each replica independently retries its own failed operations. No coordination between replicas is needed. However, aggressive retry settings (high `MaxAttempts`, low `InitialDelayMs`) can amplify load on a struggling downstream service. The `MaxDelayMs` cap and jitter work together to spread retry traffic and protect downstream services.

---

## Atomicity Dimension

When all retry attempts are exhausted (`IsSucceeded = false`), the message should be routed to the **Dead Letter Queue** (Tutorial 25) with `DeadLetterReason.MaxRetriesExceeded`. Non-retryable errors (e.g. `ValidationFailed`, deserialization errors, schema mismatches) should be detected early and sent immediately to the DLQ without consuming retry attempts. The `LastException` and `Attempts` fields in `RetryResult` provide full diagnostic context.

---

## Lab

**Objective:** Calculate exponential backoff delays, analyze why jitter is critical for **scalable** retry under thundering-herd conditions, and design a retry classification strategy.

### Step 1: Calculate Backoff Delays

With `MaxAttempts = 4`, `InitialDelayMs = 500`, `BackoffMultiplier = 2.0`, and `UseJitter = false`, calculate the delay before each retry:

| Attempt | Delay Formula | Delay |
|---------|--------------|-------|
| 1 (first retry) | 500 × 2⁰ | 500ms |
| 2 | 500 × 2¹ | ? |
| 3 | 500 × 2² | ? |
| 4 | 500 × 2³ | ? |

What is the total maximum wait time across all retries? Open `src/Processing.Retry/ExponentialBackoffRetryPolicy.cs` to verify the formula.

### Step 2: Analyze the Thundering Herd Problem

100 consumers lose connection to a database. All retry at the same exponential intervals (no jitter). Draw what happens:

```
t=0s:   [100 consumers all fail]
t=500ms: [100 consumers all retry simultaneously] → database overwhelmed again
t=1000ms: [100 consumers all retry simultaneously] → database overwhelmed again
```

Now add jitter: each consumer randomizes its delay within ±50%. Explain:
- How does jitter spread the retry load over time?
- Why is this critical for **system-level scalability** during recovery?
- What is the relationship between jitter and the database's recovery time?

### Step 3: Design Retry Classification

Not all errors are retryable. Design a classification strategy:

| Error Type | Retryable? | Action |
|-----------|-----------|--------|
| HTTP 503 (Service Unavailable) | Yes | Exponential backoff |
| HTTP 400 (Bad Request) | No | Immediate DLQ |
| `JsonException` (deserialization) | No | Immediate DLQ |
| `TimeoutException` (network) | Yes | ? |
| Schema validation failure | No | ? |

Why is fast-failing non-retryable errors critical for **pipeline throughput**? What happens if you retry a `JsonException` 4 times before giving up?

## Exam

1. With `InitialDelayMs = 1000` and `BackoffMultiplier = 2.0`, what is the delay before the 4th retry attempt?
   - A) 4000ms
   - B) 8000ms — the delay doubles each attempt: 1000, 2000, 4000, 8000
   - C) 3000ms
   - D) 16000ms

2. Why is jitter critical for **scalable** retry strategies in distributed systems?
   - A) Jitter makes retries faster
   - B) Without jitter, all consumers retry at identical intervals — creating synchronized spikes that can overwhelm the recovering service; jitter spreads retries over time, enabling gradual recovery
   - C) Jitter is only needed for testing
   - D) The broker requires jitter in retry delays

3. Why should non-retryable errors (e.g., `JsonException`) be routed to the DLQ immediately instead of retried?
   - A) Non-retryable errors are rare and don't matter
   - B) Retrying a permanent error wastes processing capacity and delays handling of valid messages — fast-failing to DLQ preserves pipeline **throughput** and enables rapid human intervention
   - C) The DLQ can fix the error automatically
   - D) Non-retryable errors eventually succeed after enough retries

---

**Previous: [← Tutorial 23 — Request-Reply](23-request-reply.md)** | **Next: [Tutorial 25 — Dead Letter Queue →](25-dead-letter-queue.md)**
