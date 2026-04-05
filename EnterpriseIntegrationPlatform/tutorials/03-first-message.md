# Tutorial 03 — Your First Message

## What You'll Learn

- Create an `IntegrationEnvelope<T>` message
- Publish it to a message broker
- Consume it from the broker
- Understand the message lifecycle

---

## The IntegrationEnvelope

Every message in the platform is wrapped in an `IntegrationEnvelope<T>`. This is the canonical message format — no matter where a message comes from or where it's going, it always travels inside an envelope.

```csharp
// Location: src/Contracts/IntegrationEnvelope.cs

public record IntegrationEnvelope<T>
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Source { get; init; }
    public string MessageType { get; init; }
    public string SchemaVersion { get; init; } = "1.0";
    public T Payload { get; init; }
    public MessagePriority Priority { get; init; }
    public string? ReplyTo { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public int? SequenceNumber { get; init; }
    public int? TotalCount { get; init; }
    public MessageIntent? Intent { get; init; }
    public Dictionary<string, string> Metadata { get; init; }
}
```

### Key Fields Explained

| Field | Purpose |
|-------|---------|
| `MessageId` | Unique identifier for this specific message |
| `CorrelationId` | Links related messages together (e.g., a split batch) |
| `CausationId` | The MessageId of the message that caused this one |
| `Source` | Where the message originated (e.g., "order-system") |
| `MessageType` | Describes the payload type (e.g., "OrderCreated") |
| `Payload` | The actual message content (generic type `T`) |
| `Priority` | Low, Normal, High, or Critical |
| `SchemaVersion` | Schema version of the message contract (default: `"1.0"`) |
| `Intent` | Command, Document, or Event — nullable (EIP message construction patterns) |
| `Metadata` | Key-value pairs for headers (TraceId, ContentType, etc.) |

---

## Creating Your First Message

Here's how to create an envelope carrying an order payload:

```csharp
// Define your payload
public record OrderPayload(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency);

// Create the envelope
var order = new OrderPayload("ORD-001", "CUST-42", 149.99m, "USD");

var envelope = new IntegrationEnvelope<OrderPayload>
{
    MessageId = Guid.NewGuid(),
    CorrelationId = Guid.NewGuid(),
    Timestamp = DateTimeOffset.UtcNow,
    Source = "order-system",
    MessageType = "OrderCreated",
    Payload = order,
    Priority = MessagePriority.Normal,
    Intent = MessageIntent.Event,
    Metadata = new Dictionary<string, string>
    {
        [MessageHeaders.ContentType] = "application/json",
        [MessageHeaders.SourceTopic] = "orders.created"
    }
};
```

### What Each Field Means in This Context

- **MessageId** — A unique ID for this specific order event
- **CorrelationId** — Links all messages related to this order (the initial event, any transforms, delivery confirmations)
- **Source** — The "order-system" produced this message
- **MessageType** — "OrderCreated" tells consumers what happened
- **Intent** — `Event` means "something happened" (vs. `Command` = "do something" or `Document` = "here's data")

---

## Publishing to a Broker

The platform abstracts the broker behind `IMessageBrokerProducer`:

```csharp
// Location: src/Ingestion/IMessageBrokerProducer.cs

public interface IMessageBrokerProducer
{
    Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default);
}
```

Publishing our order:

```csharp
// Inject the producer via DI
public class OrderService(IMessageBrokerProducer producer)
{
    public async Task CreateOrderAsync(OrderPayload order)
    {
        var envelope = new IntegrationEnvelope<OrderPayload>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "order-system",
            MessageType = "OrderCreated",
            Payload = order,
            Priority = MessagePriority.Normal,
            Intent = MessageIntent.Event,
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ContentType] = "application/json"
            }
        };

        await producer.PublishAsync(envelope, "orders.created");
    }
}
```

The producer publishes to the **configured broker** — NATS JetStream by default, Kafka for streaming, or Pulsar for production. The code doesn't change when you switch brokers.

---

## Consuming from a Broker

The consumer side uses `IMessageBrokerConsumer`:

