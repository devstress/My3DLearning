# Tutorial 24 — Retry Framework

Wrap operations with exponential backoff retry logic, tracking attempts and surfacing the last exception on exhaustion.

---

## Key Types

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

// src/Processing.Retry/RetryOptions.cs
public sealed class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 30000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseJitter { get; set; } = true;
}

// src/Processing.Retry/RetryResult.cs
public record RetryResult<T>
{
    public required bool IsSucceeded { get; init; }
    public required int Attempts { get; init; }
    public Exception? LastException { get; init; }
    public T? Result { get; init; }
}
```

---

## Exercises

### Exercise 1: Success on first attempt

```csharp
var policy = CreatePolicy();

var result = await policy.ExecuteAsync<int>(
    _ => Task.FromResult(42), CancellationToken.None);

Assert.That(result.IsSucceeded, Is.True);
Assert.That(result.Attempts, Is.EqualTo(1));
Assert.That(result.Result, Is.EqualTo(42));
Assert.That(result.LastException, Is.Null);
```

### Exercise 2: Retry succeeds after transient failure

```csharp
var policy = CreatePolicy(maxAttempts: 5);
var callCount = 0;

var result = await policy.ExecuteAsync<string>(
    _ =>
    {
        callCount++;
        if (callCount < 3)
            throw new InvalidOperationException("transient");
        return Task.FromResult("ok");
    },
    CancellationToken.None);

Assert.That(result.IsSucceeded, Is.True);
Assert.That(result.Attempts, Is.EqualTo(3));
Assert.That(result.Result, Is.EqualTo("ok"));
```

### Exercise 3: All attempts exhausted returns failure with exception

```csharp
var policy = CreatePolicy(maxAttempts: 3);

var result = await policy.ExecuteAsync<string>(
    _ => throw new TimeoutException("always fails"),
    CancellationToken.None);

Assert.That(result.IsSucceeded, Is.False);
Assert.That(result.Attempts, Is.EqualTo(3));
Assert.That(result.LastException, Is.TypeOf<TimeoutException>());
Assert.That(result.Result, Is.Null);
```

### Exercise 4: Void overload retries and fails

```csharp
var policy = CreatePolicy(maxAttempts: 2);

var result = await policy.ExecuteAsync(
    _ => throw new IOException("disk full"),
    CancellationToken.None);

Assert.That(result.IsSucceeded, Is.False);
Assert.That(result.Attempts, Is.EqualTo(2));
Assert.That(result.LastException, Is.TypeOf<IOException>());
```

### Exercise 5: Cancellation is propagated

```csharp
var policy = CreatePolicy(maxAttempts: 5);
using var cts = new CancellationTokenSource();
cts.Cancel();

Assert.ThrowsAsync<OperationCanceledException>(
    () => policy.ExecuteAsync<int>(
        _ => Task.FromResult(1), cts.Token));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial24/Lab.cs`](../tests/TutorialLabs/Tutorial24/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial24.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial24/Exam.cs`](../tests/TutorialLabs/Tutorial24/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial24.Exam"
```

---

**Previous: [← Tutorial 23 — Request-Reply](23-request-reply.md)** | **Next: [Tutorial 25 — Dead Letter Queue →](25-dead-letter-queue.md)**
