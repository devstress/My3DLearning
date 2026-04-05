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

## Lab

**Objective:** Trace the Scatter-Gather pattern's parallel request-response flow, analyze timeout behavior for **partial results**, and design a "best-of-N" selection strategy.

### Step 1: Trace a Scatter-Gather with Timeout

You scatter a pricing request to 3 suppliers with `TimeoutMs = 5000`:

| Supplier | Response Time | Price |
|----------|--------------|-------|
| A | 1 second | $120 |
| B | 3 seconds | $95 |
| C | Never responds | — |

Open `src/Processing.ScatterGather/ScatterGatherer.cs` and trace:

1. How does `ScatterGatherResult.Responses` look? (2 responses)
2. Is `TimedOut = true`? (yes — only 2 of 3 responded)
3. What is `Duration`? (≈5 seconds — the timeout)

### Step 2: Design a "Best-of-N" Selection Strategy

Using the partial results above, implement a selection strategy that picks the lowest price:

```
1. Scatter to all suppliers (parallel)
2. Gather responses until timeout
3. From gathered responses, select the one with lowest price
4. If no responses arrived, route to DLQ with reason "no-supplier-response"
```

What is the **atomicity** guarantee? The selected best price must be committed as a single decision — if the commit fails, no supplier should be charged.

### Step 3: Compare Scatter-Gather Latency vs. Sequential Calls

| Approach | 3 services × 2s avg | 10 services × 2s avg |
|----------|---------------------|----------------------|
| Sequential | 6 seconds total | 20 seconds total |
| Scatter-Gather | ~2 seconds (parallel) | ~2 seconds (parallel) |

How does the Scatter-Gather pattern enable **scalable** multi-supplier/multi-service integration? What happens to latency as you add more recipients?

## Exam

1. A Scatter-Gather operation sends to 5 recipients with a 3-second timeout. Only 3 respond in time. What does the result indicate?
   - A) Failure — all recipients must respond
   - B) `TimedOut = true` with 3 responses — the caller receives partial results and can decide how to proceed based on business logic (e.g., select best from available)
   - C) The operation retries the 2 missing recipients
   - D) The 3 responses are discarded and the operation fails

2. How does the Scatter-Gather pattern improve **integration scalability** compared to sequential service calls?
   - A) It uses less memory per request
   - B) Latency equals the slowest responder (or timeout), not the sum of all — adding more recipients doesn't increase total latency, enabling efficient multi-source integration at scale
   - C) It reduces the number of network connections
   - D) Sequential calls are always faster for small numbers of recipients

3. What **atomicity** consideration arises when the Scatter-Gather selects one response from many?
   - A) All responses must be stored permanently
   - B) The selected response must be committed atomically — if the downstream commit fails, no side effects from the selection (e.g., supplier charges) should be applied, requiring compensation for any tentative reservations
   - C) Non-selected responses are automatically compensated
   - D) The broker handles selection atomicity

---

**Previous: [← Tutorial 21 — Aggregator](21-aggregator.md)** | **Next: [Tutorial 23 — Request-Reply →](23-request-reply.md)**
