# Tutorial 03 — Your First Message

Create an `IntegrationEnvelope<T>`, publish it through a mocked broker, and consume it on the other side using NSubstitute.

## Key Types

```csharp
// src/Contracts/IntegrationEnvelope.cs — static factory creates envelopes with auto-generated IDs
public record IntegrationEnvelope<T>
{
    public static IntegrationEnvelope<T> Create(T payload, string source, string messageType,
        Guid? correlationId = null);
    // MessageId, CorrelationId, Timestamp auto-generated
}

// src/Ingestion/IMessageBrokerProducer.cs
public interface IMessageBrokerProducer
{
    Task PublishAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct = default);
}

// src/Ingestion/IMessageBrokerConsumer.cs
public interface IMessageBrokerConsumer : IAsyncDisposable
{
    Task SubscribeAsync<T>(string topic, string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler, CancellationToken ct = default);
}
```

## Exercises

### 1. Create an envelope with a string payload

```csharp
var envelope = IntegrationEnvelope<string>.Create(
    payload: "Hello, Messaging!",
    source: "Tutorial03",
    messageType: "greeting");

Assert.That(envelope.Payload, Is.EqualTo("Hello, Messaging!"));
Assert.That(envelope.Source, Is.EqualTo("Tutorial03"));
Assert.That(envelope.MessageType, Is.EqualTo("greeting"));
Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
```

### 2. Create an envelope with a domain object payload

```csharp
public sealed record OrderPayload(string OrderId, string Product, int Quantity);

var order = new OrderPayload("ORD-100", "Gadget", 3);

var envelope = IntegrationEnvelope<OrderPayload>.Create(
    payload: order,
    source: "OrderService",
    messageType: "order.created");

Assert.That(envelope.Payload, Is.EqualTo(order));
Assert.That(envelope.Payload.OrderId, Is.EqualTo("ORD-100"));
Assert.That(envelope.Payload.Product, Is.EqualTo("Gadget"));
Assert.That(envelope.Payload.Quantity, Is.EqualTo(3));
```

### 3. Publish through a mocked producer and verify the call

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var envelope = IntegrationEnvelope<string>.Create(
    "first-message", "Tutorial03", "demo.publish");

await producer.PublishAsync(envelope, "demo-topic");

await producer.Received(1).PublishAsync(
    Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "first-message"),
    Arg.Is("demo-topic"),
    Arg.Any<CancellationToken>());
```

### 4. Subscribe with a mocked consumer and simulate message delivery

```csharp
var consumer = Substitute.For<IMessageBrokerConsumer>();
Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

consumer.SubscribeAsync<string>(
        Arg.Any<string>(), Arg.Any<string>(),
        Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
        Arg.Any<CancellationToken>())
    .Returns(Task.CompletedTask);

await consumer.SubscribeAsync<string>(
    "demo-topic", "demo-group", msg => Task.CompletedTask);

var envelope = IntegrationEnvelope<string>.Create(
    "consumed-payload", "Producer", "demo.event");

Assert.That(capturedHandler, Is.Not.Null);

IntegrationEnvelope<string>? received = null;
capturedHandler = msg => { received = msg; return Task.CompletedTask; };
await capturedHandler(envelope);

Assert.That(received, Is.Not.Null);
Assert.That(received!.Payload, Is.EqualTo("consumed-payload"));
```

### 5. Verify subscribe was called with correct topic and consumer group

```csharp
var consumer = Substitute.For<IMessageBrokerConsumer>();

await consumer.SubscribeAsync<string>(
    "events-topic", "my-consumer-group", _ => Task.CompletedTask);

await consumer.Received(1).SubscribeAsync<string>(
    Arg.Is("events-topic"),
    Arg.Is("my-consumer-group"),
    Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
    Arg.Any<CancellationToken>());
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial03/Lab.cs`](../tests/TutorialLabs/Tutorial03/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial03.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial03/Exam.cs`](../tests/TutorialLabs/Tutorial03/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial03.Exam"
```

---

**Previous: [← Tutorial 02 — Environment Setup](02-environment-setup.md)** | **Next: [Tutorial 04 — The Integration Envelope →](04-integration-envelope.md)**
