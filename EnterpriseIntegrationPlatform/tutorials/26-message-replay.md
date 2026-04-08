# Tutorial 26 — Message Replay

Replay previously processed messages from the replay store with filtering and deduplication.

---

## Learning Objectives

1. Understand the Message Store / Replay pattern and when to replay messages
2. Use `MessageReplayer` with `InMemoryMessageReplayStore` to replay stored messages to a target topic
3. Verify that replay counts (`ReplayedCount`, `SkippedCount`, `FailedCount`) are accurate
4. Confirm filtering by `MessageType` and `CorrelationId` replays only matching messages
5. Validate the `SkipAlreadyReplayed` option skips messages tagged with a `ReplayId` header
6. Verify configuration guard: an empty `SourceTopic` throws `InvalidOperationException`

---

## Key Types

```csharp
// src/Processing.Replay/IMessageReplayer.cs
public interface IMessageReplayer
{
    Task<ReplayResult> ReplayAsync(ReplayFilter filter, CancellationToken ct);
}
```

```csharp
// src/Processing.Replay/IMessageReplayStore.cs
public interface IMessageReplayStore
{
    Task StoreForReplayAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct);
    IAsyncEnumerable<IntegrationEnvelope<object>> GetMessagesForReplayAsync(string topic, ReplayFilter filter, int maxMessages, CancellationToken ct);
}
```

```csharp
// src/Processing.Replay/ReplayFilter.cs
public record ReplayFilter
{
    public Guid? CorrelationId { get; init; }
    public string? MessageType { get; init; }
    public DateTimeOffset? FromTimestamp { get; init; }
    public DateTimeOffset? ToTimestamp { get; init; }
}
```

```csharp
// src/Processing.Replay/ReplayResult.cs
public record ReplayResult
{
    public required int ReplayedCount { get; init; }
    public required int SkippedCount { get; init; }
    public required int FailedCount { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}
```

## Lab — Guided Practice

> 💻 Run the lab tests to see each Message Replay concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Replay_SingleMessage_PublishesToTargetTopic` | Single message replayed to target topic |
| 2 | `Replay_MultipleMessages_ReplaysAll` | Multiple stored messages are all replayed |
| 3 | `Replay_FilterByMessageType_OnlyMatchingReplayed` | MessageType filter replays only matching messages |
| 4 | `Replay_EmptyStore_ReturnsZeroReplayed` | Empty store returns zero replayed and zero skipped |
| 5 | `Replay_SkipAlreadyReplayed_SkipsTaggedMessages` | SkipAlreadyReplayed skips messages with ReplayId header |
| 6 | `Replay_ResultTimestamps_ArePopulated` | StartedAt and CompletedAt timestamps are populated |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial26.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_FilterByCorrelationId_OnlyMatchingReplayed` | 🟢 Starter | FilterByCorrelationId — OnlyMatchingReplayed |
| 2 | `Intermediate_MaxMessages_CapsReplayCount` | 🟡 Intermediate | MaxMessages — CapsReplayCount |
| 3 | `Advanced_MissingSourceTopic_ThrowsInvalidOperation` | 🔴 Advanced | MissingSourceTopic — ThrowsInvalidOperation |

> 💻 [`tests/TutorialLabs/Tutorial26/Exam.cs`](../tests/TutorialLabs/Tutorial26/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial26.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial26.ExamAnswers"
```
---

**Previous: [← Tutorial 25 — Dead Letter Queue](25-dead-letter-queue.md)** | **Next: [Tutorial 27 — Resequencer →](27-resequencer.md)**
