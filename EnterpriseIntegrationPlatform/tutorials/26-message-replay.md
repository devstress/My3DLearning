# Tutorial 26 — Message Replay

Replay previously processed messages from the replay store with filtering and deduplication.

## Key Types

```csharp
// src/Processing.Replay/IMessageReplayer.cs
public interface IMessageReplayer
{
    Task<ReplayResult> ReplayAsync(ReplayFilter filter, CancellationToken ct);
}
```

```csharp
// src/Processing.Replay/IMessageReplayStore.cs
public interface IMessageReplayStore
{
    Task StoreForReplayAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct);
    IAsyncEnumerable<IntegrationEnvelope<object>> GetMessagesForReplayAsync(string topic, ReplayFilter filter, int maxMessages, CancellationToken ct);
}
```

```csharp
// src/Processing.Replay/ReplayFilter.cs
public record ReplayFilter
{
    public Guid? CorrelationId { get; init; }
    public string? MessageType { get; init; }
    public DateTimeOffset? FromTimestamp { get; init; }
    public DateTimeOffset? ToTimestamp { get; init; }
}
```

```csharp
// src/Processing.Replay/ReplayResult.cs
public record ReplayResult
{
    public required int ReplayedCount { get; init; }
    public required int SkippedCount { get; init; }
    public required int FailedCount { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}
```

## Exercises

### 1. Replay — AllMessagesReplayed CountsAreCorrect

```csharp
var store = new InMemoryMessageReplayStore();
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new ReplayOptions
{
    SourceTopic = "orders",
    TargetTopic = "orders-replay",
    MaxMessages = 100,
});

var replayer = new MessageReplayer(
    store, producer, options, NullLogger<MessageReplayer>.Instance);

var env1 = IntegrationEnvelope<string>.Create("p1", "Svc", "order.created");
var env2 = IntegrationEnvelope<string>.Create("p2", "Svc", "order.created");
await store.StoreForReplayAsync(env1, "orders", CancellationToken.None);
await store.StoreForReplayAsync(env2, "orders", CancellationToken.None);

var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

Assert.That(result.ReplayedCount, Is.EqualTo(2));
Assert.That(result.SkippedCount, Is.EqualTo(0));
Assert.That(result.FailedCount, Is.EqualTo(0));
```

### 2. Replay — PublishesToConfiguredTargetTopic

```csharp
var store = new InMemoryMessageReplayStore();
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new ReplayOptions
{
    SourceTopic = "events",
    TargetTopic = "events-replay",
    MaxMessages = 10,
});

var replayer = new MessageReplayer(
    store, producer, options, NullLogger<MessageReplayer>.Instance);

var env = IntegrationEnvelope<string>.Create("data", "Svc", "event.fired");
await store.StoreForReplayAsync(env, "events", CancellationToken.None);

await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<object>>(),
    "events-replay",
    Arg.Any<CancellationToken>());
```

### 3. Replay — FilterByMessageType OnlyMatchingMessagesReplayed

```csharp
var store = new InMemoryMessageReplayStore();
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new ReplayOptions
{
    SourceTopic = "topic",
    TargetTopic = "topic-replay",
    MaxMessages = 100,
});

var replayer = new MessageReplayer(
    store, producer, options, NullLogger<MessageReplayer>.Instance);

var match = IntegrationEnvelope<string>.Create("m", "Svc", "order.created");
var noMatch = IntegrationEnvelope<string>.Create("n", "Svc", "invoice.created");
await store.StoreForReplayAsync(match, "topic", CancellationToken.None);
await store.StoreForReplayAsync(noMatch, "topic", CancellationToken.None);

var filter = new ReplayFilter { MessageType = "order.created" };
var result = await replayer.ReplayAsync(filter, CancellationToken.None);

Assert.That(result.ReplayedCount, Is.EqualTo(1));
```

### 4. Replay — SkipAlreadyReplayed SkipsMessagesWithReplayIdHeader

```csharp
var store = new InMemoryMessageReplayStore();
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new ReplayOptions
{
    SourceTopic = "src",
    TargetTopic = "tgt",
    MaxMessages = 100,
    SkipAlreadyReplayed = true,
});

var replayer = new MessageReplayer(
    store, producer, options, NullLogger<MessageReplayer>.Instance);

var alreadyReplayed = new IntegrationEnvelope<string>
{
    MessageId = Guid.NewGuid(),
    CorrelationId = Guid.NewGuid(),
    Timestamp = DateTimeOffset.UtcNow,
    Source = "Svc",
    MessageType = "type",
    Payload = "data",
    Metadata = new Dictionary<string, string>
    {
        [MessageHeaders.ReplayId] = Guid.NewGuid().ToString(),
    },
};
var fresh = IntegrationEnvelope<string>.Create("fresh", "Svc", "type");

await store.StoreForReplayAsync(alreadyReplayed, "src", CancellationToken.None);
await store.StoreForReplayAsync(fresh, "src", CancellationToken.None);

var result = await replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None);

Assert.That(result.ReplayedCount, Is.EqualTo(1));
Assert.That(result.SkippedCount, Is.EqualTo(1));
```

### 5. Replay — EmptySourceTopic ThrowsInvalidOperationException

```csharp
var store = new InMemoryMessageReplayStore();
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new ReplayOptions
{
    SourceTopic = "",
    TargetTopic = "tgt",
});

var replayer = new MessageReplayer(
    store, producer, options, NullLogger<MessageReplayer>.Instance);

Assert.ThrowsAsync<InvalidOperationException>(
    () => replayer.ReplayAsync(new ReplayFilter(), CancellationToken.None));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial26/Lab.cs`](../tests/TutorialLabs/Tutorial26/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial26.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial26/Exam.cs`](../tests/TutorialLabs/Tutorial26/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial26.Exam"
```

---

**Previous: [← Tutorial 25 — Dead Letter Queue](25-dead-letter-queue.md)** | **Next: [Tutorial 27 — Resequencer →](27-resequencer.md)**
