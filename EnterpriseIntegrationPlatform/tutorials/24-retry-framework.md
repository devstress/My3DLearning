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

## Exercises

1. With `MaxAttempts = 4`, `InitialDelayMs = 500`, `BackoffMultiplier = 2.0`, and `UseJitter = false`, calculate the delay before each retry attempt.

2. Why is jitter important? Describe a scenario where 100 consumers without jitter cause problems for a recovering database.

3. A `JsonException` during deserialization is not retryable. How would you detect this and short-circuit to the DLQ?

---

**Previous: [← Tutorial 23 — Request-Reply](23-request-reply.md)** | **Next: [Tutorial 25 — Dead Letter Queue →](25-dead-letter-queue.md)**
