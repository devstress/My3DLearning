# Tutorial 05 — Message Brokers

## What You'll Learn

- The three message brokers in the platform and when to use each
- Kafka for event streaming vs. NATS/Pulsar for task delivery
- How the broker abstraction lets you switch without code changes
- Head-of-line blocking and why it matters

---

## The Three-Broker Architecture

The platform doesn't use just one message broker — it uses the **right broker for each job**:

```
┌──────────────────────────────────────────────────────────┐
│                    Broker Layer                            │
│                                                          │
│  ┌──────────────┐  ┌───────────────┐  ┌───────────────┐ │
│  │    Kafka      │  │     NATS      │  │    Pulsar     │ │
│  │              │  │  JetStream    │  │  Key_Shared   │ │
│  │  Event       │  │              │  │              │ │
│  │  Streaming   │  │  Task        │  │  Task        │ │
│  │  + Audit     │  │  Delivery    │  │  Delivery    │ │
│  │              │  │  (default)   │  │  (prod scale) │ │
│  └──────────────┘  └───────────────┘  └───────────────┘ │
│                                                          │
│         IMessageBrokerProducer / IMessageBrokerConsumer   │
└──────────────────────────────────────────────────────────┘
```

### When to Use Each Broker

| Broker | Best For | Why |
|--------|----------|-----|
| **Kafka** | Event streams, audit logs, fan-out analytics | Partitioned log with ordering guarantees; excellent for high-throughput streaming |
| **NATS JetStream** | Task delivery (default), dev/test | Lightweight single binary; per-subject filtering; no head-of-line blocking |
| **Apache Pulsar** | Large-scale production task delivery | Key_Shared subscriptions; messages for recipient A never block recipient B |

---

## The Head-of-Line Blocking Problem

This is the most important concept for understanding the broker choice.

### Kafka's Model

Kafka organizes messages into **partitions**. Within a consumer group, each partition is consumed by **exactly one consumer**:

```
Topic: orders (3 partitions)

Partition 0: [msg1] [msg3] [msg6] ──→ Consumer A
Partition 1: [msg2] [msg4] [msg7] ──→ Consumer B
Partition 2: [msg5] [msg8] [msg9] ──→ Consumer C
```

**The problem**: If `msg3` takes 30 seconds to process, `msg6` waits behind it. This is **head-of-line (HOL) blocking**. Message 6 might be for a completely different customer, but it's stuck.

### NATS JetStream's Model

NATS uses **subjects** with **queue groups**. Any consumer in the group can pick up any message:

```
Subject: orders.created

Queue Group "processors":
  Consumer A picks msg1 → processing...
  Consumer B picks msg2 → done
  Consumer B picks msg3 → done    (B is fast, takes more)
  Consumer C picks msg4 → done
```

No single slow message blocks others — each consumer processes independently.

### Pulsar Key_Shared

Pulsar with **Key_Shared** subscriptions routes messages by key:

```
Key_Shared on recipientId:

recipientId=alice → Consumer A (all Alice's messages, in order)
recipientId=bob   → Consumer B (all Bob's messages, in order)
recipientId=carol → Consumer C (all Carol's messages, in order)
```

**Recipient A never blocks Recipient B**, even with millions of recipients. Each recipient's messages stay ordered.

---

## The Broker Abstraction

The platform abstracts all three brokers behind two interfaces:

```csharp
// src/Ingestion/IMessageBrokerProducer.cs
public interface IMessageBrokerProducer
{
    Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default);
}

// src/Ingestion/IMessageBrokerConsumer.cs
public interface IMessageBrokerConsumer : IAsyncDisposable
{
    Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default);
}
```

### Switching Brokers

The broker is a **deployment-time configuration** — no code changes required:

```csharp
// In DI registration (simplified)
services.AddIngestion(options =>
{
    options.BrokerType = BrokerType.NatsJetStream;  // Default
    // options.BrokerType = BrokerType.Kafka;        // For streaming
    // options.BrokerType = BrokerType.Pulsar;       // For production scale
});
```

Your publishing and consuming code stays identical:

```csharp
// This code works with ANY broker
await producer.PublishAsync(envelope, "orders.created");

await consumer.SubscribeAsync<OrderPayload>(
    "orders.created", "processors", HandleOrder);
```

---

## Kafka: The Event Streaming Backbone

Kafka excels at:

- **Ordered event streams** — Events within a partition maintain strict order
- **Audit logging** — Immutable append-only log with configurable retention
- **Fan-out analytics** — Multiple consumer groups each get every message
- **Replay** — Read from any offset to reprocess historical events

The platform uses Kafka specifically for:

| Use Case | Topic Pattern |
|----------|---------------|
| Audit events | `audit.*` |
| Event streaming | `events.*` |
| Analytics fan-out | `analytics.*` |
| Ack/Nack notifications | `notifications.ack.*`, `notifications.nack.*` |

### Kafka Configuration

```csharp
// src/Ingestion.Kafka/ provides the Kafka implementation
// Key settings:
// - Bootstrap servers (connection)
// - Consumer group IDs
// - Partition count per topic
// - Replication factor
// - Retention period
```

