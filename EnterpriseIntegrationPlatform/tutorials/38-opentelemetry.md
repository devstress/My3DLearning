# Tutorial 38 — OpenTelemetry

Instrument message processing with OpenTelemetry traces, metrics, and spans.

## Key Types

```csharp
// src/Observability/PlatformActivitySource.cs
public static class PlatformActivitySource
{
    public const string TagMessageId = "eip.message.id";
    public const string TagCorrelationId = "eip.message.correlation_id";
    public const string TagCausationId = "eip.message.causation_id";
    public const string TagMessageType = "eip.message.type";
    public const string TagSource = "eip.message.source";
    public const string TagPriority = "eip.message.priority";
    public const string TagStage = "eip.processing.stage";
    public const string TagDeliveryStatus = "eip.delivery.status";

    public static Activity? StartActivity(
        string stageName,
        ActivityKind kind = ActivityKind.Internal)
    {
        return DiagnosticsConfig.ActivitySource.StartActivity(stageName, kind);
    }

    public static Activity? StartActivity<T>(
        string stageName,
        IntegrationEnvelope<T> envelope,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = DiagnosticsConfig.ActivitySource.StartActivity(stageName, kind);
        if (activity is not null)
        {
            TraceEnricher.Enrich(activity, envelope);
        }
        return activity;
    }
}
```

```csharp
// src/Observability/PlatformMeters.cs
public static class PlatformMeters
{
    public static readonly Counter<long> MessagesReceived =
        DiagnosticsConfig.Meter.CreateCounter<long>("eip.messages.received",
            unit: "{message}", description: "Total number of messages received by the platform.");

    public static readonly Counter<long> MessagesProcessed =
        DiagnosticsConfig.Meter.CreateCounter<long>("eip.messages.processed",
            unit: "{message}", description: "Total number of messages processed successfully.");

    public static readonly Counter<long> MessagesFailed =
        DiagnosticsConfig.Meter.CreateCounter<long>("eip.messages.failed",
            unit: "{message}", description: "Total number of messages that failed processing.");

    public static readonly Counter<long> MessagesDeadLettered =
        DiagnosticsConfig.Meter.CreateCounter<long>("eip.messages.dead_lettered",
            unit: "{message}", description: "Total number of messages sent to the dead-letter store.");

    public static readonly Counter<long> MessagesRetried =
        DiagnosticsConfig.Meter.CreateCounter<long>("eip.messages.retried",
            unit: "{message}", description: "Total number of message retry attempts.");

    public static readonly Histogram<double> ProcessingDuration =
        DiagnosticsConfig.Meter.CreateHistogram<double>("eip.messages.processing_duration",
            unit: "ms", description: "Duration of end-to-end message processing in milliseconds.");

    public static readonly UpDownCounter<long> MessagesInFlight =
        DiagnosticsConfig.Meter.CreateUpDownCounter<long>("eip.messages.in_flight",
            unit: "{message}", description: "Number of messages currently in-flight.");

    // Static helper methods for recording with consistent tags:
    public static void RecordReceived(string messageType, string source);
    public static void RecordProcessed(string messageType, double durationMs);
    public static void RecordFailed(string messageType);
    public static void RecordDeadLettered(string messageType);
    public static void RecordRetry(string messageType, int retryCount);
}
```

```csharp
// src/Observability/DiagnosticsConfig.cs
public static class DiagnosticsConfig
{
    public const string ServiceName = "EnterpriseIntegrationPlatform";
    public const string ServiceVersion = "1.0.0";
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);
    public static readonly Meter Meter = new(ServiceName, ServiceVersion);
}
```

```csharp
// src/Observability/CorrelationPropagator.cs
public static class CorrelationPropagator
{
    public static IntegrationEnvelope<T> InjectTraceContext<T>(
        IntegrationEnvelope<T> envelope)
    {
        var activity = Activity.Current;
        if (activity is null) return envelope;

        envelope.Metadata[MessageHeaders.TraceId] = activity.TraceId.ToString();
        envelope.Metadata[MessageHeaders.SpanId]  = activity.SpanId.ToString();
        return envelope;
    }

    public static Activity? ExtractAndStart<T>(
        IntegrationEnvelope<T> envelope,
        string stageName,
        ActivityKind kind = ActivityKind.Consumer)
    {
        // Rebuilds an ActivityContext from envelope metadata and starts
        // a child Activity linked to the upstream trace.
        ...
    }
}
```

## Exercises

### 1. MessageEvent — RecordShape AllPropertiesAccessible

