# Tutorial 23 — Request-Reply

Send a request over an async channel and correlate the response by `CorrelationId` with timeout support.

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

## Exercises

### Exercise 1: RequestReplyMessage record properties

```csharp
var correlationId = Guid.NewGuid();
var msg = new RequestReplyMessage<string>(
    "payload", "req-topic", "reply-topic", "TestSvc", "cmd.ping", correlationId);

Assert.That(msg.Payload, Is.EqualTo("payload"));
Assert.That(msg.RequestTopic, Is.EqualTo("req-topic"));
Assert.That(msg.ReplyTopic, Is.EqualTo("reply-topic"));
Assert.That(msg.Source, Is.EqualTo("TestSvc"));
Assert.That(msg.MessageType, Is.EqualTo("cmd.ping"));
Assert.That(msg.CorrelationId, Is.EqualTo(correlationId));
```

### Exercise 2: Correlator publishes request with ReplyTo set

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var consumer = Substitute.For<IMessageBrokerConsumer>();
var options = Options.Create(new RequestReplyOptions { TimeoutMs = 500 });

var correlator = new RequestReplyCorrelator<string, string>(
    producer, consumer, options,
    NullLogger<RequestReplyCorrelator<string, string>>.Instance);

var msg = new RequestReplyMessage<string>(
    "ping", "commands", "replies", "TestSvc", "cmd.ping");

await correlator.SendAndReceiveAsync(msg);

await producer.Received(1).PublishAsync(
    Arg.Is<IntegrationEnvelope<string>>(e => e.ReplyTo == "replies"),
    "commands",
    Arg.Any<CancellationToken>());
```

### Exercise 3: Correlator sets intent to Command

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var consumer = Substitute.For<IMessageBrokerConsumer>();
var options = Options.Create(new RequestReplyOptions { TimeoutMs = 500 });

var correlator = new RequestReplyCorrelator<string, string>(
    producer, consumer, options,
    NullLogger<RequestReplyCorrelator<string, string>>.Instance);

var msg = new RequestReplyMessage<string>(
    "data", "req", "rep", "Svc", "cmd.do");

await correlator.SendAndReceiveAsync(msg);

await producer.Received(1).PublishAsync(
    Arg.Is<IntegrationEnvelope<string>>(e => e.Intent == MessageIntent.Command),
    "req",
    Arg.Any<CancellationToken>());
```

### Exercise 4: Timeout returns TimedOut result

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var consumer = Substitute.For<IMessageBrokerConsumer>();
var options = Options.Create(new RequestReplyOptions { TimeoutMs = 300 });

var correlator = new RequestReplyCorrelator<string, string>(
    producer, consumer, options,
    NullLogger<RequestReplyCorrelator<string, string>>.Instance);

var msg = new RequestReplyMessage<string>(
    "request-data", "cmd-topic", "reply-topic", "Svc", "cmd.type");

var result = await correlator.SendAndReceiveAsync(msg);

Assert.That(result.TimedOut, Is.True);
Assert.That(result.Reply, Is.Null);
Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
```

### Exercise 5: Empty RequestTopic throws

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var consumer = Substitute.For<IMessageBrokerConsumer>();
var options = Options.Create(new RequestReplyOptions { TimeoutMs = 500 });

var correlator = new RequestReplyCorrelator<string, string>(
    producer, consumer, options,
    NullLogger<RequestReplyCorrelator<string, string>>.Instance);

var msg = new RequestReplyMessage<string>(
    "data", "", "reply-topic", "Svc", "type");

Assert.ThrowsAsync<ArgumentException>(
    () => correlator.SendAndReceiveAsync(msg));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial23/Lab.cs`](../tests/TutorialLabs/Tutorial23/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial23.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial23/Exam.cs`](../tests/TutorialLabs/Tutorial23/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial23.Exam"
```

---

**Previous: [← Tutorial 22 — Scatter-Gather](22-scatter-gather.md)** | **Next: [Tutorial 24 — Retry Framework →](24-retry-framework.md)**
