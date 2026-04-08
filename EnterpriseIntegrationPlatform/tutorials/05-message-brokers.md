# Tutorial 05 — Message Brokers

Configure the five broker implementations (NATS JetStream, Kafka, Pulsar, Postgres, Northguard) via `BrokerOptions` and publish messages through the broker abstraction.

> **New:** Northguard (LinkedIn's next-generation Kafka replacement) has been added as a fifth broker.
> Northguard is currently internal to LinkedIn; this integration point is ready for when the API
> becomes externally available or for teams running inside LinkedIn's infrastructure.
> See [Northguard Scenarios](#northguard--when-to-use-it) below for guidance on when to choose it.

## Learning Objectives

After completing this tutorial you will be able to:

1. Configure `BrokerOptions` for each supported broker protocol (NATS, Kafka, Pulsar, Postgres, Northguard)
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
    Northguard = 4,     // LinkedIn's Kafka successor — ultra-high-scale log striping
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

## Northguard — When to Use It

Northguard is LinkedIn's next-generation log storage engine, built to replace Apache Kafka at extreme scale. It is accessed through the **Xinfra** virtualised pub/sub layer, which provides a unified client API across both Kafka and Northguard clusters.

### Key Architecture Differences from Kafka

| Concern | Kafka | Northguard |
|---------|-------|------------|
| **Data unit** | Partitions (monolithic) | Segments + ranges (fine-grained log striping) |
| **Metadata** | Single centralised controller | Raft-backed sharded vnodes |
| **Rebalancing** | Stop-the-world partition reassignment | Automatic, incremental range splits/merges |
| **Scaling** | Manual partition count tuning | Self-balancing as load changes |
| **Replication** | Partition-level ISR | Configurable per-segment storage policies |

### Scenarios Where You Need Northguard

1. **Ultra-high-throughput event streaming (> 1 M events/sec per topic)** — Kafka's centralised controller becomes a metadata bottleneck at extreme topic/partition counts. Northguard's sharded metadata layer eliminates this.

2. **Massive multi-tenant platforms (hundreds of thousands of topics)** — Kafka clusters struggle when topic counts exceed ~50k. Northguard's fine-grained segmentation handles 100k+ topics without operational overhead.

3. **Frequent scaling / elastic workloads** — Adding or removing Kafka brokers triggers expensive partition reassignment. Northguard's range-based storage rebalances incrementally and automatically.

4. **Long-retention, high-volume audit and compliance logs** — When storing petabytes of data with strict durability guarantees, Northguard's flexible storage policies provide better control than Kafka's uniform replication factor.

5. **Cross-cluster topic federation and migration** — Via Xinfra, you can migrate topics from Kafka to Northguard (or vice versa) without application code changes — dual writes, epoch-based ordering, and seamless cutover.

6. **Reducing operational toil at scale** — If your team spends significant effort on Kafka cluster management (rebalancing, broker recovery, partition skew remediation), Northguard's self-managing architecture drastically reduces ops burden.

### When to Stick with Kafka

- **Mature ecosystem integration** — Kafka Connect, Kafka Streams, ksqlDB, and the vast ecosystem of tooling is battle-tested. Northguard's ecosystem is nascent.
- **Moderate scale (< 50k topics, < 100 brokers)** — Kafka handles this well and the operational model is well understood.
- **Public/OSS requirement** — Northguard is currently internal to LinkedIn. Use Kafka (or other OSS brokers) for general deployments.

### Configuration

```csharp
// appsettings.json
{
  "Broker": {
    "BrokerType": "Northguard",
    "ConnectionString": "https://northguard.example.com"
  }
}

// Program.cs
builder.Services.AddIngestion(options =>
{
    options.BrokerType = BrokerType.Northguard;
    options.ConnectionString = "https://northguard.example.com";
});
```

> **Note:** Northguard is currently an internal LinkedIn system. This integration is provided as a forward-looking implementation for organisations running inside LinkedIn's infrastructure or preparing for Northguard's future public availability.

---

**Previous: [← Tutorial 04 — Integration Envelope](04-integration-envelope.md)** | **Next: [Tutorial 06 — Messaging Channels →](06-messaging-channels.md)**
