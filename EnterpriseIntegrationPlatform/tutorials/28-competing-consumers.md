# Tutorial 28 — Competing Consumers

Scale message processing horizontally with competing consumer instances.

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

## Exercises

### 1. BackpressureSignal — SignalAndRelease TogglesCorrectly

```csharp
var bp = new BackpressureSignal();

Assert.That(bp.IsBackpressured, Is.False);

bp.Signal();
Assert.That(bp.IsBackpressured, Is.True);

bp.Release();
Assert.That(bp.IsBackpressured, Is.False);
```

### 2. InMemoryConsumerScaler — ScaleUp IncreasesCount

```csharp
var scaler = new InMemoryConsumerScaler(
    NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 1);

Assert.That(scaler.CurrentCount, Is.EqualTo(1));

await scaler.ScaleAsync(3, CancellationToken.None);

Assert.That(scaler.CurrentCount, Is.EqualTo(3));
```

### 3. ConsumerLagInfo — RecordProperties AreCorrect

```csharp
var now = DateTimeOffset.UtcNow;
var info = new ConsumerLagInfo("group-1", "orders", 500, now);

Assert.That(info.ConsumerGroup, Is.EqualTo("group-1"));
Assert.That(info.Topic, Is.EqualTo("orders"));
Assert.That(info.CurrentLag, Is.EqualTo(500));
Assert.That(info.Timestamp, Is.EqualTo(now));
```

### 4. InMemoryLagMonitor — ReportAndGet ReturnsReportedLag

```csharp
var monitor = new InMemoryConsumerLagMonitor();
var lag = new ConsumerLagInfo("grp", "topic", 1234, DateTimeOffset.UtcNow);

await monitor.ReportLagAsync(lag);
var retrieved = await monitor.GetLagAsync("topic", "grp", CancellationToken.None);

Assert.That(retrieved.CurrentLag, Is.EqualTo(1234));
```

### 5. EvaluateAndScale — HighLag ScalesUp

```csharp
var lagMonitor = Substitute.For<IConsumerLagMonitor>();
var scaler = Substitute.For<IConsumerScaler>();
var backpressure = new BackpressureSignal();
var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

scaler.CurrentCount.Returns(1);
lagMonitor.GetLagAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns(new ConsumerLagInfo("grp", "topic", 5000, DateTimeOffset.UtcNow));

var options = Options.Create(new CompetingConsumerOptions
{
    MinConsumers = 1,
    MaxConsumers = 10,
    ScaleUpThreshold = 1000,
    ScaleDownThreshold = 100,
    CooldownMs = 1000,
    TargetTopic = "topic",
    ConsumerGroup = "grp",
});

var orchestrator = new CompetingConsumerOrchestrator(
    lagMonitor, scaler, backpressure, options,
    NullLogger<CompetingConsumerOrchestrator>.Instance, timeProvider);

await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

await scaler.Received(1).ScaleAsync(2, Arg.Any<CancellationToken>());
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial28/Lab.cs`](../tests/TutorialLabs/Tutorial28/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial28.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial28/Exam.cs`](../tests/TutorialLabs/Tutorial28/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial28.Exam"
```

---

**Previous: [← Tutorial 27 — Resequencer](27-resequencer.md)** | **Next: [Tutorial 29 — Throttle & Rate Limiting →](29-throttle-rate-limiting.md)**
