# Tutorial 24 — Retry Framework

Wrap operations with exponential backoff retry logic, tracking attempts and surfacing the last exception on exhaustion.

---

## Learning Objectives

1. Understand exponential backoff retry logic and when to apply it
2. Use `ExponentialBackoffRetryPolicy` to wrap operations with configurable `RetryOptions`
3. Verify that a successful first attempt returns `IsSucceeded = true` with `Attempts = 1`
4. Confirm retry behaviour: transient failures are retried up to `MaxAttempts`
5. Validate exhaustion: all attempts fail → `IsSucceeded = false`, `LastException` is captured
6. Verify the void overload returns `RetryResult<bool>` and cancellation is propagated

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

## Lab — Guided Practice

> 💻 Run the lab tests to see each Retry Framework concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Execute_SucceedsFirstAttempt_ReturnsResult` | Successful first attempt returns result with Attempts = 1 |
| 2 | `Execute_FailsThenSucceeds_RetriesCorrectly` | Transient failures are retried until success |
| 3 | `Execute_AllAttemptsFail_ReturnsFailure` | All attempts exhausted returns IsSucceeded = false with LastException |
| 4 | `Execute_VoidOverload_ReturnsRetryResultBool` | Void overload returns RetryResult&lt;bool&gt; on success |
| 5 | `Execute_RetryThenPublish_EndToEnd` | Retry + publish end-to-end via real NATS |
| 6 | `Execute_MaxAttemptsOne_NoRetry` | MaxAttempts = 1 means no retry on failure |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial24.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_ExhaustRetries_CapturesLastException` | 🟢 Starter | ExhaustRetries — CapturesLastException |
| 2 | `Intermediate_CancellationDuringRetry_ThrowsOperationCanceled` | 🟡 Intermediate | CancellationDuringRetry — ThrowsOperationCanceled |
| 3 | `Advanced_RetrySuccessThenPublish_FullPipeline` | 🔴 Advanced | RetrySuccessThenPublish — FullPipeline |

> 💻 [`tests/TutorialLabs/Tutorial24/Exam.cs`](../tests/TutorialLabs/Tutorial24/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial24.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial24.ExamAnswers"
```
---

**Previous: [← Tutorial 23 — Request-Reply](23-request-reply.md)** | **Next: [Tutorial 25 — Dead Letter Queue →](25-dead-letter-queue.md)**
