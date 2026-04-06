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
    public required Guid MessageId { get; init; }
    public required Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Source { get; init; }
    public required string MessageType { get; init; }
    public string SchemaVersion { get; init; } = "1.0";
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public required T Payload { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public string? ReplyTo { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public int? SequenceNumber { get; init; }
    public int? TotalCount { get; init; }
    public MessageIntent? Intent { get; init; }
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

public interface IMessageBrokerConsumer : IAsyncDisposable
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

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial03/Lab.cs`](../tests/TutorialLabs/Tutorial03/Lab.cs)

**Objective:** Create an `IntegrationEnvelope<T>`, publish it to a Message Channel, and trace the Correlation Identifier through a publish-subscribe round-trip.

### Step 1: Create and Inspect an Integration Envelope

Using the static factory method, create an envelope and inspect the EIP Message pattern fields it populates automatically:

```csharp
var envelope = IntegrationEnvelope<string>.Create(
    payload: "{\"orderId\": 42, \"amount\": 99.95}",
    source: "OrderService",
    messageType: "order.created");
```

Verify: `MessageId` is a non-empty `Guid` (Message Identity), `CorrelationId` is generated (Correlation Identifier pattern), `Timestamp` is UTC (for ordering and expiration), and `Priority` defaults to `Normal`.

### Step 2: Trace the Message Lifecycle

Draw the 8-step message lifecycle from the tutorial on paper or whiteboard:

```
CREATE → PUBLISH → PERSIST → CONSUME → WORKFLOW → ACTIVITIES → ACK/NACK → OBSERVE
```

For each step, identify: (a) which EIP pattern applies, (b) where **atomicity** is enforced (hint: PERSIST ensures durability, WORKFLOW ensures all-or-nothing), and (c) which step enables **scalability** through parallel processing (hint: CONSUME with consumer groups).

### Step 3: Design a Multi-Consumer Topology

Imagine you need both an **analytics service** and a **billing service** to receive `order.created` messages. Design the consumer group configuration:

- Analytics: consumer group = `"analytics-processors"` (receives every message)
- Billing: consumer group = `"billing-processors"` (receives every message)
- Within billing, 3 instances share the load

Explain which EIP patterns are at play: **Publish-Subscribe Channel** (different groups) vs. **Competing Consumers** (same group, multiple instances). Why does this design scale without code changes?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial03/Exam.cs`](../tests/TutorialLabs/Tutorial03/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 02 — Environment Setup](02-environment-setup.md)** | **Next: [Tutorial 04 — The Integration Envelope →](04-integration-envelope.md)**
