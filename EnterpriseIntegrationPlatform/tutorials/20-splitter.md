# Tutorial 20 — Splitter

Break composite messages into individual items using `IMessageSplitter<T>` with pluggable `ISplitStrategy<T>`.

---

## Learning Objectives

1. Understand the Splitter pattern and how it decomposes composite messages into individual items
2. Use `MessageSplitter<T>` with `FuncSplitStrategy<T>` to split comma-separated payloads
3. Verify that split envelopes preserve `CorrelationId` and set `CausationId` to the source message
4. Confirm that `SequenceNumber` and `TotalCount` metadata are set on every split envelope
5. Validate edge cases: empty split results produce no publishes, missing target topic throws
6. Use `JsonArraySplitStrategy` to split JSON arrays into individual element envelopes

---

## Key Types

```csharp
// src/Processing.Splitter/IMessageSplitter.cs
public interface IMessageSplitter<T>
{
    Task<SplitResult<T>> SplitAsync(
        IntegrationEnvelope<T> source,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Splitter/ISplitStrategy.cs
public interface ISplitStrategy<T>
{
    IReadOnlyList<T> Split(T composite);
}
```

```csharp
// src/Processing.Splitter/SplitResult.cs
public sealed record SplitResult<T>(
    IReadOnlyList<IntegrationEnvelope<T>> SplitEnvelopes,
    Guid SourceMessageId,
    string TargetTopic,
    int ItemCount);
```

```csharp
// src/Processing.Splitter/SplitterOptions.cs
public sealed class SplitterOptions
{
    public string TargetTopic { get; init; }
    public string? ArrayPropertyName { get; init; }
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Splitter concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Split_ProducesCorrectItemCount` | Split a composite string and verify item count and envelope count |
| 2 | `Split_PreservesCorrelationId` | Split envelopes inherit the source CorrelationId |
| 3 | `Split_SetsCausationIdToSourceMessageId` | Each split envelope's CausationId equals the source MessageId |
| 4 | `Split_SequenceNumbers_AreZeroBased` | Split envelopes have zero-based SequenceNumber values |
| 5 | `Split_TotalCount_MatchesItemCount` | Every split envelope's TotalCount matches the total item count |
| 6 | `Split_EmptyResult_ReturnsZeroItems` | Empty strategy result produces zero items and no publishes |
| 7 | `Split_SourceMessageId_CapturedInResult` | SplitResult captures the original source MessageId and topic |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial20.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Splitter pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_TargetMessageTypeOverride_AppliedToAll` | 🟢 Starter |
| 2 | `Intermediate_MetadataPreserved_AcrossSplitEnvelopes` | 🟡 Intermediate |
| 3 | `Advanced_LargeBatch_AllItemsPublished` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial20.Exam"
```

---

**Previous: [← Tutorial 19 — Content Filter](19-content-filter.md)** | **Next: [Tutorial 21 — Aggregator →](21-aggregator.md)**
