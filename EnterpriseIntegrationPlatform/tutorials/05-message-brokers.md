# Tutorial 05 — Message Brokers

Configure the three broker implementations (NATS JetStream, Kafka, Pulsar) via `BrokerOptions` and publish messages through the broker abstraction.

## Key Types

```csharp
// src/Ingestion/BrokerType.cs
public enum BrokerType
{
    NatsJetStream = 0,  // Default — lightweight, no HOL blocking
    Kafka = 1,          // Event streaming, audit logs, fan-out
    Pulsar = 2,         // Key_Shared — per-recipient ordering at scale
}

// src/Ingestion/BrokerOptions.cs
public sealed class BrokerOptions
{
    public BrokerType BrokerType { get; set; } = BrokerType.NatsJetStream;
    public string ConnectionString { get; set; } = string.Empty;
    public int TransactionTimeoutSeconds { get; set; } = 30;
}

// src/Ingestion/IMessageBrokerProducer.cs
public interface IMessageBrokerProducer
{
    Task PublishAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct = default);
}
```

## Exercises

### 1. Configure BrokerOptions for each broker

```csharp
var nats = new BrokerOptions
{
    BrokerType = BrokerType.NatsJetStream,
    ConnectionString = "nats://localhost:15222",
    TransactionTimeoutSeconds = 30,
};
Assert.That(nats.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
Assert.That(nats.ConnectionString, Is.EqualTo("nats://localhost:15222"));

var kafka = new BrokerOptions
{
    BrokerType = BrokerType.Kafka,
    ConnectionString = "localhost:9092",
    TransactionTimeoutSeconds = 60,
};
Assert.That(kafka.BrokerType, Is.EqualTo(BrokerType.Kafka));
Assert.That(kafka.TransactionTimeoutSeconds, Is.EqualTo(60));

var pulsar = new BrokerOptions
{
    BrokerType = BrokerType.Pulsar,
    ConnectionString = "pulsar://localhost:6650",
    TransactionTimeoutSeconds = 45,
};
Assert.That(pulsar.BrokerType, Is.EqualTo(BrokerType.Pulsar));
```

### 2. Publish through a mocked NATS producer

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var envelope = IntegrationEnvelope<string>.Create(
    "nats-message", "NatsService", "nats.event");

await producer.PublishAsync(envelope, "nats-events");

await producer.Received(1).PublishAsync(
    Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "nats-message"),
    Arg.Is("nats-events"),
    Arg.Any<CancellationToken>());
```

### 3. Publish through a mocked Kafka producer

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var envelope = IntegrationEnvelope<string>.Create(
    "kafka-message", "KafkaService", "kafka.event");

await producer.PublishAsync(envelope, "kafka-events");

await producer.Received(1).PublishAsync(
    Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "kafka-message"),
    Arg.Is("kafka-events"),
    Arg.Any<CancellationToken>());
```

### 4. Publish to multiple topics and verify each

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var orderEnvelope = IntegrationEnvelope<string>.Create(
    "new-order", "OrderService", "order.created");
var paymentEnvelope = IntegrationEnvelope<string>.Create(
    "payment-received", "PaymentService", "payment.received");
var shippingEnvelope = IntegrationEnvelope<string>.Create(
    "shipment-dispatched", "ShippingService", "shipment.dispatched");

await producer.PublishAsync(orderEnvelope, "orders-topic");
await producer.PublishAsync(paymentEnvelope, "payments-topic");
await producer.PublishAsync(shippingEnvelope, "shipping-topic");

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("orders-topic"),
    Arg.Any<CancellationToken>());

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("payments-topic"),
    Arg.Any<CancellationToken>());

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("shipping-topic"),
    Arg.Any<CancellationToken>());

await producer.Received(3).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>());
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial05/Lab.cs`](../tests/TutorialLabs/Tutorial05/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial05.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial05/Exam.cs`](../tests/TutorialLabs/Tutorial05/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial05.Exam"
```

---

**Previous: [← Tutorial 04 — Integration Envelope](04-integration-envelope.md)** | **Next: [Tutorial 06 — Messaging Channels →](06-messaging-channels.md)**
