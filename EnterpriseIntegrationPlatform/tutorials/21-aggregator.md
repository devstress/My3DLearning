# Tutorial 21 — Aggregator

Collect related messages by `CorrelationId` and combine them into a single aggregate when the group is complete.

---

## Learning Objectives

1. Understand the Aggregator pattern and how it collects related messages into a single aggregate
2. Use `InMemoryMessageAggregateStore` to group envelopes by `CorrelationId`
3. Apply `CountCompletionStrategy` to detect when a group has reached its expected size
4. Verify that incomplete groups return `IsComplete = false` with no aggregate envelope
5. Confirm that the aggregate envelope merges metadata and preserves the highest priority
6. Validate that different `CorrelationId` values form separate, isolated groups

---

## Key Types

```csharp
// src/Processing.Aggregator/IMessageAggregator.cs
public interface IMessageAggregator<TItem, TAggregate>
{
    Task<AggregateResult<TAggregate>> AggregateAsync(
        IntegrationEnvelope<TItem> envelope,
        CancellationToken cancellationToken = default);
}

// src/Processing.Aggregator/ICompletionStrategy.cs
public interface ICompletionStrategy<T>
{
    bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group);
}

// src/Processing.Aggregator/CountCompletionStrategy.cs
public sealed class CountCompletionStrategy<T> : ICompletionStrategy<T>
{
    public CountCompletionStrategy(int expectedCount) { ... }
    public bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group) =>
        group.Count >= _expectedCount;
}

// src/Processing.Aggregator/IAggregationStrategy.cs
public interface IAggregationStrategy<TItem, TAggregate>
{
    TAggregate Aggregate(IReadOnlyList<TItem> items);
}

// src/Processing.Aggregator/IMessageAggregateStore.cs
public interface IMessageAggregateStore<T>
{
    Task<IReadOnlyList<IntegrationEnvelope<T>>> AddAsync(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
    Task RemoveGroupAsync(Guid correlationId, CancellationToken cancellationToken = default);
}

// src/Processing.Aggregator/AggregateResult.cs
public sealed record AggregateResult<TAggregate>(
    bool IsComplete,
    IntegrationEnvelope<TAggregate>? AggregateEnvelope,
    Guid CorrelationId,
    int ReceivedCount);
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Aggregator concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Aggregate_SingleMessage_GroupNotComplete` | Single message does not complete the group |
| 2 | `Aggregate_ReachesCount_CompletesAndPublishes` | Group completes and publishes when count is reached |
| 3 | `Aggregate_PreservesCorrelationId` | Aggregate envelope preserves the original CorrelationId |
| 4 | `Aggregate_DifferentCorrelationIds_FormSeparateGroups` | Different CorrelationIds form isolated groups |
| 5 | `Aggregate_CountCompletion_ExactThreshold` | Exact threshold triggers completion on the final message |
| 6 | `Aggregate_MergesMetadata_FromAllEnvelopes` | Metadata from all envelopes is merged into the aggregate |
| 7 | `Aggregate_UsesHighestPriority` | Aggregate envelope uses the highest priority from the group |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial21.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_InterleavedGroups_CompleteIndependently` | 🟢 Starter | InterleavedGroups — CompleteIndependently |
| 2 | `Intermediate_MetadataConflict_LaterOverridesEarlier` | 🟡 Intermediate | MetadataConflict — LaterOverridesEarlier |
| 3 | `Advanced_DuplicateMessage_IsIdempotent` | 🔴 Advanced | DuplicateMessage — IsIdempotent |

> 💻 [`tests/TutorialLabs/Tutorial21/Exam.cs`](../tests/TutorialLabs/Tutorial21/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial21.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial21.ExamAnswers"
```
---

**Previous: [← Tutorial 20 — Splitter](20-splitter.md)** | **Next: [Tutorial 22 — Scatter-Gather →](22-scatter-gather.md)**
