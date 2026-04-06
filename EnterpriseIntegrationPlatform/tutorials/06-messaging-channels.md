# Tutorial 06 — Messaging Channels

## What You'll Learn

- The difference between Point-to-Point and Publish-Subscribe channels
- Datatype Channel for format-specific routing
- Invalid Message Channel for validation failures
- Messaging Bridge for cross-broker communication

---

## What Is a Messaging Channel?

A **Messaging Channel** is a named conduit through which messages flow. In the EIP book, a channel is the logical pipe connecting a sender to a receiver. In this platform, channels are implemented on top of the broker abstraction, adding semantic behavior.

```
                    Messaging Channels
┌─────────────────────────────────────────────────────┐
│                                                     │
│  Point-to-Point     Publish-Subscribe               │
│  ┌───────────┐      ┌───────────┐                  │
│  │  1 msg →  │      │  1 msg →  │                  │
│  │  1 consumer│      │  N consumers│                 │
│  └───────────┘      └───────────┘                  │
│                                                     │
│  Datatype           Invalid Message                 │
│  ┌───────────┐      ┌───────────┐                  │
│  │  Route by │      │  Failed   │                  │
│  │  content  │      │  validation│                  │
│  │  type     │      │  → quarantine│                │
│  └───────────┘      └───────────┘                  │
│                                                     │
│  Messaging Bridge                                   │
│  ┌───────────────────────────────────┐             │
│  │  Source broker ──→ Target broker   │             │
│  └───────────────────────────────────┘             │
└─────────────────────────────────────────────────────┘
```

---

## Point-to-Point Channel

The **Point-to-Point Channel** ensures each message is consumed by **exactly one** consumer. This is the classic work queue pattern.

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

### How It Works

```
Producer → [  Queue  ] → Consumer A  (gets msg 1)
                       → Consumer B  (gets msg 2)
                       → Consumer C  (gets msg 3)
                       
Each message goes to ONE consumer (load balanced)
```

**Use when:** You have work items that should be processed exactly once — orders to fulfill, payments to process, files to transform.

---

## Publish-Subscribe Channel

The **Publish-Subscribe Channel** broadcasts each message to **all** subscribers. Every subscriber gets every message.

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

### How It Works

```
Producer → [  Topic  ] → Subscriber A  (gets ALL messages)
                       → Subscriber B  (gets ALL messages)
                       → Subscriber C  (gets ALL messages)
                       
Each message goes to EVERY subscriber (fan-out)
```

**Use when:** Multiple independent systems need the same events — audit logging, analytics, notification services.

### Point-to-Point vs. Publish-Subscribe

| Feature | Point-to-Point | Publish-Subscribe |
|---------|---------------|-------------------|
| Delivery | One consumer per message | All subscribers per message |
| Use case | Work distribution | Event notification |
| Scaling | Add consumers to share load | Each subscriber independent |
| EIP Name | Point-to-Point Channel | Publish-Subscribe Channel |

---

## Datatype Channel

The **Datatype Channel** routes messages to a dedicated channel derived from their `MessageType`. Each distinct message type flows on its own channel, ensuring type-safe consumption. Use `ResolveChannel` to look up the channel name for a given message type.

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

### How It Works

```
                  ┌─→ [OrderCreated channel]   (messageType: "OrderCreated")
Message ─→ Publish┤
                  ├─→ [InvoiceReceived channel](messageType: "InvoiceReceived")
                  │
                  └─→ [ShipmentDispatched channel](messageType: "ShipmentDispatched")
```

**Use when:** You have distinct message types and each type should flow on its own dedicated channel.

---

## Invalid Message Channel

The **Invalid Message Channel** quarantines messages that fail validation. Instead of silently dropping bad messages, they're routed to a special channel for inspection.

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

> `RouteInvalidAsync` handles messages that were parsed into an envelope but failed validation. `RouteRawInvalidAsync` handles raw data that could not be deserialized into an envelope at all.

### How It Works

```
Message arrives → Validate
  ├─ Valid   → Continue processing
  └─ Invalid → Invalid Message Channel → Inspect / Fix / Discard
```

The `InvalidMessageEnvelope` wraps the original message with error context:
- Original envelope (preserved exactly as received)
- Validation error description
- Timestamp of rejection
- Retry count (if this was a retry attempt)

**Use when:** You need to capture and diagnose malformed messages without losing them.

---

## Messaging Bridge

The **Messaging Bridge** connects two different messaging systems. It consumes from one broker/channel and republishes to another.

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

### How It Works

```
┌──────────┐     ┌──────────────────┐     ┌──────────┐
│  NATS    │ ──→ │ Messaging Bridge │ ──→ │  Kafka   │
│ (tasks)  │     │  (consume+pub)   │     │ (events) │
└──────────┘     └──────────────────┘     └──────────┘
```

**Use when:** You need to move messages between different broker types — for example, bridging task delivery (NATS) to event streaming (Kafka) for audit.

---

## Channel Patterns in Practice

Here's how a typical message flow uses multiple channel types:

```
1. External system sends HTTP request to Gateway.Api

2. Gateway publishes to Point-to-Point channel (task delivery)
   → One worker picks it up

3. Worker validates the message
   → Invalid? → Invalid Message Channel (quarantine)
   → Valid? → Continue

4. Worker checks content type via Datatype Channel
   → JSON? → JSON processor
   → XML? → XML-to-JSON normalizer first

5. Processed result published to Publish-Subscribe channel
   → Subscriber A: Audit system (records the event)
   → Subscriber B: Analytics (updates dashboards)
   → Subscriber C: Notification service (sends confirmation)
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial06/Lab.cs`](../tests/TutorialLabs/Tutorial06/Lab.cs)

**Objective:** Classify messaging scenarios by channel type and design a channel topology that ensures **atomic delivery** and **scalable fan-out**.

### Step 1: Map Scenarios to Channel Types

For each scenario, identify the correct EIP channel pattern and the platform class that implements it:

| Scenario | Channel Pattern | Platform Class |
|----------|----------------|----------------|
| Processing purchase orders (one processor per order) | ? | `PointToPointChannel` or `PublishSubscribeChannel`? |
| Notifying 5 systems when a shipment is dispatched | ? | ? |
| Handling messages in XML or JSON from different partners | ? | ? |
| Quarantining messages with missing required fields | ? | ? |

Open `src/Ingestion/Channels/` and verify your answers against the actual implementations.

### Step 2: Design a Messaging Bridge for Broker Migration

Your company uses Kafka for all integrations but wants to add NATS for new microservices. Using the `MessagingBridge` class in `src/Ingestion/Channels/`, design a bridge configuration that:

- Reads from Kafka topic `legacy.orders.created`
- Publishes to NATS subject `eip.orders.created`
- Preserves the `CorrelationId` and all `Metadata` across the bridge

Draw the message flow and identify where **atomicity** could be lost (hint: what if the bridge crashes after reading from Kafka but before publishing to NATS?). How does the platform's Ack/Nack pattern mitigate this?

### Step 3: Evaluate Scalability of Channel Patterns

Compare Point-to-Point and Publish-Subscribe channels under high load:

- Point-to-Point with 3 competing consumers processing 10,000 messages/second
- Pub-Sub with 5 subscriber groups, each with 2 consumers

For each, explain: How does adding more consumers affect throughput? What happens to in-flight messages? Where is the bottleneck?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial06/Exam.cs`](../tests/TutorialLabs/Tutorial06/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 05 — Message Brokers](05-message-brokers.md)** | **Next: [Tutorial 07 — Temporal Workflows →](07-temporal-workflows.md)**
