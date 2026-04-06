# Tutorial 39 — Message Lifecycle

Track messages through their complete lifecycle from ingestion to delivery.

## Key Types

```csharp
// src/Observability/MessageEvent.cs
public sealed record MessageEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public required Guid MessageId { get; init; }
    public required Guid CorrelationId { get; init; }
    public required string MessageType { get; init; }
    public required string Source { get; init; }
    public required string Stage { get; init; }
    public required DeliveryStatus Status { get; init; }
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? Details { get; init; }
    public string? BusinessKey { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
```

```csharp
// src/Observability/IMessageStateStore.cs
public interface IMessageStateStore
{
    Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByMessageIdAsync(
        Guid messageId, CancellationToken cancellationToken = default);

    Task<MessageEvent?> GetLatestByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);
}
```

```csharp
// src/Observability/ITraceAnalyzer.cs
public interface ITraceAnalyzer
{
    Task<string> AnalyseTraceAsync(
        string traceContextJson, CancellationToken cancellationToken = default);

    Task<string> WhereIsMessageAsync(
        Guid correlationId, string knownState, CancellationToken cancellationToken = default);
}
```

```csharp
// src/Observability/IObservabilityEventLog.cs
public interface IObservabilityEventLog
{
    Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default);
}
```

## Exercises

### 1. SmartProxy — TrackRequest IncrementsOutstandingCount

```csharp
var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

var envelope = CreateEnvelopeWithReplyTo("request", "Svc", "cmd.query", "reply-queue-1");

var tracked = proxy.TrackRequest(envelope);

Assert.That(tracked, Is.True);
Assert.That(proxy.OutstandingCount, Is.EqualTo(1));
```

### 2. SmartProxy — CorrelateReply ReturnsCorrelation

```csharp
var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

var request = CreateEnvelopeWithReplyTo("request", "Svc", "cmd.query", "reply-queue");
proxy.TrackRequest(request);

// Create a reply with the same CorrelationId
var reply = IntegrationEnvelope<string>.Create(
    "response", "ReplySvc", "cmd.response",
    correlationId: request.CorrelationId);

var correlation = proxy.CorrelateReply(reply);

Assert.That(correlation, Is.Not.Null);
Assert.That(correlation!.CorrelationId, Is.EqualTo(request.CorrelationId));
Assert.That(correlation.OriginalReplyTo, Is.EqualTo("reply-queue"));
Assert.That(correlation.RequestMessageId, Is.EqualTo(request.MessageId));
Assert.That(proxy.OutstandingCount, Is.EqualTo(0));
```

### 3. SmartProxy — CorrelateReply ReturnsNull ForUnknownReply

```csharp
var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

var unknownReply = IntegrationEnvelope<string>.Create("data", "Svc", "unknown.reply");

var correlation = proxy.CorrelateReply(unknownReply);

Assert.That(correlation, Is.Null);
```

### 4. TestMessageGenerator — PublishesToTargetTopic

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();
var generator = new TestMessageGenerator(
    producer, NullLogger<TestMessageGenerator>.Instance);

var result = await generator.GenerateAsync("test-topic", CancellationToken.None);

Assert.That(result.Succeeded, Is.True);
Assert.That(result.TargetTopic, Is.EqualTo("test-topic"));
Assert.That(result.MessageId, Is.Not.EqualTo(Guid.Empty));

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    "test-topic",
    Arg.Any<CancellationToken>());
```

### 5. ControlBusOptions — Shape

```csharp
var opts = new ControlBusOptions();

Assert.That(opts.ControlTopic, Is.EqualTo("eip.control-bus"));
Assert.That(opts.ConsumerGroup, Is.EqualTo("control-bus-consumers"));
Assert.That(opts.Source, Is.EqualTo("ControlBus"));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial39/Lab.cs`](../tests/TutorialLabs/Tutorial39/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial39.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial39/Exam.cs`](../tests/TutorialLabs/Tutorial39/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial39.Exam"
```

---

**Previous: [← Tutorial 38 — OpenTelemetry](38-opentelemetry.md)** | **Next: [Tutorial 40 — RAG with Ollama →](40-rag-ollama.md)**
