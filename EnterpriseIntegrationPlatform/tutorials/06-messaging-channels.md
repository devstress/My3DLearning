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
        CancellationToken cancellationToken = default);

    Task ReceiveAsync<T>(
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
        CancellationToken cancellationToken = default);

    Task SubscribeAsync<T>(
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

The **Datatype Channel** routes messages by their content type. Different formats (JSON, XML, CSV) go to different handlers.

```csharp
// src/Ingestion/Channels/IDatatypeChannel.cs
public interface IDatatypeChannel
{
    Task RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);

    Task RegisterHandlerAsync(
        string contentType,
        Func<object, Task> handler,
        CancellationToken cancellationToken = default);
}
```

### How It Works

```
                  ┌─→ [JSON Handler]    (content-type: application/json)
Message ─→ Route ─┤
                  ├─→ [XML Handler]     (content-type: application/xml)
                  │
                  └─→ [CSV Handler]     (content-type: text/csv)
```

**Use when:** You receive messages in multiple formats and each format needs different processing logic.

---

## Invalid Message Channel

The **Invalid Message Channel** quarantines messages that fail validation. Instead of silently dropping bad messages, they're routed to a special channel for inspection.

```csharp
// src/Ingestion/Channels/IInvalidMessageChannel.cs
public interface IInvalidMessageChannel
{
    Task RouteInvalidAsync<T>(
        IntegrationEnvelope<T> envelope,
        string validationError,
        CancellationToken cancellationToken = default);
}
```

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
public interface IMessagingBridge
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
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

## Exercises

1. **Channel selection**: For each scenario, choose the channel type:
   - Processing incoming purchase orders (one processor per order)
   - Notifying 5 different systems when a shipment is dispatched
   - Handling messages that arrive as XML or JSON from different partners
   - Quarantining messages with missing required fields

2. **Bridge design**: Your company uses Kafka for everything but wants to add NATS for new microservices. Design a Messaging Bridge that keeps both systems in sync.

3. **Dead Letter Channel**: How does the Invalid Message Channel relate to the Dead Letter Channel? When would you use each?

---

**Previous: [← Tutorial 05 — Message Brokers](05-message-brokers.md)** | **Next: [Tutorial 07 — Temporal Workflows →](07-temporal-workflows.md)**
