# Tutorial 22 — Scatter-Gather

Broadcast a request to multiple recipients in parallel and collect their responses within a timeout window.

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

## Exercises

### Exercise 1: Empty recipients returns immediately

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 5000 });

var sg = new ScatterGatherer<string, string>(
    producer, options,
    NullLogger<ScatterGatherer<string, string>>.Instance);

var request = new ScatterRequest<string>(
    Guid.NewGuid(), "ping", new List<string>());

var result = await sg.ScatterGatherAsync(request);

Assert.That(result.Responses, Is.Empty);
Assert.That(result.TimedOut, Is.False);
Assert.That(result.Duration, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(1)));
```

### Exercise 2: Exceeding max recipients throws

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var options = Options.Create(new ScatterGatherOptions
{
    MaxRecipients = 2,
    TimeoutMs = 5000,
});

var sg = new ScatterGatherer<string, string>(
    producer, options,
    NullLogger<ScatterGatherer<string, string>>.Instance);

var request = new ScatterRequest<string>(
    Guid.NewGuid(), "payload",
    new List<string> { "t1", "t2", "t3" });

Assert.ThrowsAsync<ArgumentException>(() => sg.ScatterGatherAsync(request));
```

### Exercise 3: Scatter publishes to each recipient topic

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 500 });

var sg = new ScatterGatherer<string, string>(
    producer, options,
    NullLogger<ScatterGatherer<string, string>>.Instance);

var recipients = new List<string> { "svc-a", "svc-b" };
var request = new ScatterRequest<string>(
    Guid.NewGuid(), "hello", recipients);

await sg.ScatterGatherAsync(request);

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    "svc-a",
    Arg.Any<CancellationToken>());

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    "svc-b",
    Arg.Any<CancellationToken>());
```

### Exercise 4: Full scatter-gather completes before timeout

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var options = Options.Create(new ScatterGatherOptions { TimeoutMs = 10_000 });

var sg = new ScatterGatherer<string, string>(
    producer, options,
    NullLogger<ScatterGatherer<string, string>>.Instance);

var correlationId = Guid.NewGuid();
var request = new ScatterRequest<string>(
    correlationId, "query", new List<string> { "svc-a" });

var scatterTask = sg.ScatterGatherAsync(request);

await Task.Delay(100);
var submitted = await sg.SubmitResponseAsync(
    correlationId,
    new GatherResponse<string>("svc-a", "answer", DateTimeOffset.UtcNow, true, null));

var result = await scatterTask;

Assert.That(submitted, Is.True);
Assert.That(result.Responses.Count, Is.EqualTo(1));
Assert.That(result.Responses[0].Payload, Is.EqualTo("answer"));
Assert.That(result.TimedOut, Is.False);
```

### Exercise 5: Default option values

```csharp
var opts = new ScatterGatherOptions();

Assert.That(opts.TimeoutMs, Is.EqualTo(30_000));
Assert.That(opts.MaxRecipients, Is.EqualTo(50));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial22/Lab.cs`](../tests/TutorialLabs/Tutorial22/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial22.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial22/Exam.cs`](../tests/TutorialLabs/Tutorial22/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial22.Exam"
```

---

**Previous: [← Tutorial 21 — Aggregator](21-aggregator.md)** | **Next: [Tutorial 23 — Request-Reply →](23-request-reply.md)**
