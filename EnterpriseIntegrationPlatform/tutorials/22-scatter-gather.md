# Tutorial 22 — Scatter-Gather

## What You'll Learn

- The EIP Scatter-Gather pattern for broadcasting a request and collecting responses
- How `IScatterGatherer<TRequest,TResponse>` scatters to a recipient list with a timeout
- `ScatterRequest` with payload, correlation ID, and recipient list
- `GatherResponse` per recipient with success/error status
- `ScatterGatherResult` with collected responses and timeout flag

---

## EIP Pattern: Scatter-Gather

> *"Use a Scatter-Gather that broadcasts a message to multiple recipients and re-aggregates the responses back into a single message."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
                          ┌──────────┐
                     ┌───▶│ Service A│───┐
  ┌──────────────┐   │    └──────────┘   │    ┌──────────────┐
  │   Scatter    │───┤    ┌──────────┐   ├───▶│   Gather     │
  │  (broadcast) │   ├───▶│ Service B│───┤    │  (aggregate) │
  └──────────────┘   │    └──────────┘   │    └──────────────┘
                     │    ┌──────────┐   │
                     └───▶│ Service C│───┘
                          └──────────┘
              ←──── timeout window ────▶
```

Scatter-Gather combines the Recipient List (fan-out) and Aggregator (collection) into a single operation with a timeout window. Responses that arrive after the timeout are discarded.

---

## Platform Implementation

### IScatterGatherer<TRequest, TResponse>

```csharp
// src/Processing.ScatterGather/IScatterGatherer.cs
public interface IScatterGatherer<TRequest, TResponse>
{
    Task<ScatterGatherResult<TResponse>> ScatterGatherAsync(
        ScatterRequest<TRequest> request,
        CancellationToken cancellationToken = default);
}
```

### ScatterRequest

```csharp
// src/Processing.ScatterGather/ScatterRequest.cs
public sealed record ScatterRequest<TRequest>(
    Guid CorrelationId,
    TRequest Payload,
    IReadOnlyList<string> Recipients);
```

### GatherResponse

```csharp
// src/Processing.ScatterGather/GatherResponse.cs
public sealed record GatherResponse<TResponse>(
    string Recipient,
    TResponse Payload,
    DateTimeOffset ReceivedAt,
    bool IsSuccess,
    string? ErrorMessage);
```

### ScatterGatherResult

```csharp
// src/Processing.ScatterGather/ScatterGatherResult.cs
public sealed record ScatterGatherResult<TResponse>(
    Guid CorrelationId,
    IReadOnlyList<GatherResponse<TResponse>> Responses,
    bool TimedOut,
    TimeSpan Duration);
```

`TimedOut = true` when the gather phase ended because `ScatterGatherOptions.TimeoutMs` elapsed before all recipients responded. The `Responses` list contains whatever was collected before the timeout.

---

## Scalability Dimension

The scatter phase is **stateless fan-out** — identical to the Recipient List. The gather phase is **stateful** — it holds a `TaskCompletionSource` per recipient and waits for replies correlated by `CorrelationId`. Each scatter-gather operation is independent, so multiple operations can run concurrently. The timeout prevents unbounded resource holding. For high-volume scatter-gather, partition operations across replicas by `CorrelationId`.

---

## Atomicity Dimension

Scatter-Gather has **best-effort semantics** within the timeout window. If a recipient fails to respond, its `GatherResponse` is absent from the result and `TimedOut = true`. The caller decides how to handle partial results — it may proceed with available responses or retry the entire operation. The `Duration` field provides observability into how long the operation took, enabling SLA monitoring.

---

## Exercises

1. You scatter a pricing request to 3 suppliers with a 5-second timeout. Supplier A responds in 1 s, Supplier B in 3 s, Supplier C never responds. What does `ScatterGatherResult` look like?

2. How would you implement a "best of N" strategy where you take the lowest price from all responses received within the timeout?

3. Compare Scatter-Gather to calling each service sequentially. What is the latency difference with 3 services averaging 2 seconds each?

---

**Previous: [← Tutorial 21 — Aggregator](21-aggregator.md)** | **Next: [Tutorial 23 — Request-Reply →](23-request-reply.md)**
