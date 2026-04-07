# Tutorial 05 — Message Brokers

Configure the three broker implementations (NATS JetStream, Kafka, Pulsar) via `BrokerOptions` and publish messages through the broker abstraction.

## Learning Objectives

After completing this tutorial you will be able to:

1. Configure `BrokerOptions` for each supported broker protocol (NATS, Kafka, Pulsar, Postgres)
2. Publish messages through `IMessageBrokerProducer` — the protocol-agnostic abstraction
3. Route messages to multiple topics and verify per-topic delivery
4. Register event-driven consumers with push-based message handlers
5. Use polling consumers to retrieve batches of messages with max-message limits
6. Apply selective consumers with predicate-based priority filtering

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

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how broker configuration, protocol-agnostic
> publishing, and consumer patterns work through real NATS JetStream via Aspire.

| # | Test | Concept |
|---|------|---------|
| 1 | `BrokerOptions_Defaults_NatsJetStreamWithSectionName` | Default broker options and section name |
| 2 | `BrokerType_AllProtocols_Enumerated` | All four broker protocols enumerated |
| 3 | `Publish_NatsConfig_MessageDeliveredViaAbstraction` | Protocol-agnostic publish through real NATS |
| 4 | `Publish_MultipleTopics_PerTopicDeliveryVerified` | Multi-topic routing with per-topic verification |
| 5 | `EventDrivenConsumer_HandlerTriggeredOnMessageArrival` | Push-based event-driven consumer handler |
| 6 | `PollingConsumer_BatchRetrieval_MaxMessagesRespected` | Pull-based polling with max-message limit |
| 7 | `SelectiveConsumer_PredicateFilters_OnlyMatchingDelivered` | Priority-based predicate filtering |
| 8 | `SubscribeConsumer_MultipleHandlers_AllInvoked` | Multiple independent subscription handlers |

> 💻 [`tests/TutorialLabs/Tutorial05/Lab.cs`](../tests/TutorialLabs/Tutorial05/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial05.Lab"
```

---

## Exam — Assessment Challenges

> **Purpose:** Prove you can apply message broker patterns in realistic scenarios —
> multi-broker fan-out, selective filtering, and DI-wired pipelines.

| Difficulty | Challenge | What you prove |
|------------|-----------|---------------|
| 🟢 Starter | `Starter_MultiBrokerFanOut_AllEndpointsReceive` | Fan out one event to NATS + Kafka + Pulsar simultaneously |
| 🟡 Intermediate | `Intermediate_SelectiveConsumer_PriorityGate` | Priority-based triage with selective consumer predicate |
| 🔴 Advanced | `Advanced_DIHost_BrokerOptionsConfigured` | AspireIntegrationTestHost DI pipeline with BrokerOptions |

> 💻 [`tests/TutorialLabs/Tutorial05/Exam.cs`](../tests/TutorialLabs/Tutorial05/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial05.Exam"
```

---

**Previous: [← Tutorial 04 — Integration Envelope](04-integration-envelope.md)** | **Next: [Tutorial 06 — Messaging Channels →](06-messaging-channels.md)**
