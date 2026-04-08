# Tutorial 22 — Scatter-Gather

Broadcast a request to multiple recipients in parallel and collect their responses within a timeout window.

---

## Learning Objectives

1. Understand the Scatter-Gather pattern and how it fans out requests to multiple recipients
2. Use `ScatterGatherer<TRequest, TResponse>` to broadcast a `ScatterRequest` and collect `GatherResponse` objects
3. Verify that scatter publishes to every recipient topic and gathers responses by `CorrelationId`
4. Confirm that partial responses are returned when the timeout expires before all recipients reply
5. Validate edge cases: empty recipients return immediately, exceeding `MaxRecipients` throws
6. Submit responses with `SubmitResponseAsync` and observe completion before the timeout window

---

## Key Types

```csharp
// src/Processing.ScatterGather/IScatterGatherer.cs
public interface IScatterGatherer<TRequest, TResponse>
{
    Task<ScatterGatherResult<TResponse>> ScatterGatherAsync(
        ScatterRequest<TRequest> request,
        CancellationToken cancellationToken = default);
}

// src/Processing.ScatterGather/ScatterRequest.cs
public sealed record ScatterRequest<TRequest>(
    Guid CorrelationId,
    TRequest Payload,
    IReadOnlyList<string> Recipients);

// src/Processing.ScatterGather/GatherResponse.cs
public sealed record GatherResponse<TResponse>(
    string Recipient,
    TResponse Payload,
    DateTimeOffset ReceivedAt,
    bool IsSuccess,
    string? ErrorMessage);

// src/Processing.ScatterGather/ScatterGatherResult.cs
public sealed record ScatterGatherResult<TResponse>(
    Guid CorrelationId,
    IReadOnlyList<GatherResponse<TResponse>> Responses,
    bool TimedOut,
    TimeSpan Duration);
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Scatter-Gather concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Scatter_PublishesToAllRecipients` | Scatter publishes to every recipient topic and gathers all responses |
| 2 | `Scatter_EmptyRecipients_ReturnsImmediately` | Empty recipient list returns immediately with no responses |
| 3 | `Gather_TimesOut_ReturnsPartialResponses` | Timeout returns partial responses when not all recipients reply |
| 4 | `Gather_PreservesCorrelationId` | Result preserves the original CorrelationId |
| 5 | `SubmitResponse_UnknownCorrelation_ReturnsFalse` | Submitting a response for an unknown CorrelationId returns false |
| 6 | `Scatter_ExceedsMaxRecipients_Throws` | Exceeding MaxRecipients throws ArgumentException |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial22.Lab"
```

---

## Exam — Assessment Challenges

> 🎯 Prove you can apply the Scatter-Gather pattern in realistic, end-to-end scenarios.
> Each challenge combines multiple concepts and uses a business-like domain.

| # | Challenge | Difficulty |
|---|-----------|------------|
| 1 | `Starter_MixedResponses_SuccessAndFailure` | 🟢 Starter |
| 2 | `Intermediate_Duration_IsTracked` | 🟡 Intermediate |
| 3 | `Advanced_ConcurrentOperations_IsolateByCorrelation` | 🔴 Advanced |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial22.Exam"
```

---

**Previous: [← Tutorial 21 — Aggregator](21-aggregator.md)** | **Next: [Tutorial 23 — Request-Reply →](23-request-reply.md)**
