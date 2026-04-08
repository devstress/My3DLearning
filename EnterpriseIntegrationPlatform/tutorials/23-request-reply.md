# Tutorial 23 — Request-Reply

Send a request over an async channel and correlate the response by `CorrelationId` with timeout support.

---

## Learning Objectives

1. Understand the Request-Reply pattern and how it correlates async responses by `CorrelationId`
2. Use `RequestReplyCorrelator<TRequest, TResponse>` to send a request and await a correlated reply
3. Verify that the published request envelope sets `ReplyTo` and `Intent = Command`
4. Confirm that a correlated reply is received before the timeout and the result contains the payload
5. Validate timeout behaviour: when no reply arrives, the result has `TimedOut = true` and `Reply = null`
6. Validate input validation: empty `RequestTopic` or `ReplyTopic` throws `ArgumentException`

---

## Key Types

```csharp
// src/Processing.RequestReply/IRequestReplyCorrelator.cs
public interface IRequestReplyCorrelator<TRequest, TResponse>
{
    Task<RequestReplyResult<TResponse>> SendAndReceiveAsync(
        RequestReplyMessage<TRequest> request,
        CancellationToken cancellationToken = default);
}

// src/Processing.RequestReply/RequestReplyMessage.cs
public record RequestReplyMessage<TRequest>(
    TRequest Payload,
    string RequestTopic,
    string ReplyTopic,
    string Source,
    string MessageType,
    Guid? CorrelationId = null);

// src/Processing.RequestReply/RequestReplyResult.cs
public record RequestReplyResult<TResponse>(
    Guid CorrelationId,
    IntegrationEnvelope<TResponse>? Reply,
    bool TimedOut,
    TimeSpan Duration);

// src/Processing.RequestReply/RequestReplyOptions.cs
public sealed class RequestReplyOptions
{
    public int TimeoutMs { get; set; } = 30_000;
    public string ConsumerGroup { get; set; } = "request-reply";
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each Request-Reply concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `SendAndReceive_PublishesRequestToTopic` | Request envelope is published with CorrelationId and ReplyTo set |
| 2 | `SendAndReceive_ReceivesCorrelatedReply` | Correlated reply is received before timeout with correct payload |
| 3 | `SendAndReceive_TimesOut_ReturnsNullReply` | Timeout returns TimedOut = true and Reply = null |
| 4 | `SendAndReceive_DurationIsTracked` | Duration is tracked and greater than zero on successful reply |
| 5 | `SendAndReceive_EmptyRequestTopic_Throws` | Empty RequestTopic throws ArgumentException |
| 6 | `SendAndReceive_EmptyReplyTopic_Throws` | Empty ReplyTopic throws ArgumentException |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial23.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_RequestEnvelope_HasIntentAndReplyTo` | 🟢 Starter | RequestEnvelope — HasIntentAndReplyTo |
| 2 | `Intermediate_ConcurrentRequests_CorrelateCorrectly` | 🟡 Intermediate | ConcurrentRequests — CorrelateCorrectly |
| 3 | `Advanced_Timeout_DurationIsReasonable` | 🔴 Advanced | Timeout — DurationIsReasonable |

> 💻 [`tests/TutorialLabs/Tutorial23/Exam.cs`](../tests/TutorialLabs/Tutorial23/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial23.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial23.ExamAnswers"
```
---

**Previous: [← Tutorial 22 — Scatter-Gather](22-scatter-gather.md)** | **Next: [Tutorial 24 — Retry Framework →](24-retry-framework.md)**
