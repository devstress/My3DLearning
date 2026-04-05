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

## Lab Exercise

**Objective:** Create an `IntegrationEnvelope<T>`, publish it through `IMessageBrokerProducer`, and consume it with `IMessageBrokerConsumer` using mocked dependencies.

### Step 1: Create an Integration Envelope

In a new or existing C# file, create an `IntegrationEnvelope<string>` using the static factory method:

```csharp
var envelope = IntegrationEnvelope<string>.Create(
    payload: "{\"orderId\": 42, \"amount\": 99.95}",
    source: "OrderService",
    messageType: "order.created");
```

Inspect the returned envelope — verify that `MessageId` is a non-empty `Guid`, `Timestamp` is close to `DateTimeOffset.UtcNow`, and `Priority` defaults to `MessagePriority.Normal`.

### Step 2: Mock a Publish-Subscribe Round-Trip

Using NSubstitute, create mocks for `IMessageBrokerProducer` and `IMessageBrokerConsumer`. Call `PublishAsync` on the producer with your envelope and the topic `"eip.orders.created"`. Then set up the consumer's `SubscribeAsync` to capture the handler callback and invoke it with the same envelope. Verify the handler receives an envelope whose `CorrelationId` matches the original.

### Step 3: Write a Unit Test

In `tests/UnitTests/`, create a test class named `FirstMessageTests`. Add a test method called `Create_WithValidParameters_SetsIdentityFieldsCorrectly` that calls `IntegrationEnvelope<string>.Create()` with a payload, source, and message type, then asserts: (1) `MessageId` is not `Guid.Empty`, (2) `CorrelationId` is not `Guid.Empty`, (3) `Source` equals the provided value, and (4) `MessageType` equals the provided value.

## Knowledge Check

1. What is the purpose of the `CorrelationId` field on `IntegrationEnvelope<T>`?
   - A) It uniquely identifies a single message in the broker's storage
   - B) It links all messages that belong to the same logical business transaction, even across splits and transformations
   - C) It stores the consumer group name for load balancing
   - D) It provides the encryption key for message payloads

2. When the platform publishes a message through `IMessageBrokerProducer`, what allows switching from NATS to Kafka without changing application code?
   - A) The message is automatically converted to a different format by the broker
   - B) The `IMessageBrokerProducer` interface abstracts the broker, so a different implementation is injected at deployment time
   - C) Kafka and NATS use identical wire protocols
   - D) The `IntegrationEnvelope<T>` handles broker selection internally

3. Which `MessageIntent` value should be assigned to a message that instructs a downstream service to perform an action (e.g., "process this payment")?
   - A) `MessageIntent.Event`
   - B) `MessageIntent.Document`
   - C) `MessageIntent.Command`
   - D) `MessageIntent.Query`

---

**Previous: [← Tutorial 02 — Environment Setup](02-environment-setup.md)** | **Next: [Tutorial 04 — The Integration Envelope →](04-integration-envelope.md)**
