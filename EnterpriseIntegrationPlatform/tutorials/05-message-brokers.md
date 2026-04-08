# Tutorial 05 — Message Brokers

Configure the four broker implementations (NATS JetStream, Kafka, Pulsar, Postgres) via `BrokerOptions` and publish messages through the broker abstraction.

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
    Postgres = 3,       // SQL-based — no extra broker, ≤ 5 k TPS
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

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_MultiBrokerFanOut_AllEndpointsReceive` | 🟢 Starter | MultiBrokerFanOut — AllEndpointsReceive |
| 2 | `Intermediate_SelectiveConsumer_PriorityGate` | 🟡 Intermediate | SelectiveConsumer — PriorityGate |
| 3 | `Advanced_DIHost_BrokerOptionsConfigured` | 🔴 Advanced | DIHost — BrokerOptionsConfigured |

> 💻 [`tests/TutorialLabs/Tutorial05/Exam.cs`](../tests/TutorialLabs/Tutorial05/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial05.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial05.ExamAnswers"
```
---

**Previous: [← Tutorial 04 — Integration Envelope](04-integration-envelope.md)** | **Next: [Tutorial 06 — Messaging Channels →](06-messaging-channels.md)**
