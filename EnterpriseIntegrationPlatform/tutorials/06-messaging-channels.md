# Tutorial 06 — Messaging Channels

Point-to-Point, Publish-Subscribe, Datatype, Invalid Message, and Messaging Bridge channel patterns.

---

## Key Types

```csharp
// src/Ingestion/Channels/IPointToPointChannel.cs
public interface IPointToPointChannel
{
    Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        string channel,
        CancellationToken cancellationToken = default);

    Task ReceiveAsync<T>(
        string channel,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Ingestion/Channels/IPublishSubscribeChannel.cs
public interface IPublishSubscribeChannel
{
    Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string channel,
        CancellationToken cancellationToken = default);

    Task SubscribeAsync<T>(
        string channel,
        string subscriberId,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Ingestion/Channels/IDatatypeChannel.cs
public interface IDatatypeChannel
{
    Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);

    string ResolveChannel(string messageType);
}
```

```csharp
// src/Ingestion/Channels/IInvalidMessageChannel.cs
public interface IInvalidMessageChannel
{
    Task RouteInvalidAsync<T>(
        IntegrationEnvelope<T> envelope,
        string reason,
        CancellationToken cancellationToken = default);

    Task RouteRawInvalidAsync(
        string rawData,
        string sourceTopic,
        string reason,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Ingestion/Channels/IMessagingBridge.cs
public interface IMessagingBridge : IAsyncDisposable
{
    Task StartAsync<T>(
        string sourceChannel,
        string targetChannel,
        CancellationToken cancellationToken = default);

    long ForwardedCount { get; }
    long DuplicateCount { get; }
}
```

---

## Exercises

### 1. Point-to-Point: single consumer receives the message

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var envelope = IntegrationEnvelope<string>.Create(
    payload: "order-123",
    source: "OrderService",
    messageType: "order.created") with
{
    Intent = MessageIntent.Command,
};

await producer.PublishAsync(envelope, "orders.point-to-point");

await producer.Received(1).PublishAsync(
    Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "order-123"),
    Arg.Is("orders.point-to-point"),
    Arg.Any<CancellationToken>());
```

### 2. Publish-Subscribe: every consumer group gets a copy

```csharp
var consumer = Substitute.For<IMessageBrokerConsumer>();
var producer = Substitute.For<IMessageBrokerProducer>();

var envelope = IntegrationEnvelope<string>.Create(
    "event-data", "EventService", "event.published") with
{
    Intent = MessageIntent.Event,
};

var groups = new[] { "billing-group", "analytics-group", "notifications-group" };

foreach (var group in groups)
{
    await consumer.SubscribeAsync<string>(
        "events.pubsub", group, _ => Task.CompletedTask);
}

await producer.PublishAsync(envelope, "events.pubsub");

await consumer.Received(3).SubscribeAsync<string>(
    Arg.Is("events.pubsub"),
    Arg.Any<string>(),
    Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
    Arg.Any<CancellationToken>());

foreach (var group in groups)
{
    await consumer.Received(1).SubscribeAsync<string>(
        Arg.Is("events.pubsub"),
        Arg.Is(group),
        Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
        Arg.Any<CancellationToken>());
}
```

### 3. Datatype Channel: each message type routes to its own topic

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var orderEnvelope = IntegrationEnvelope<string>.Create(
    "new-order", "OrderService", "order.created");
var paymentEnvelope = IntegrationEnvelope<string>.Create(
    "payment-received", "PaymentService", "payment.completed");
var inventoryEnvelope = IntegrationEnvelope<string>.Create(
    "stock-updated", "InventoryService", "inventory.adjusted");

await producer.PublishAsync(orderEnvelope, "datatype.order.created");
await producer.PublishAsync(paymentEnvelope, "datatype.payment.completed");
await producer.PublishAsync(inventoryEnvelope, "datatype.inventory.adjusted");

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("datatype.order.created"),
    Arg.Any<CancellationToken>());
await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("datatype.payment.completed"),
    Arg.Any<CancellationToken>());
await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("datatype.inventory.adjusted"),
    Arg.Any<CancellationToken>());
```

### 4. Invalid Message Channel: expired envelopes

```csharp
var expired = IntegrationEnvelope<string>.Create(
    "stale-data", "LegacySystem", "legacy.update") with
{
    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
};

Assert.That(expired.IsExpired, Is.True);

var valid = IntegrationEnvelope<string>.Create(
    "fresh-data", "ModernSystem", "modern.update") with
{
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
};

Assert.That(valid.IsExpired, Is.False);

var noExpiry = IntegrationEnvelope<string>.Create(
    "persistent-data", "CoreService", "core.event");

Assert.That(noExpiry.ExpiresAt, Is.Null);
Assert.That(noExpiry.IsExpired, Is.False);
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial06/Lab.cs`](../tests/TutorialLabs/Tutorial06/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial06.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial06/Exam.cs`](../tests/TutorialLabs/Tutorial06/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial06.Exam"
```

---

**Previous: [← Tutorial 05 — Message Brokers](05-message-brokers.md)** | **Next: [Tutorial 07 — Temporal Workflows →](07-temporal-workflows.md)**