```csharp
// Location: src/Ingestion/IMessageBrokerConsumer.cs

public interface IMessageBrokerConsumer
{
    Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
```

Consuming our order:

```csharp
public class OrderProcessor(IMessageBrokerConsumer consumer)
{
    public async Task StartAsync(CancellationToken ct)
    {
        await consumer.SubscribeAsync<OrderPayload>(
            topic: "orders.created",
            consumerGroup: "order-processors",
            handler: async envelope =>
            {
                Console.WriteLine($"Received order: {envelope.Payload.OrderId}");
                Console.WriteLine($"  Amount: {envelope.Payload.Amount} {envelope.Payload.Currency}");
                Console.WriteLine($"  CorrelationId: {envelope.CorrelationId}");
                Console.WriteLine($"  Timestamp: {envelope.Timestamp}");
            },
            cancellationToken: ct);
    }
}
```

### Consumer Groups

The `consumerGroup` parameter is important:

- **Same consumer group** = messages are distributed across consumers (load balancing)
- **Different consumer groups** = each group gets every message (fan-out)

This maps directly to the EIP patterns:
- Same group → **Competing Consumers** pattern
- Different groups → **Publish-Subscribe Channel** pattern

---

## The Message Lifecycle

When you publish a message, here's what happens:

```
1. CREATE      → IntegrationEnvelope created with unique MessageId
2. PUBLISH     → IMessageBrokerProducer publishes to broker topic
3. PERSIST     → Broker durably stores the message (Kafka log / NATS stream)
4. CONSUME     → IMessageBrokerConsumer picks up the message
5. WORKFLOW    → Temporal workflow orchestrates processing
6. ACTIVITIES  → Validate → Transform → Route → Deliver
7. ACK/NACK   → Success = Ack published; Failure = Nack published
8. OBSERVE     → OpenTelemetry traces, logs, and metrics recorded at every step
```

This lifecycle is the foundation of everything in the platform. Every tutorial builds on this flow.

---

## How It Connects to EIP Patterns

What we just did touches several EIP patterns:

| Pattern | Where We Used It |
|---------|-----------------|
| **Message** | `IntegrationEnvelope<T>` wraps our payload |
| **Message Channel** | The `"orders.created"` topic is a channel |
| **Document Message** / **Event Message** | The `Intent` field distinguishes message types |
| **Correlation Identifier** | `CorrelationId` links related messages |
| **Envelope Wrapper** | The envelope wraps the raw `OrderPayload` with metadata |
| **Format Indicator** | `MessageHeaders.ContentType` tells consumers the format |

---

## Writing a Test

The platform uses NUnit 4.4.0 for testing. Here's how you'd test envelope creation:

```csharp
[TestFixture]
public class IntegrationEnvelopeTests
{
    [Test]
    public void Create_SetsMessageId()
    {
        var envelope = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "test",
            MessageType = "TestMessage",
            Payload = "hello",
            Priority = MessagePriority.Normal,
            Intent = MessageIntent.Document,
            Metadata = new Dictionary<string, string>()
        };

        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.Payload, Is.EqualTo("hello"));
        Assert.That(envelope.Intent, Is.EqualTo(MessageIntent.Document));
    }
}
```

> **Testing convention:** The platform uses NUnit with `[SetUp]` for per-test initialization and NSubstitute for mocking. See `rules/coding-standards.md` for full conventions.

---

## Exercises

1. **Trace a CorrelationId**: Imagine you publish a message, it gets split into 5 parts, each part gets transformed, and then they're aggregated back together. Which field ensures they all stay linked? What would `CausationId` be set to on each split message?

2. **Choose the Intent**: For each scenario, pick the correct `MessageIntent`:
   - "Process this payment" → ?
   - "Here is the quarterly report" → ?
   - "A new customer registered" → ?

3. **Broker independence**: Why does the platform use `IMessageBrokerProducer` instead of calling Kafka/NATS directly? What happens when you switch from NATS to Pulsar?

---

**Previous: [← Tutorial 02 — Environment Setup](02-environment-setup.md)** | **Next: [Tutorial 04 — The Integration Envelope →](04-integration-envelope.md)**
