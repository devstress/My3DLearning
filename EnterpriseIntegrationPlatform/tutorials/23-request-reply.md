# Tutorial 23 — Request-Reply

## What You'll Learn

- The EIP Request-Reply pattern for synchronous-style messaging over async channels
- How `IRequestReplyCorrelator<TRequest,TResponse>` sends and waits for a reply
- How `IntegrationEnvelope.ReplyTo` and `CorrelationId` link request to response
- `RequestReplyMessage` for describing the request
- `RequestReplyResult` with timeout handling and duration tracking

---

## EIP Pattern: Request-Reply

> *"Send a pair of Request-Reply messages, each on its own channel."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────┐   Request (ReplyTo=reply-topic)   ┌──────────┐
  │ Requester│──────────────────────────────────▶ │ Responder│
  │          │◀──────────────────────────────────  │          │
  └──────────┘   Reply (CorrelationId=X)          └──────────┘
       │                                                │
       │              request-topic                     │
       │              reply-topic                       │
       └───── correlated by CorrelationId ──────────────┘
```

The requester publishes a message with `ReplyTo` set to a reply topic and a unique `CorrelationId`. The responder processes the request and publishes the reply to the `ReplyTo` topic with the same `CorrelationId`. The requester subscribes to the reply topic and matches by `CorrelationId`.

---

## Platform Implementation

### IRequestReplyCorrelator<TRequest, TResponse>

```csharp
// src/Processing.RequestReply/IRequestReplyCorrelator.cs
public interface IRequestReplyCorrelator<TRequest, TResponse>
{
    Task<RequestReplyResult<TResponse>> SendAndReceiveAsync(
        RequestReplyMessage<TRequest> request,
        CancellationToken cancellationToken = default);
}
```

### RequestReplyMessage

```csharp
// src/Processing.RequestReply/RequestReplyMessage.cs
public record RequestReplyMessage<TRequest>(
    TRequest Payload,
    string RequestTopic,
    string ReplyTopic,
    string Source,
    string MessageType,
    Guid? CorrelationId = null);
```

When `CorrelationId` is `null`, the correlator generates a new one.

### RequestReplyResult

```csharp
// src/Processing.RequestReply/RequestReplyResult.cs
public record RequestReplyResult<TResponse>(
    Guid CorrelationId,
    IntegrationEnvelope<TResponse>? Reply,
    bool TimedOut,
    TimeSpan Duration);
```

### RequestReplyOptions

```csharp
// src/Processing.RequestReply/RequestReplyOptions.cs
public sealed class RequestReplyOptions
{
    public int TimeoutMs { get; set; } = 30_000;    // 30 seconds default
    public string ConsumerGroup { get; set; } = "request-reply";
}
```

---

## Scalability Dimension

The requester is **stateful for the duration of the request** — it holds a pending `TaskCompletionSource` keyed by `CorrelationId`. Multiple concurrent request-reply operations are supported within a single instance. For horizontal scaling, each replica subscribes to the reply topic with its own consumer group and filters replies by `CorrelationId`. Replies not matching any pending request are ignored. The timeout ensures resources are released even if the responder never replies.

---

## Atomicity Dimension

The request is published to the request topic and the correlator subscribes to the reply topic **before** sending. This avoids a race condition where the reply arrives before the subscription is active. If the requester crashes after sending the request, the reply may arrive with no listener — the responder should be idempotent. On timeout, `RequestReplyResult.TimedOut = true` and `Reply = null`, allowing the caller to decide whether to retry or escalate.

---

## Exercises

1. A request is sent with `TimeoutMs = 5000`. The responder takes 7 seconds. What does `RequestReplyResult` look like?

2. Two requesters send requests with different `CorrelationId` values to the same request topic. How does each requester get the correct reply?

3. Why does the correlator subscribe to the reply topic **before** publishing the request? What race condition does this prevent?

---

**Previous: [← Tutorial 22 — Scatter-Gather](22-scatter-gather.md)** | **Next: [Tutorial 24 — Retry Framework →](24-retry-framework.md)**
