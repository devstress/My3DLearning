# Tutorial 01 — Introduction to Enterprise Integration

Enterprise integration connects applications through messaging. This platform implements 65+ EIP patterns in .NET 10.

## Key Types

```csharp
// The universal message wrapper — every message in the platform is an IntegrationEnvelope<T>
// src/Contracts/IntegrationEnvelope.cs
public record IntegrationEnvelope<T>
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string Source { get; init; }
    public string MessageType { get; init; }
    public T Payload { get; init; }
    public MessagePriority Priority { get; init; }
    public string SchemaVersion { get; init; }
    public MessageIntent? Intent { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}

// Publish and consume via broker abstractions
// src/Ingestion/IMessageBrokerProducer.cs
public interface IMessageBrokerProducer
{
    Task PublishAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct = default);
}

// src/Ingestion/IMessageBrokerConsumer.cs
public interface IMessageBrokerConsumer : IAsyncDisposable
{
    Task SubscribeAsync<T>(string topic, Func<IntegrationEnvelope<T>, Task> handler, CancellationToken ct = default);
}

// Real channels that wrap the broker interfaces
// src/Ingestion/Channels/PointToPointChannel.cs — queue semantics, one consumer per message
// src/Ingestion/Channels/PublishSubscribeChannel.cs — fan-out, every subscriber gets every message
```

## Exercises

### 1. Send a command through a real PointToPointChannel

```csharp
var broker = new MockEndpoint("broker");
var channel = new PointToPointChannel(broker, broker, NullLogger<PointToPointChannel>.Instance);

var order = IntegrationEnvelope<string>.Create(
    "PlaceOrder:ORD-001", "WebApp", "order.place") with
{
    Intent = MessageIntent.Command,
};
await channel.SendAsync(order, "orders-queue", CancellationToken.None);

// The channel published to the broker — message arrived
broker.AssertReceivedOnTopic("orders-queue", 1);
```

### 2. Subscribe and receive through a real channel

```csharp
IntegrationEnvelope<string>? received = null;
await channel.ReceiveAsync<string>("orders-queue", "processor",
    msg => { received = msg; return Task.CompletedTask; }, CancellationToken.None);

await broker.SendAsync(order);
// Handler was invoked — received is now populated
```

### 3. Fan-out with PublishSubscribeChannel

```csharp
var channel = new PublishSubscribeChannel(broker, broker, NullLogger<PublishSubscribeChannel>.Instance);

await channel.SubscribeAsync<string>("events-topic", "audit-service",
    msg => { /* audit */ return Task.CompletedTask; }, CancellationToken.None);
await channel.SubscribeAsync<string>("events-topic", "notification-service",
    msg => { /* notify */ return Task.CompletedTask; }, CancellationToken.None);

await channel.PublishAsync(evt, "events-topic", CancellationToken.None);
// Both subscribers receive the message
```

### 4. Multi-hop pipeline: P2P → handler → PubSub

```csharp
// Handler receives from P2P, enriches, and publishes to PubSub
await inputChannel.ReceiveAsync<string>("ingest-queue", "enricher",
    async msg =>
    {
        var enriched = msg with
        {
            Metadata = new Dictionary<string, string> { ["enriched"] = "true" },
        };
        await fanoutChannel.PublishAsync(enriched, "enriched-events", CancellationToken.None);
    }, CancellationToken.None);
```

### 5. Causation chain through real channels

```csharp
var command = IntegrationEnvelope<string>.Create("CreateUser", "WebApp", "user.create") with
{
    Intent = MessageIntent.Command,
};
var evt = IntegrationEnvelope<string>.Create("UserCreated", "UserService", "user.created",
    correlationId: command.CorrelationId, causationId: command.MessageId) with
{
    Intent = MessageIntent.Event,
};
// Both flow through real channels — causation chain preserved
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial01/Lab.cs`](../tests/TutorialLabs/Tutorial01/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial01.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial01/Exam.cs`](../tests/TutorialLabs/Tutorial01/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial01.Exam"
```

---

**Next: [Tutorial 02 — Setting Up Your Environment →](02-environment-setup.md)**
