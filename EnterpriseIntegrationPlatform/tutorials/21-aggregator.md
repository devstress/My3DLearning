# Tutorial 21 — Aggregator

Collect related messages by `CorrelationId` and combine them into a single aggregate when the group is complete.

---

## Key Types

```csharp
// src/Processing.Aggregator/IMessageAggregator.cs
public interface IMessageAggregator<TItem, TAggregate>
{
    Task<AggregateResult<TAggregate>> AggregateAsync(
        IntegrationEnvelope<TItem> envelope,
        CancellationToken cancellationToken = default);
}

// src/Processing.Aggregator/ICompletionStrategy.cs
public interface ICompletionStrategy<T>
{
    bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group);
}

// src/Processing.Aggregator/CountCompletionStrategy.cs
public sealed class CountCompletionStrategy<T> : ICompletionStrategy<T>
{
    public CountCompletionStrategy(int expectedCount) { ... }
    public bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group) =>
        group.Count >= _expectedCount;
}

// src/Processing.Aggregator/IAggregationStrategy.cs
public interface IAggregationStrategy<TItem, TAggregate>
{
    TAggregate Aggregate(IReadOnlyList<TItem> items);
}

// src/Processing.Aggregator/IMessageAggregateStore.cs
public interface IMessageAggregateStore<T>
{
    Task<IReadOnlyList<IntegrationEnvelope<T>>> AddAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
    Task RemoveGroupAsync(Guid correlationId, CancellationToken cancellationToken = default);
}

// src/Processing.Aggregator/AggregateResult.cs
public sealed record AggregateResult<TAggregate>(
    bool IsComplete,
    IntegrationEnvelope<TAggregate>? AggregateEnvelope,
    Guid CorrelationId,
    int ReceivedCount);
```

---

## Exercises

### Exercise 1: Store groups items by CorrelationId

```csharp
var store = new InMemoryMessageAggregateStore<string>();
var correlationId = Guid.NewGuid();

var e1 = IntegrationEnvelope<string>.Create(
    "item-1", "Svc", "line", correlationId: correlationId);
var e2 = IntegrationEnvelope<string>.Create(
    "item-2", "Svc", "line", correlationId: correlationId);

await store.AddAsync(e1);
var group = await store.AddAsync(e2);

Assert.That(group.Count, Is.EqualTo(2));
Assert.That(group[0].Payload, Is.EqualTo("item-1"));
Assert.That(group[1].Payload, Is.EqualTo("item-2"));
```

### Exercise 2: CountCompletionStrategy fires when count reached

```csharp
var strategy = new CountCompletionStrategy<string>(2);
var envelopes = new[]
{
    IntegrationEnvelope<string>.Create("a", "Svc", "t"),
    IntegrationEnvelope<string>.Create("b", "Svc", "t"),
};

Assert.That(strategy.IsComplete(envelopes), Is.True);
```

### Exercise 3: Aggregator returns incomplete when group not ready

```csharp
var store = new InMemoryMessageAggregateStore<string>();
var completion = new CountCompletionStrategy<string>(3);
var aggregation = Substitute.For<IAggregationStrategy<string, string>>();
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new AggregatorOptions
{
    TargetTopic = "aggregated-topic",
    ExpectedCount = 3,
});

var aggregator = new MessageAggregator<string, string>(
    store, completion, aggregation, producer, options,
    NullLogger<MessageAggregator<string, string>>.Instance);

var correlationId = Guid.NewGuid();
var envelope = IntegrationEnvelope<string>.Create(
    "item-1", "Svc", "line", correlationId: correlationId);

var result = await aggregator.AggregateAsync(envelope);

Assert.That(result.IsComplete, Is.False);
Assert.That(result.AggregateEnvelope, Is.Null);
Assert.That(result.ReceivedCount, Is.EqualTo(1));
Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
```

### Exercise 4: Aggregator completes and publishes when count reached

```csharp
var store = new InMemoryMessageAggregateStore<string>();
var completion = new CountCompletionStrategy<string>(2);
var aggregation = Substitute.For<IAggregationStrategy<string, string>>();
aggregation
    .Aggregate(Arg.Any<IReadOnlyList<string>>())
    .Returns(ci =>
    {
        var items = ci.Arg<IReadOnlyList<string>>();
        return string.Join(",", items);
    });

var producer = Substitute.For<IMessageBrokerProducer>();
var options = Options.Create(new AggregatorOptions
{
    TargetTopic = "agg-out",
    TargetMessageType = "order.batch",
    ExpectedCount = 2,
});

var aggregator = new MessageAggregator<string, string>(
    store, completion, aggregation, producer, options,
    NullLogger<MessageAggregator<string, string>>.Instance);

var correlationId = Guid.NewGuid();
var e1 = IntegrationEnvelope<string>.Create(
    "A", "Svc", "line", correlationId: correlationId);
var e2 = IntegrationEnvelope<string>.Create(
    "B", "Svc", "line", correlationId: correlationId);

await aggregator.AggregateAsync(e1);
var result = await aggregator.AggregateAsync(e2);

Assert.That(result.IsComplete, Is.True);
Assert.That(result.ReceivedCount, Is.EqualTo(2));
Assert.That(result.AggregateEnvelope, Is.Not.Null);
Assert.That(result.AggregateEnvelope!.Payload, Is.EqualTo("A,B"));
Assert.That(result.AggregateEnvelope.MessageType, Is.EqualTo("order.batch"));
Assert.That(result.AggregateEnvelope.CorrelationId, Is.EqualTo(correlationId));
```

### Exercise 5: Aggregator merges metadata from all envelopes

```csharp
var store = new InMemoryMessageAggregateStore<string>();
var completion = new CountCompletionStrategy<string>(2);
var aggregation = Substitute.For<IAggregationStrategy<string, string>>();
aggregation.Aggregate(Arg.Any<IReadOnlyList<string>>()).Returns("merged");
var producer = Substitute.For<IMessageBrokerProducer>();
var options = Options.Create(new AggregatorOptions
{
    TargetTopic = "merged-topic",
    ExpectedCount = 2,
});

var aggregator = new MessageAggregator<string, string>(
    store, completion, aggregation, producer, options,
    NullLogger<MessageAggregator<string, string>>.Instance);

var correlationId = Guid.NewGuid();
var e1 = IntegrationEnvelope<string>.Create(
    "A", "Svc", "line", correlationId: correlationId) with
{
    Metadata = new Dictionary<string, string> { ["key1"] = "val1" },
};
var e2 = IntegrationEnvelope<string>.Create(
    "B", "Svc", "line", correlationId: correlationId) with
{
    Metadata = new Dictionary<string, string> { ["key2"] = "val2" },
};

await aggregator.AggregateAsync(e1);
var result = await aggregator.AggregateAsync(e2);

Assert.That(result.AggregateEnvelope!.Metadata, Contains.Key("key1"));
Assert.That(result.AggregateEnvelope.Metadata, Contains.Key("key2"));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial21/Lab.cs`](../tests/TutorialLabs/Tutorial21/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial21.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial21/Exam.cs`](../tests/TutorialLabs/Tutorial21/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial21.Exam"
```

---

**Previous: [← Tutorial 20 — Splitter](20-splitter.md)** | **Next: [Tutorial 22 — Scatter-Gather →](22-scatter-gather.md)**
