# Tutorial 41 — OpenClaw Web UI

Search and trace messages through the OpenClaw web UI powered by Blazor Server.

## Learning Objectives

After completing this tutorial you will be able to:

1. Record lifecycle events and query them by correlation ID
2. Track multiple stages of a message lifecycle and retrieve the latest
3. Query lifecycle events by business key across messages
4. Query event history by `MessageId`
5. Publish all lifecycle events to a `NatsBrokerEndpoint`

---

## Key Types

```csharp
// src/Observability/MessageEvent.cs — lifecycle event record
public sealed record MessageEvent
{
    public Guid EventId { get; init; }
    public Guid MessageId { get; init; }
    public string Stage { get; init; }
    public string? BusinessKey { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

// src/Observability/IObservabilityEventLog.cs — lifecycle event storage
public interface IObservabilityEventLog
{
    Task RecordAsync(MessageEvent evt, CancellationToken ct = default);
    Task<IReadOnlyList<MessageEvent>> QueryByCorrelationAsync(Guid correlationId, CancellationToken ct = default);
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `RecordEvent_QueryByCorrelation_PublishToNatsBrokerEndpoint` | Record event and query by correlation |
| 2 | `RecordMultipleStages_TrackLifecycle_PublishLatest` | Track multiple stages and publish latest |
| 3 | `QueryByBusinessKey_PublishMatchingEvents` | Query by business key |
| 4 | `QueryByMessageId_PublishEventHistory` | Query event history by MessageId |
| 5 | `GetLatestByCorrelation_NoneRecorded_ReturnsNull` | No events recorded returns null |
| 6 | `PublishAllLifecycleEventsToNatsBrokerEndpoint` | Publish all lifecycle events to broker |

> 💻 [`tests/TutorialLabs/Tutorial41/Lab.cs`](../tests/TutorialLabs/Tutorial41/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial41.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullMessageLifecycle_RecordAllStages_QueryAndPublish` | 🟢 Starter | Full message lifecycle — record all stages, query, publish |
| 2 | `Challenge2_MultipleMessagesSharedBusinessKey_QueryAndPublish` | 🟡 Intermediate | Multiple messages with shared business key |
| 3 | `Challenge3_MessageStateSnapshot_CreateAndPublish` | 🔴 Advanced | Message state snapshot creation and publish |

> 💻 [`tests/TutorialLabs/Tutorial41/Exam.cs`](../tests/TutorialLabs/Tutorial41/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial41.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial41.ExamAnswers"
```

---

**Previous: [← Tutorial 40 — RAG with Ollama](40-rag-ollama.md)** | **Next: [Tutorial 42 — Configuration →](42-configuration.md)**
