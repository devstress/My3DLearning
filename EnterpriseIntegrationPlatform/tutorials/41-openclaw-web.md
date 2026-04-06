# Tutorial 41 — OpenClaw Web UI

Search and trace messages through the OpenClaw web UI powered by Blazor Server.

## Exercises

### 1. InspectionResult — RecordShape HasExpectedProperties

```csharp
var result = new InspectionResult
{
    Query = "ORD-123",
    Found = true,
    Summary = "Message delivered",
    OllamaAvailable = false,
    Events = new List<MessageEvent>(),
    LatestStage = "Delivery",
    LatestStatus = DeliveryStatus.Delivered,
};

Assert.That(result.Query, Is.EqualTo("ORD-123"));
Assert.That(result.Found, Is.True);
Assert.That(result.Summary, Is.EqualTo("Message delivered"));
Assert.That(result.OllamaAvailable, Is.False);
Assert.That(result.Events, Is.Empty);
Assert.That(result.LatestStage, Is.EqualTo("Delivery"));
Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.Delivered));
```

### 2. MessageStateInspector — WhereIsByCorrelationAsync ReturnsResult

```csharp
var correlationId = Guid.NewGuid();
var events = new List<MessageEvent>
{
    new()
    {
        MessageId = Guid.NewGuid(),
        CorrelationId = correlationId,
        MessageType = "Order",
        Source = "Gateway",
        Stage = "Ingestion",
        Status = DeliveryStatus.Pending,
    },
};

var log = Substitute.For<IObservabilityEventLog>();
log.GetByCorrelationIdAsync(correlationId, Arg.Any<CancellationToken>())
    .Returns(events);

var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
traceAnalyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns("Message is at Ingestion stage");

var inspector = new MessageStateInspector(
    log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

var result = await inspector.WhereIsByCorrelationAsync(correlationId);

Assert.That(result.Found, Is.True);
Assert.That(result.Events, Has.Count.EqualTo(1));
Assert.That(result.LatestStage, Is.EqualTo("Ingestion"));
```

### 3. MessageStateInspector — WhereIsAsync ReturnsResult

```csharp
var correlationId = Guid.NewGuid();
var events = new List<MessageEvent>
{
    new()
    {
        MessageId = Guid.NewGuid(),
        CorrelationId = correlationId,
        MessageType = "Shipment",
        Source = "Warehouse",
        Stage = "Routing",
        Status = DeliveryStatus.InFlight,
        BusinessKey = "SHIP-456",
    },
};

var log = Substitute.For<IObservabilityEventLog>();
log.GetByBusinessKeyAsync("SHIP-456", Arg.Any<CancellationToken>())
    .Returns(events);

var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
traceAnalyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns("Message is being routed");

var inspector = new MessageStateInspector(
    log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

var result = await inspector.WhereIsAsync("SHIP-456");

Assert.That(result.Found, Is.True);
Assert.That(result.Query, Is.EqualTo("SHIP-456"));
Assert.That(result.LatestStage, Is.EqualTo("Routing"));
```

### 4. MessageStateInspector — CreateSnapshot CreatesValidSnapshot

```csharp
var log = Substitute.For<IObservabilityEventLog>();
var traceAnalyzer = Substitute.For<ITraceAnalyzer>();

var inspector = new MessageStateInspector(
    log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

var envelope = IntegrationEnvelope<string>.Create("payload", "TestSvc", "order.created");

var snapshot = inspector.CreateSnapshot(envelope, "Ingestion", DeliveryStatus.Pending);

Assert.That(snapshot.MessageId, Is.EqualTo(envelope.MessageId));
Assert.That(snapshot.CorrelationId, Is.EqualTo(envelope.CorrelationId));
Assert.That(snapshot.CurrentStage, Is.EqualTo("Ingestion"));
Assert.That(snapshot.DeliveryStatus, Is.EqualTo(DeliveryStatus.Pending));
Assert.That(snapshot.Source, Is.EqualTo("TestSvc"));
Assert.That(snapshot.MessageType, Is.EqualTo("order.created"));
```

### 5. Mock — ITraceAnalyzer WhereIsMessageAsync ReturnsAnalysis

```csharp
var analyzer = Substitute.For<ITraceAnalyzer>();
var correlationId = Guid.NewGuid();

analyzer.WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns("Message is in the dead-letter queue after 3 retries");

var analysis = await analyzer.WhereIsMessageAsync(correlationId, "{}");

Assert.That(analysis, Does.Contain("dead-letter"));
await analyzer.Received(1).WhereIsMessageAsync(
    correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>());
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial41/Lab.cs`](../tests/TutorialLabs/Tutorial41/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial41.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial41/Exam.cs`](../tests/TutorialLabs/Tutorial41/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial41.Exam"
```

---

**Previous: [← Tutorial 40 — RAG with Ollama](40-rag-ollama.md)** | **Next: [Tutorial 42 — Configuration →](42-configuration.md)**