---

## NATS JetStream: The Default Task Broker

NATS JetStream is the **default broker** for task-oriented message delivery:

- **Lightweight** — Single binary, minimal configuration
- **Per-subject filtering** — Subscribe to specific subjects with wildcards
- **Queue groups** — Built-in competing consumers without HOL blocking
- **Cloud-native** — Perfect for local development and cloud deployments

```
NATS Subject Hierarchy:

eip.>                          (all platform messages)
eip.orders.>                   (all order messages)
eip.orders.created             (specific subject)
eip.orders.validated
eip.orders.processed
eip.dlq.>                     (all dead letter messages)
```

### NATS Wildcards

| Pattern | Matches |
|---------|---------|
| `eip.orders.*` | `eip.orders.created`, `eip.orders.validated` |
| `eip.orders.>` | `eip.orders.created`, `eip.orders.us.created` (multi-level) |
| `eip.*.created` | `eip.orders.created`, `eip.invoices.created` |

---

## Apache Pulsar: Production Scale

For large-scale on-premises production, Pulsar with **Key_Shared** subscriptions provides:

- **Key-based routing** — All messages for the same key go to the same consumer
- **No cross-key blocking** — Different keys are processed independently
- **Built-in multi-tenancy** — Lightweight topic creation scales to millions of tenants
- **Tiered storage** — Automatic offloading to S3/GCS for cost efficiency

---

## Choosing the Right Broker

```
Decision Tree:

Is this an event stream or audit log?
  → YES → Use Kafka

Is this task delivery (process and acknowledge)?
  → YES →
    Running locally or in the cloud?
      → Local/cloud → Use NATS JetStream (default)
    Large-scale on-premises production?
      → On-prem → Use Apache Pulsar with Key_Shared
```

---

## Lab

**Objective:** Design a broker topic hierarchy for a multi-tenant system and analyze how different broker architectures affect **scalability** and **message ordering guarantees**.

### Step 1: Design a Multi-Region Topic Hierarchy

Design a NATS subject hierarchy for a multi-region e-commerce system with: orders, payments, and refunds across three regions (US, EU, APAC). Use NATS conventions (`.` for levels, `*` for single-level wildcard, `>` for multi-level wildcard):

```
eip.{region}.{domain}.{event}
Example: eip.us.orders.created
```

Write subscriber patterns for: (a) all events in EU: `eip.eu.>`, (b) all order events globally: `eip.*.orders.*`, (c) only payment completions in APAC: `eip.apac.payments.completed`.

Explain how this hierarchy enables **horizontal scalability** — new regions can be added without changing existing subscribers.

### Step 2: Compare Broker Scalability Characteristics

Create a comparison table for Kafka, NATS JetStream, and Pulsar:

| Characteristic | Kafka | NATS JetStream | Pulsar |
|---------------|-------|----------------|--------|
| Ordering guarantee | Per-partition | Per-subject | Per-key (Key_Shared) |
| HOL blocking risk | ? | ? | ? |
| Multi-tenant isolation | ? | ? | ? |
| Scale-out mechanism | ? | ? | ? |

For each cell, explain the implication for a platform processing 10,000 messages/second from 50 tenants.

### Step 3: Design for Atomicity Across Broker Switches

The platform uses `IMessageBrokerProducer` / `IMessageBrokerConsumer` to abstract the broker. Describe a scenario where switching from NATS to Kafka for a specific message type would change the **atomicity** guarantees (hint: Kafka's transactional producer vs. NATS at-least-once). What compensating design would the platform need?

## Exam

1. What is head-of-line (HOL) blocking and why is it a **scalability** problem?
   - A) HOL blocking occurs when a slow message in a partition delays all subsequent messages; NATS queue groups avoid it because any available consumer can pick up any message
   - B) HOL blocking is a network-layer issue that all brokers handle identically
   - C) HOL blocking only affects messages with `MessagePriority.Low`
   - D) HOL blocking means messages are delivered out of order

2. Why does the platform define `IMessageBrokerProducer` and `IMessageBrokerConsumer` as abstractions rather than coding directly against a specific broker SDK?
   - A) The broker SDKs do not support .NET 10
   - B) It allows the broker implementation to be swapped at deployment time without changing application code — enabling different scalability and atomicity trade-offs per workload
   - C) Abstractions are required by the C# compiler for async methods
   - D) Each broker uses a different serialization format

3. When would you choose Apache Pulsar's Key_Shared subscription over Kafka's partition-based consumption for **multi-tenant scalability**?
   - A) When you need strict global order across all keys
   - B) When you want per-key ordering without cross-key head-of-line blocking — one tenant's slow processing should not affect others
   - C) When your messages do not have any key
   - D) When you require messages to be stored for less than 24 hours

---

**Previous: [← Tutorial 04 — Integration Envelope](04-integration-envelope.md)** | **Next: [Tutorial 06 — Messaging Channels →](06-messaging-channels.md)**
