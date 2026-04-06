# Tutorial 28 — Competing Consumers

## What You'll Learn

- The EIP Competing Consumers pattern for parallel message processing
- How `CompetingConsumerOrchestrator` (a `BackgroundService`) manages consumer lifecycles
- `IConsumerLagMonitor` for measuring how far behind consumers are
- `IConsumerScaler` for adding or removing consumer instances
- `IBackpressureSignal` for communicating overload upstream
- Auto-scaling based on consumer lag with cooldown to prevent flapping
- Why this is the **Scalability pillar centerpiece** of the platform

---

## EIP Pattern: Competing Consumers

> *"Create multiple consumers on a single Point-to-Point Channel so that the consumers can process multiple messages concurrently."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
                          ┌──────────────┐
                     ┌───▶│  Consumer 1  │
  ┌──────────┐      │    └──────────────┘
  │  Broker  │──────┤    ┌──────────────┐
  │  Topic   │      ├───▶│  Consumer 2  │
  └──────────┘      │    └──────────────┘
                    │    ┌──────────────┐
                    └───▶│  Consumer N  │
                          └──────────────┘
                                │
                    Orchestrator scales N
                    based on lag + backpressure
```

Multiple consumers read from the same topic. The broker ensures each message is delivered to exactly one consumer in the group. The orchestrator monitors lag and adjusts the consumer count dynamically.

---

## Platform Implementation

### CompetingConsumerOrchestrator

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

The orchestrator runs as a hosted `BackgroundService`. On each evaluation cycle it reads the consumer lag via `GetLagAsync`, compares against thresholds, and calls `ScaleAsync` with the desired consumer count. Key features:

- **Backpressure signaling** — when at max capacity, signals backpressure to upstream producers
- **Cooldown guard** — prevents scaling flapping with a configurable cooldown period
- **Backpressure-aware scale-down** — won't scale down while backpressure is active

### IConsumerLagMonitor

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

> **Note:** `ConsumerLagInfo` tracks lag per consumer group and topic. The active consumer count is available separately via `IConsumerScaler.CurrentCount`.

### IConsumerScaler

```csharp
// src/Processing.CompetingConsumers/IConsumerScaler.cs
public interface IConsumerScaler
{
    int CurrentCount { get; }
    Task ScaleAsync(int desiredCount, CancellationToken ct = default);
}
```

### IBackpressureSignal

```csharp
// src/Processing.CompetingConsumers/IBackpressureSignal.cs
public interface IBackpressureSignal
{
    bool IsBackpressured { get; }
    void Signal();
    void Release();
}
```

When `IsBackpressured` is true, the orchestrator pauses scale-down and can signal upstream producers (via broker flow control or HTTP 429) to slow ingestion.

### CompetingConsumerOptions

```csharp
// src/Processing.CompetingConsumers/CompetingConsumerOptions.cs
public sealed class CompetingConsumerOptions
{
    public int MinConsumers { get; set; } = 1;
    public int MaxConsumers { get; set; } = 10;
    public long ScaleUpThreshold { get; set; } = 1000;
    public long ScaleDownThreshold { get; set; } = 100;
    public int CooldownMs { get; set; } = 30_000;
    public string TargetTopic { get; set; } = string.Empty;
    public string ConsumerGroup { get; set; } = string.Empty;
}
```

The `CooldownMs` prevents flapping — after a scale event, no further scaling occurs until the cooldown expires. This avoids rapid oscillation when lag hovers near a threshold.

---

## Scalability Dimension

This is the **Scalability pillar centerpiece**. Competing consumers turn a single-threaded message pipeline into a horizontally scalable processing tier. The broker partitions the topic; each consumer claims one or more partitions. Adding consumers increases throughput linearly — up to the partition count. The lag monitor provides the feedback loop, and the scaler adjusts capacity without human intervention.

---

## Atomicity Dimension

Each consumer processes messages independently and Acks them individually. If a consumer crashes, its partitions are rebalanced to surviving consumers, and unacked messages are redelivered. The orchestrator only adjusts the consumer count — it never touches message processing itself. This separation ensures that scaling events cannot corrupt in-flight message handling.

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial28/Lab.cs`](../tests/TutorialLabs/Tutorial28/Lab.cs)

**Objective:** Trace the auto-scaling orchestrator with backpressure signaling, analyze cooldown to prevent scaling flap, and design a production backpressure integration.

### Step 1: Trace the Scaling Decision Path

A topic has 8 partitions, `MaxConsumers = 12`, and current consumer lag is 5,000. Open `src/Processing.CompetingConsumers/CompetingConsumerOrchestrator.cs` and trace `EvaluateAndScaleAsync`:

1. Lag exceeds `ScaleUpThreshold` → what happens if current consumers = 8?
2. Lag exceeds threshold but `currentCount >= MaxConsumers` → what signal is emitted?
3. After scaling up, what prevents another scale-up in the next cycle? (hint: cooldown)

Now: with `MaxConsumers = 12` and 8 Kafka partitions, what happens when the orchestrator scales to 9 consumers? (hint: one consumer will be idle — Kafka can't assign more consumers than partitions)

### Step 2: Analyze Cooldown for Scaling Stability

Consumer lag oscillates between 900 and 1100 with `ScaleUpThreshold = 1000`. Without cooldown:

```
Cycle 1: lag=1100 → scale up (3→4)
Cycle 2: lag=900 → scale down (4→3)
Cycle 3: lag=1100 → scale up (3→4)
... flapping forever
```

How does `CooldownMs` break this cycle? What is the relationship between cooldown duration and scaling stability? What value would you set for a production system?

### Step 3: Design a Backpressure Integration

When the consumer pool is at maximum capacity and lag keeps growing, the orchestrator signals backpressure. Design a system-wide response:

| Component | Backpressure Action |
|-----------|-------------------|
| Gateway API | Return HTTP 429 to upstream senders |
| Ingestion producers | Pause or slow message publishing |
| Dashboard (OpenClaw) | Show backpressure warning to operators |
| Monitoring (OpenTelemetry) | Emit backpressure metrics and alerts |

How does backpressure prevent **cascade failures** in a scalable system? What happens without it?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial28/Exam.cs`](../tests/TutorialLabs/Tutorial28/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 27 — Resequencer](27-resequencer.md)** | **Next: [Tutorial 29 — Throttle & Rate Limiting →](29-throttle-rate-limiting.md)**
