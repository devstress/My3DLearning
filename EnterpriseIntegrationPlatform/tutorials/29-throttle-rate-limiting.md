# Tutorial 29 — Throttle & Rate Limiting

Control message throughput with token-bucket rate limiting and throttling.

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

## Exercises

### 1. AcquireAsync — WithAvailableTokens ReturnsPermitted

```csharp
var options = Options.Create(new ThrottleOptions
{
    MaxMessagesPerSecond = 100,
    BurstCapacity = 10,
    MaxWaitTime = TimeSpan.FromSeconds(5),
});

using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");

var result = await throttle.AcquireAsync(envelope);

Assert.That(result.Permitted, Is.True);
Assert.That(result.RejectionReason, Is.Null);
```

### 2. AcquireAsync — ConsumesToken DecreasesAvailableCount

```csharp
var options = Options.Create(new ThrottleOptions
{
    MaxMessagesPerSecond = 100,
    BurstCapacity = 5,
});

using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

var before = throttle.AvailableTokens;

var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");
await throttle.AcquireAsync(envelope);

Assert.That(throttle.AvailableTokens, Is.LessThan(before));
```

### 3. AcquireAsync — NoTokensWithRejectOnBackpressure RejectsImmediately

```csharp
var options = Options.Create(new ThrottleOptions
{
    MaxMessagesPerSecond = 1,
    BurstCapacity = 1,
    RejectOnBackpressure = true,
});

using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");

// Consume the only token.
await throttle.AcquireAsync(envelope);

// Next acquire should be rejected (no tokens, reject mode).
var result = await throttle.AcquireAsync(envelope);

Assert.That(result.Permitted, Is.False);
Assert.That(result.RejectionReason, Is.Not.Null.And.Not.Empty);
```

### 4. ThrottleOptions — Defaults AreReasonable

```csharp
var opts = new ThrottleOptions();

Assert.That(opts.MaxMessagesPerSecond, Is.EqualTo(100));
Assert.That(opts.BurstCapacity, Is.EqualTo(200));
Assert.That(opts.MaxWaitTime, Is.EqualTo(TimeSpan.FromSeconds(30)));
Assert.That(opts.RejectOnBackpressure, Is.False);
```

### 5. ThrottleResult — ContainsExpectedFields

```csharp
var options = Options.Create(new ThrottleOptions
{
    MaxMessagesPerSecond = 100,
    BurstCapacity = 10,
});

using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");
var result = await throttle.AcquireAsync(envelope);

Assert.That(result.Permitted, Is.True);
Assert.That(result.WaitTime, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
Assert.That(result.RemainingTokens, Is.GreaterThanOrEqualTo(0));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial29/Lab.cs`](../tests/TutorialLabs/Tutorial29/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial29.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial29/Exam.cs`](../tests/TutorialLabs/Tutorial29/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial29.Exam"
```

---

**Previous: [← Tutorial 28 — Competing Consumers](28-competing-consumers.md)** | **Next: [Tutorial 30 — Rule Engine →](30-rule-engine.md)**
