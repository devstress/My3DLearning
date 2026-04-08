# Tutorial 03 — Your First Message

Create an `IntegrationEnvelope<T>`, understand its anatomy (auto-generated identity, causation chains, priority, metadata, expiration), and deliver messages through Point-to-Point and Publish-Subscribe channels using MockEndpoint for verified end-to-end testing.

## Learning Objectives

After completing this tutorial you will be able to:

1. Create an `IntegrationEnvelope<T>` with auto-generated `MessageId`, `CorrelationId`, and `Timestamp`
2. Build parent→child causation chains using `CausationId` and shared `CorrelationId`
3. Set priority, intent, schema version, and expiration on an envelope
4. Attach metadata key-value pairs and sequence numbers to messages
5. Send messages through a `PointToPointChannel` and verify single-consumer delivery
6. Fan out messages through a `PublishSubscribeChannel` and verify all subscribers receive

## Key Types

```csharp
// src/Contracts/IntegrationEnvelope.cs — canonical message wrapper
public record IntegrationEnvelope<T>
{
    public required Guid MessageId { get; init; }
    public required Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }              // parent→child lineage
    public required DateTimeOffset Timestamp { get; init; }
    public required string Source { get; init; }
    public required string MessageType { get; init; }
    public string SchemaVersion { get; init; } = "1.0";
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public required T Payload { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public DateTimeOffset? ExpiresAt { get; init; }       // Message Expiration
    public int? SequenceNumber { get; init; }             // Splitter position
    public int? TotalCount { get; init; }
    public MessageIntent? Intent { get; init; }           // Command/Document/Event
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;

    public static IntegrationEnvelope<T> Create(
        T payload, string source, string messageType,
        Guid? correlationId = null, Guid? causationId = null);
}

// src/Ingestion/Channels/PointToPointChannel.cs — queue semantics
public sealed class PointToPointChannel : IPointToPointChannel
{
    // Each message delivered to exactly one consumer in the group
    Task SendAsync<T>(IntegrationEnvelope<T> envelope, string channel, CancellationToken ct);
}

// src/Ingestion/Channels/PublishSubscribeChannel.cs — fan-out delivery
public sealed class PublishSubscribeChannel : IPublishSubscribeChannel
{
    // Every subscriber receives every message
    Task PublishAsync<T>(IntegrationEnvelope<T> envelope, string channel, CancellationToken ct);
}
```

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how envelopes are constructed, how
> metadata and lifecycle properties work, and how messages flow through real
> NATS JetStream channels via `NatsBrokerEndpoint`.

| # | Test | Concept |
|---|------|---------|
| 1 | `Envelope_FactoryAutoGeneratesIdentityFields` | Auto-generated MessageId, CorrelationId, Timestamp |
| 2 | `Envelope_DomainObjectPayload_PreservedEndToEnd` | Typed record payload survives publish → receive |
| 3 | `Envelope_CausationId_LinksChildToParent` | Parent→child causation chain |
| 4 | `Envelope_PriorityIntentSchemaVersion_DefaultsAndOverrides` | Defaults and `with` overrides for priority/intent |
| 5 | `Envelope_Metadata_KeyValuePairsFlowWithMessage` | Metadata dictionary through real NATS |
| 6 | `Envelope_ExpiresAt_IsExpiredProperty` | Message expiration pattern |
| 7 | `Envelope_SequenceNumbers_SplitBatchTracking` | SequenceNumber + TotalCount for batch tracking |
| 8 | `PointToPointChannel_SendToQueue_SingleDelivery` | P2P channel — single consumer delivery |
| 9 | `PublishSubscribeChannel_FanOut_AllSubscribersReceive` | PubSub channel — fan-out to all subscribers |
| 10 | `TopicRouting_MessagesDeliveredToCorrectTopics` | Topic-based routing through real NATS |

> 💻 [`tests/TutorialLabs/Tutorial03/Lab.cs`](../tests/TutorialLabs/Tutorial03/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial03.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_CausationChain_ThreeGenerationLineage` | 🟢 Starter | CausationChain — ThreeGenerationLineage |
| 2 | `Intermediate_PointToPointVsPubSub_ChannelSemantics` | 🟡 Intermediate | PointToPointVsPubSub — ChannelSemantics |
| 3 | `Advanced_PriorityExpiration_MessageLifecycle` | 🔴 Advanced | PriorityExpiration — MessageLifecycle |

> 💻 [`tests/TutorialLabs/Tutorial03/Exam.cs`](../tests/TutorialLabs/Tutorial03/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial03.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial03.ExamAnswers"
```
---

**Previous: [← Tutorial 02 — Environment Setup](02-environment-setup.md)** | **Next: [Tutorial 04 — The Integration Envelope →](04-integration-envelope.md)**
