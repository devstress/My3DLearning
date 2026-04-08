# Tutorial 28 — Competing Consumers

Scale message processing horizontally with competing consumer instances.

---

## Learning Objectives

1. Understand the Competing Consumers pattern for horizontal message processing
2. Use `CompetingConsumerOrchestrator` to evaluate lag and auto-scale consumer instances
3. Configure scale-up and scale-down thresholds with `CompetingConsumerOptions`
4. Verify backpressure signaling when maximum consumer capacity is reached
5. Confirm minimum consumer floor prevents over-scaling down
6. Observe steady-state behavior when lag falls between thresholds

---

## Key Types

```csharp
// src/Processing.CompetingConsumers/CompetingConsumerOrchestrator.cs
public sealed class CompetingConsumerOrchestrator : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAndScaleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during competing consumer orchestration cycle");
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(_options.CooldownMs), _timeProvider, stoppingToken);
        }
    }

    internal async Task EvaluateAndScaleAsync(CancellationToken cancellationToken)
    {
        var lagInfo = await _lagMonitor.GetLagAsync(
            _options.TargetTopic, _options.ConsumerGroup, cancellationToken);

        var currentCount = _scaler.CurrentCount;
        var now = _timeProvider.GetUtcNow();
        var cooldown = TimeSpan.FromMilliseconds(_options.CooldownMs);

        if (lagInfo.CurrentLag >= _options.ScaleUpThreshold)
        {
            if (currentCount >= _options.MaxConsumers)
            {
                _backpressure.Signal();  // signal backpressure when at capacity
                return;
            }

            _backpressure.Release();
            if ((now - _lastScaleTime) < cooldown) return;  // cooldown guard

            var desired = Math.Min(currentCount + 1, _options.MaxConsumers);
            await _scaler.ScaleAsync(desired, cancellationToken);
            _lastScaleTime = now;
        }
        else if (lagInfo.CurrentLag <= _options.ScaleDownThreshold)
        {
            _backpressure.Release();
            if (currentCount <= _options.MinConsumers) return;
            if (_backpressure.IsBackpressured) return;  // pause scale-down under backpressure
            if ((now - _lastScaleTime) < cooldown) return;

            var desired = Math.Max(currentCount - 1, _options.MinConsumers);
            await _scaler.ScaleAsync(desired, cancellationToken);
            _lastScaleTime = now;
        }
        else
        {
            _backpressure.Release();
        }
    }
}
```

```csharp
// src/Processing.CompetingConsumers/IConsumerLagMonitor.cs
public interface IConsumerLagMonitor
{
    Task<ConsumerLagInfo> GetLagAsync(
        string topic,
        string consumerGroup,
        CancellationToken ct = default);
}

public record ConsumerLagInfo(
    string ConsumerGroup,
    string Topic,
    long CurrentLag,
    DateTimeOffset Timestamp);
```

```csharp
// src/Processing.CompetingConsumers/IConsumerScaler.cs
public interface IConsumerScaler
{
    int CurrentCount { get; }
    Task ScaleAsync(int desiredCount, CancellationToken ct = default);
}
```

```csharp
// src/Processing.CompetingConsumers/IBackpressureSignal.cs
public interface IBackpressureSignal
{
    bool IsBackpressured { get; }
    void Signal();
    void Release();
}
```

## Lab — Guided Practice

> 💻 Run the lab tests to see each Competing Consumers concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `HighLag_ScalesUp` | High lag triggers consumer scale-up |
| 2 | `LowLag_ScalesDown` | Low lag triggers consumer scale-down |
| 3 | `MaxConsumers_SignalsBackpressure` | At max consumers, backpressure is signaled |
| 4 | `MinConsumers_DoesNotScaleBelow` | Min consumer floor prevents scale-down |
| 5 | `ModerateLag_NoScaleChange` | Moderate lag keeps consumer count stable |
| 6 | `BackpressureReleased_AfterLagDrops` | Backpressure released when lag drops |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial28.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_ProgressiveScaleUp_ReachesMax` | 🟢 Starter | ProgressiveScaleUp — ReachesMax |
| 2 | `Intermediate_ZeroLag_DefaultsReturned` | 🟡 Intermediate | ZeroLag — DefaultsReturned |
| 3 | `Advanced_BackpressureAtMax_ThenRelease` | 🔴 Advanced | BackpressureAtMax — ThenRelease |

> 💻 [`tests/TutorialLabs/Tutorial28/Exam.cs`](../tests/TutorialLabs/Tutorial28/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial28.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial28.ExamAnswers"
```
---

**Previous: [← Tutorial 27 — Resequencer](27-resequencer.md)** | **Next: [Tutorial 29 — Throttle & Rate Limiting →](29-throttle-rate-limiting.md)**
