# Tutorial 39 — Message Lifecycle

Track messages through their complete lifecycle from ingestion to delivery.

## Learning Objectives

After completing this tutorial you will be able to:

1. Track outstanding requests with `SmartProxy` and correlate replies
2. Detect missing `ReplyTo` headers and handle unknown correlation IDs
3. Publish and subscribe to control commands via the `ControlBus`
4. Capture control-bus messages through a `MockEndpoint`
5. Combine Smart Proxy and Control Bus in an end-to-end round-trip

## Key Types

```csharp
// src/Observability/MessageEvent.cs
public sealed record MessageEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public required Guid MessageId { get; init; }
    public required Guid CorrelationId { get; init; }
    public required string MessageType { get; init; }
    public required string Source { get; init; }
    public required string Stage { get; init; }
    public required DeliveryStatus Status { get; init; }
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? Details { get; init; }
    public string? BusinessKey { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
```

```csharp
// src/Observability/IMessageStateStore.cs
public interface IMessageStateStore
{
    Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByMessageIdAsync(
        Guid messageId, CancellationToken cancellationToken = default);

    Task<MessageEvent?> GetLatestByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);
}
```

```csharp
// src/Observability/ITraceAnalyzer.cs
public interface ITraceAnalyzer
{
    Task<string> AnalyseTraceAsync(
        string traceContextJson, CancellationToken cancellationToken = default);

    Task<string> WhereIsMessageAsync(
        Guid correlationId, string knownState, CancellationToken cancellationToken = default);
}
```

```csharp
// src/Observability/IObservabilityEventLog.cs
public interface IObservabilityEventLog
{
    Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `SmartProxy_TrackRequest_IncrementsOutstanding` | Track request increments outstanding count |
| 2 | `SmartProxy_CorrelateReply_ReturnsCorrelation` | Correlate reply returns stored correlation |
| 3 | `SmartProxy_CorrelateReply_ReturnsNull_ForUnknown` | Unknown correlation returns null |
| 4 | `SmartProxy_NoReplyTo_ReturnsFalse` | No ReplyTo header returns false |
| 5 | `ControlBus_PublishCommand_MockEndpoint_CapturesMessage` | Control bus publish captured by MockEndpoint |
| 6 | `ControlBus_Subscribe_MockEndpoint_DeliversCommand` | Control bus subscribe delivers command |
| 7 | `ControlBus_PublishAndSubscribe_E2E_Roundtrip` | Control bus end-to-end round-trip |

> 💻 [`tests/TutorialLabs/Tutorial39/Lab.cs`](../tests/TutorialLabs/Tutorial39/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial39.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullSmartProxyLifecycle` | 🟢 Starter | Full Smart Proxy track → correlate lifecycle |
| 2 | `Challenge2_ControlBus_PublishMultipleCommands_MockEndpoint` | 🟡 Intermediate | Control bus publish multiple commands via MockEndpoint |
| 3 | `Challenge3_SmartProxy_And_ControlBus_CombinedE2E` | 🔴 Advanced | Smart Proxy + Control Bus combined end-to-end |

> 💻 [`tests/TutorialLabs/Tutorial39/Exam.cs`](../tests/TutorialLabs/Tutorial39/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial39.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial39.ExamAnswers"
```

---

**Previous: [← Tutorial 38 — OpenTelemetry](38-opentelemetry.md)** | **Next: [Tutorial 40 — RAG with Ollama →](40-rag-ollama.md)**
