# Tutorial 01 — Introduction to Enterprise Integration

Enterprise integration connects applications through messaging. This platform implements 65+ EIP patterns in .NET 10.

## Learning Objectives

After completing this tutorial you will be able to:

1. Create an `IntegrationEnvelope<T>` with auto-generated identity fields
2. Send messages through a `PointToPointChannel` (queue semantics — one consumer)
3. Fan out messages through a `PublishSubscribeChannel` (every subscriber receives)
4. Wire multi-hop pipelines across channels
5. Build causation chains that track parent→child message lineage

## Key Types

```csharp
// src/Contracts/IntegrationEnvelope.cs — the universal message wrapper
public record IntegrationEnvelope<T>
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string Source { get; init; }
    public string MessageType { get; init; }
    public T Payload { get; init; }
    public MessagePriority Priority { get; init; }
    public string SchemaVersion { get; init; }
    public MessageIntent? Intent { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}

// src/Ingestion/IMessageBrokerProducer.cs
public interface IMessageBrokerProducer
{
    Task PublishAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct = default);
}

// src/Ingestion/IMessageBrokerConsumer.cs
public interface IMessageBrokerConsumer : IAsyncDisposable
{
    Task SubscribeAsync<T>(string topic, Func<IntegrationEnvelope<T>, Task> handler, CancellationToken ct = default);
}

// src/Ingestion/Channels/PointToPointChannel.cs — queue semantics
// src/Ingestion/Channels/PublishSubscribeChannel.cs — fan-out delivery
```

---

## Lab — Guided Practice

> **Purpose:** Run each test, read the code, understand how channels and envelopes work
> with a real NATS JetStream broker.

The lab demonstrates each concept step by step using `NatsBrokerEndpoint` (real NATS
via Aspire). Each test focuses on **one concept** — run them in order.

| # | Test | Concept |
|---|------|---------|
| 1 | `PointToPoint_SendAndReceive_MessageFlowsThroughChannel` | Send a command via P2P, verify delivery |
| 2 | `PubSub_MultipleSubscribers_AllReceiveFanOut` | Fan-out event to two subscribers |
| 3 | `PointToPoint_MultipleMessages_AllDeliveredInSequence` | Batch of 5 messages in sequence |
| 4 | `PointToPoint_DomainObject_FlowsThroughChannel` | Typed record `OrderPayload` through P2P |
| 5 | `ChannelHop_P2PToHandler_ThenPubSubFanOut` | P2P → enrichment handler → PubSub hop |
| 6 | `PubSub_CausationChain_PreservedThroughChannelHops` | Causation chain across channels |

> 💻 [`tests/TutorialLabs/Tutorial01/Lab.cs`](../tests/TutorialLabs/Tutorial01/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial01.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_CommandToEvent_SingleChannelHop` | 🟢 Starter | CommandToEvent — SingleChannelHop |
| 2 | `Intermediate_FanOutPipeline_MultipleDownstreamChannels` | 🟡 Intermediate | FanOutPipeline — MultipleDownstreamChannels |
| 3 | `Advanced_ImmutableEnrichment_OriginalAndEnriched_SeparateChannels` | 🔴 Advanced | ImmutableEnrichment — OriginalAndEnriched SeparateChannels |

> 💻 [`tests/TutorialLabs/Tutorial01/Exam.cs`](../tests/TutorialLabs/Tutorial01/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial01.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial01.ExamAnswers"
```
---

**Next: [Tutorial 02 — Setting Up Your Environment →](02-environment-setup.md)**
