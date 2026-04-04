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
            var lag = await _lagMonitor.GetCurrentLagAsync(stoppingToken);

            if (lag > _options.ScaleUpThreshold)
                await _scaler.ScaleUpAsync(1, stoppingToken);
            else if (lag < _options.ScaleDownThreshold && _scaler.CurrentCount > _options.MinConsumers)
                await _scaler.ScaleDownAsync(1, stoppingToken);

            await Task.Delay(_options.EvaluationInterval, stoppingToken);
        }
    }
}
```

The orchestrator runs as a hosted `BackgroundService`. On each evaluation cycle it reads the consumer lag, compares against thresholds, and scales up or down.

### IConsumerLagMonitor

```csharp
// src/Processing.CompetingConsumers/IConsumerLagMonitor.cs
public interface IConsumerLagMonitor
{
    Task<long> GetCurrentLagAsync(CancellationToken ct);
    Task<IDictionary<string, long>> GetLagByPartitionAsync(CancellationToken ct);
}
```

### IConsumerScaler

```csharp
// src/Processing.CompetingConsumers/IConsumerScaler.cs
public interface IConsumerScaler
{
    int CurrentCount { get; }
    Task ScaleUpAsync(int count, CancellationToken ct);
    Task ScaleDownAsync(int count, CancellationToken ct);
}
```

### IBackpressureSignal

```csharp
// src/Processing.CompetingConsumers/IBackpressureSignal.cs
public interface IBackpressureSignal
{
    bool IsActive { get; }
    void Activate(string reason);
    void Deactivate();
}
```

When backpressure is active, the orchestrator pauses scale-down and can signal upstream producers (via broker flow control or HTTP 429) to slow ingestion.

### CompetingConsumerOptions

```csharp
// src/Processing.CompetingConsumers/CompetingConsumerOptions.cs
public sealed class CompetingConsumerOptions
{
    public int MinConsumers { get; init; } = 1;
    public int MaxConsumers { get; init; } = 10;
    public long ScaleUpThreshold { get; init; } = 1000;
    public long ScaleDownThreshold { get; init; } = 100;
    public TimeSpan EvaluationInterval { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan CooldownPeriod { get; init; } = TimeSpan.FromMinutes(2);
}
```

The `CooldownPeriod` prevents flapping — after a scale event, no further scaling occurs until the cooldown expires. This avoids rapid oscillation when lag hovers near a threshold.

---

## Scalability Dimension

This is the **Scalability pillar centerpiece**. Competing consumers turn a single-threaded message pipeline into a horizontally scalable processing tier. The broker partitions the topic; each consumer claims one or more partitions. Adding consumers increases throughput linearly — up to the partition count. The lag monitor provides the feedback loop, and the scaler adjusts capacity without human intervention.

---

## Atomicity Dimension

Each consumer processes messages independently and Acks them individually. If a consumer crashes, its partitions are rebalanced to surviving consumers, and unacked messages are redelivered. The orchestrator only adjusts the consumer count — it never touches message processing itself. This separation ensures that scaling events cannot corrupt in-flight message handling.

---

## Exercises

1. A topic has 8 partitions and `MaxConsumers = 12`. What happens when the orchestrator tries to scale beyond 8 consumers? Why is `MaxConsumers` still useful?

2. Consumer lag oscillates between 900 and 1100 with `ScaleUpThreshold = 1000`. Without `CooldownPeriod`, what behavior would you observe? How does cooldown fix it?

3. Design an `IBackpressureSignal` integration that returns HTTP 429 from the Gateway API when backpressure is active.

---

**Previous: [← Tutorial 27 — Resequencer](27-resequencer.md)** | **Next: [Tutorial 29 — Throttle & Rate Limiting →](29-throttle-rate-limiting.md)**