```csharp
var evt = new MessageEvent
{
    EventId = Guid.NewGuid(),
    MessageId = Guid.NewGuid(),
    CorrelationId = Guid.NewGuid(),
    MessageType = "order.placed",
    Source = "OrderSvc",
    Stage = "Ingestion",
    Status = DeliveryStatus.Pending,
    RecordedAt = DateTimeOffset.UtcNow,
    Details = "Received at gateway",
    BusinessKey = "ORD-123",
    TraceId = "abc123",
    SpanId = "def456",
};

Assert.That(evt.EventId, Is.Not.EqualTo(Guid.Empty));
Assert.That(evt.MessageId, Is.Not.EqualTo(Guid.Empty));
Assert.That(evt.CorrelationId, Is.Not.EqualTo(Guid.Empty));
Assert.That(evt.MessageType, Is.EqualTo("order.placed"));
Assert.That(evt.Source, Is.EqualTo("OrderSvc"));
Assert.That(evt.Stage, Is.EqualTo("Ingestion"));
Assert.That(evt.Status, Is.EqualTo(DeliveryStatus.Pending));
Assert.That(evt.Details, Is.EqualTo("Received at gateway"));
Assert.That(evt.BusinessKey, Is.EqualTo("ORD-123"));
Assert.That(evt.TraceId, Is.EqualTo("abc123"));
Assert.That(evt.SpanId, Is.EqualTo("def456"));
```

### 2. InMemoryMessageStateStore — RecordAndRetrieveByCorrelationId

```csharp
var store = new InMemoryMessageStateStore();
var correlationId = Guid.NewGuid();

var evt = new MessageEvent
{
    EventId = Guid.NewGuid(),
    MessageId = Guid.NewGuid(),
    CorrelationId = correlationId,
    MessageType = "order.placed",
    Source = "OrderSvc",
    Stage = "Routing",
    Status = DeliveryStatus.InFlight,
    RecordedAt = DateTimeOffset.UtcNow,
};

await store.RecordAsync(evt);

var results = await store.GetByCorrelationIdAsync(correlationId);

Assert.That(results, Has.Count.EqualTo(1));
Assert.That(results[0].CorrelationId, Is.EqualTo(correlationId));
```

### 3. InMemoryMessageStateStore — RecordAndRetrieveByBusinessKey

```csharp
var store = new InMemoryMessageStateStore();

var evt = new MessageEvent
{
    EventId = Guid.NewGuid(),
    MessageId = Guid.NewGuid(),
    CorrelationId = Guid.NewGuid(),
    MessageType = "invoice.paid",
    Source = "BillingSvc",
    Stage = "Processing",
    Status = DeliveryStatus.Delivered,
    RecordedAt = DateTimeOffset.UtcNow,
    BusinessKey = "INV-2024-001",
};

await store.RecordAsync(evt);

var results = await store.GetByBusinessKeyAsync("INV-2024-001");

Assert.That(results, Has.Count.EqualTo(1));
Assert.That(results[0].BusinessKey, Is.EqualTo("INV-2024-001"));
```

### 4. InspectionResult — RecordShape

```csharp
var result = new InspectionResult
{
    Query = "ORD-123",
    Found = true,
    Summary = "Message delivered successfully",
    Events = new List<MessageEvent>(),
    LatestStage = "Delivery",
    LatestStatus = DeliveryStatus.Delivered,
};

Assert.That(result.Query, Is.EqualTo("ORD-123"));
Assert.That(result.Found, Is.True);
Assert.That(result.Summary, Is.EqualTo("Message delivered successfully"));
Assert.That(result.Events, Is.Not.Null);
Assert.That(result.LatestStage, Is.EqualTo("Delivery"));
Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.Delivered));
```

### 5. MessageStateSnapshot — RecordShape

```csharp
var snapshot = new MessageStateSnapshot
{
    MessageId = Guid.NewGuid(),
    CorrelationId = Guid.NewGuid(),
    CausationId = Guid.NewGuid(),
    MessageType = "order.shipped",
    Source = "ShippingSvc",
    Priority = MessagePriority.High,
    Timestamp = DateTimeOffset.UtcNow,
    CurrentStage = "Delivery",
    DeliveryStatus = DeliveryStatus.Delivered,
    TraceId = "trace-abc",
    SpanId = "span-xyz",
    RetryCount = 0,
};

Assert.That(snapshot.MessageId, Is.Not.EqualTo(Guid.Empty));
Assert.That(snapshot.CorrelationId, Is.Not.EqualTo(Guid.Empty));
Assert.That(snapshot.CausationId, Is.Not.Null);
Assert.That(snapshot.MessageType, Is.EqualTo("order.shipped"));
Assert.That(snapshot.Source, Is.EqualTo("ShippingSvc"));
Assert.That(snapshot.Priority, Is.EqualTo(MessagePriority.High));
Assert.That(snapshot.CurrentStage, Is.EqualTo("Delivery"));
Assert.That(snapshot.DeliveryStatus, Is.EqualTo(DeliveryStatus.Delivered));
Assert.That(snapshot.TraceId, Is.EqualTo("trace-abc"));
Assert.That(snapshot.SpanId, Is.EqualTo("span-xyz"));
Assert.That(snapshot.RetryCount, Is.EqualTo(0));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial38/Lab.cs`](../tests/TutorialLabs/Tutorial38/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial38.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial38/Exam.cs`](../tests/TutorialLabs/Tutorial38/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial38.Exam"
```

---

**Previous: [← Tutorial 37 — File Connector](37-connector-file.md)** | **Next: [Tutorial 39 — Message Lifecycle →](39-message-lifecycle.md)**
