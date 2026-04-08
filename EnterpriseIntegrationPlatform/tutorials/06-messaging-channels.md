# Tutorial 06 — Messaging Channels

Point-to-Point, Publish-Subscribe, Datatype, Invalid Message, and Messaging Bridge channel patterns.

## Learning Objectives

After completing this tutorial you will be able to:

1. Send messages through a Point-to-Point channel and verify single-consumer delivery
2. Publish events via Publish-Subscribe channels with fan-out to multiple subscriber groups
3. Route messages to type-specific topics using the Datatype Channel pattern
4. Redirect malformed or expired messages to an Invalid Message Channel
5. Register consumer handlers and verify handler invocation on message arrival

## Key Types

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

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how messaging channel patterns work
> through real NATS JetStream via Aspire. Read the code and comments to understand
> each concept before moving to the Exam.

| # | Test | Concept |
|---|------|---------|
| 1 | `PointToPoint_Send_DeliversToQueueChannel` | Point-to-Point send through real NATS |
| 2 | `PointToPoint_Receive_HandlerTriggeredOnSend` | ReceiveAsync registers a consumer handler |
| 3 | `PointToPoint_MultipleSends_AllDelivered` | Multiple messages accumulate in order |
| 4 | `PubSub_Publish_DeliversToChannel` | Publish-Subscribe through real NATS |
| 5 | `PubSub_Subscribe_MultipleSubscribersGetFanOut` | Fan-out to multiple subscribers |
| 6 | `DatatypeChannel_RoutesMessageByType` | Datatype Channel routes by MessageType |
| 7 | `DatatypeChannel_ResolveChannel_ComputesTopicName` | ResolveChannel computes topic name |
| 8 | `InvalidMessageChannel_RouteInvalid_PublishesToInvalidTopic` | Invalid Message Channel routes malformed messages |
| 9 | `InvalidMessageChannel_RouteRawInvalid_CapturesRawData` | RouteRawInvalidAsync handles raw data |

> 💻 [`tests/TutorialLabs/Tutorial06/Lab.cs`](../tests/TutorialLabs/Tutorial06/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial06.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_Bridge_PointToPointRelay` | 🟢 Starter | Bridge — PointToPointRelay |
| 2 | `Intermediate_PubSubFanOut_ThreeSubscribersReceive` | 🟡 Intermediate | PubSubFanOut — ThreeSubscribersReceive |
| 3 | `Advanced_DatatypeChannel_MultipleTypesRoutedCorrectly` | 🔴 Advanced | DatatypeChannel — MultipleTypesRoutedCorrectly |

> 💻 [`tests/TutorialLabs/Tutorial06/Exam.cs`](../tests/TutorialLabs/Tutorial06/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial06.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial06.ExamAnswers"
```
---

**Previous: [← Tutorial 05 — Message Brokers](05-message-brokers.md)** | **Next: [Tutorial 07 — Temporal Workflows →](07-temporal-workflows.md)**
