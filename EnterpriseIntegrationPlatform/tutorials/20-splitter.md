# Tutorial 20 — Splitter

Break composite messages into individual items using `IMessageSplitter<T>` with pluggable `ISplitStrategy<T>`.

---

## Key Types

```csharp
// src/Processing.Splitter/IMessageSplitter.cs
public interface IMessageSplitter<T>
{
    Task<SplitResult<T>> SplitAsync(
        IntegrationEnvelope<T> source,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Splitter/ISplitStrategy.cs
public interface ISplitStrategy<T>
{
    IReadOnlyList<T> Split(T composite);
}
```

```csharp
// src/Processing.Splitter/SplitResult.cs
public sealed record SplitResult<T>(
    IReadOnlyList<IntegrationEnvelope<T>> SplitEnvelopes,
    Guid SourceMessageId,
    string TargetTopic,
    int ItemCount);
```

```csharp
// src/Processing.Splitter/SplitterOptions.cs
public sealed class SplitterOptions
{
    public string TargetTopic { get; init; }
    public string? ArrayPropertyName { get; init; }
}
```

---

## Exercises

### Exercise 1: Split comma-separated string into individual envelopes

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var strategy = new FuncSplitStrategy<string>(
    composite => composite.Split(',').ToList());

var options = Options.Create(new SplitterOptions { TargetTopic = "items-topic" });
var splitter = new MessageSplitter<string>(
    strategy, producer, options,
    NullLogger<MessageSplitter<string>>.Instance);

var source = IntegrationEnvelope<string>.Create(
    "apple,banana,cherry", "InventoryService", "batch.items");

var result = await splitter.SplitAsync(source);

Assert.That(result.ItemCount, Is.EqualTo(3));
Assert.That(result.TargetTopic, Is.EqualTo("items-topic"));
Assert.That(result.SourceMessageId, Is.EqualTo(source.MessageId));
Assert.That(result.SplitEnvelopes[0].Payload, Is.EqualTo("apple"));
Assert.That(result.SplitEnvelopes[1].Payload, Is.EqualTo("banana"));
Assert.That(result.SplitEnvelopes[2].Payload, Is.EqualTo("cherry"));
```

### Exercise 2: Split preserves CorrelationId and sets CausationId

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var strategy = new FuncSplitStrategy<string>(s => new[] { s });

var options = Options.Create(new SplitterOptions { TargetTopic = "topic" });
var splitter = new MessageSplitter<string>(
    strategy, producer, options,
    NullLogger<MessageSplitter<string>>.Instance);

var source = IntegrationEnvelope<string>.Create(
    "payload", "Service", "event.type");

var result = await splitter.SplitAsync(source);

var splitEnv = result.SplitEnvelopes[0];
Assert.That(splitEnv.CorrelationId, Is.EqualTo(source.CorrelationId));
Assert.That(splitEnv.CausationId, Is.EqualTo(source.MessageId));
Assert.That(splitEnv.MessageId, Is.Not.EqualTo(source.MessageId));
```

### Exercise 3: No target topic configured throws

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var strategy = new FuncSplitStrategy<string>(s => new[] { s });

var options = Options.Create(new SplitterOptions { TargetTopic = "" });
var splitter = new MessageSplitter<string>(
    strategy, producer, options,
    NullLogger<MessageSplitter<string>>.Instance);

var source = IntegrationEnvelope<string>.Create("data", "Svc", "evt");

Assert.ThrowsAsync<InvalidOperationException>(
    () => splitter.SplitAsync(source));
```

### Exercise 4: Zero items returns empty result, no publish

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var strategy = new FuncSplitStrategy<string>(_ => Array.Empty<string>());

var options = Options.Create(new SplitterOptions { TargetTopic = "topic" });
var splitter = new MessageSplitter<string>(
    strategy, producer, options,
    NullLogger<MessageSplitter<string>>.Instance);

var source = IntegrationEnvelope<string>.Create("empty", "Svc", "evt");

var result = await splitter.SplitAsync(source);

Assert.That(result.ItemCount, Is.EqualTo(0));
Assert.That(result.SplitEnvelopes, Is.Empty);
await producer.DidNotReceive()
    .PublishAsync(Arg.Any<IntegrationEnvelope<string>>(),
        Arg.Any<string>(), Arg.Any<CancellationToken>());
```

### Exercise 5: JsonArraySplitStrategy splits top-level array

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var splitOptions = Options.Create(new SplitterOptions { TargetTopic = "json-items" });
var strategy = new JsonArraySplitStrategy(splitOptions);

var splitter = new MessageSplitter<JsonElement>(
    strategy, producer, splitOptions,
    NullLogger<MessageSplitter<JsonElement>>.Instance);

var jsonArray = JsonSerializer.Deserialize<JsonElement>(
    """[{"id":1},{"id":2},{"id":3}]""");

var source = IntegrationEnvelope<JsonElement>.Create(
    jsonArray, "BatchService", "batch.created");

var result = await splitter.SplitAsync(source);

Assert.That(result.ItemCount, Is.EqualTo(3));
Assert.That(result.SplitEnvelopes[0].Payload.GetProperty("id").GetInt32(), Is.EqualTo(1));
Assert.That(result.SplitEnvelopes[1].Payload.GetProperty("id").GetInt32(), Is.EqualTo(2));
Assert.That(result.SplitEnvelopes[2].Payload.GetProperty("id").GetInt32(), Is.EqualTo(3));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial20/Lab.cs`](../tests/TutorialLabs/Tutorial20/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial20.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial20/Exam.cs`](../tests/TutorialLabs/Tutorial20/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial20.Exam"
```

---

**Previous: [← Tutorial 19 — Content Filter](19-content-filter.md)** | **Next: [Tutorial 21 — Aggregator →](21-aggregator.md)**
