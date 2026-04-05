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
            var lagInfo = await _lagMonitor.GetLagAsync(
                _options.TargetTopic, _options.ConsumerGroup, stoppingToken);

            if (lagInfo.CurrentLag >= _options.ScaleUpThreshold)
                await _scaler.ScaleAsync(
                    Math.Min(_scaler.CurrentCount + 1, _options.MaxConsumers), stoppingToken);
            else if (lagInfo.CurrentLag <= _options.ScaleDownThreshold
                     && _scaler.CurrentCount > _options.MinConsumers)
                await _scaler.ScaleAsync(_scaler.CurrentCount - 1, stoppingToken);

            await Task.Delay(_options.CooldownMs, stoppingToken);
        }
    }
}
```

The orchestrator runs as a hosted `BackgroundService`. On each evaluation cycle it reads the consumer lag via `GetLagAsync`, compares against thresholds, and calls `ScaleAsync` with the desired consumer count.

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

public sealed record ConsumerLagInfo(
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

## Exercises

1. A topic has 8 partitions and `MaxConsumers = 12`. What happens when the orchestrator tries to scale beyond 8 consumers? Why is `MaxConsumers` still useful?

2. Consumer lag oscillates between 900 and 1100 with `ScaleUpThreshold = 1000`. Without `CooldownMs`, what behavior would you observe? How does cooldown fix it?

3. Design an `IBackpressureSignal` integration that returns HTTP 429 from the Gateway API when backpressure is active.

---

**Previous: [← Tutorial 27 — Resequencer](27-resequencer.md)** | **Next: [Tutorial 29 — Throttle & Rate Limiting →](29-throttle-rate-limiting.md)**
