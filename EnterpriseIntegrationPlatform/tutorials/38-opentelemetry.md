# Tutorial 38 — OpenTelemetry

Instrument message processing with OpenTelemetry traces, metrics, and spans.

## Learning Objectives

After completing this tutorial you will be able to:

1. Record and retrieve lifecycle events by correlation ID and business key
2. Query the latest event for a correlation and order stages chronologically
3. Retrieve events by `MessageId` for per-message history
4. Inject trace context into envelopes with `CorrelationPropagator`
5. Publish lifecycle events end-to-end through a `MockEndpoint`

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

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `RecordAndRetrieve_ByCorrelationId` | Record and retrieve by CorrelationId |
| 2 | `RecordAndRetrieve_ByBusinessKey` | Record and retrieve by business key |
| 3 | `GetLatestByCorrelationId_ReturnsNewestEvent` | Get latest event for a correlation |
| 4 | `GetByMessageId_ReturnsMatchingEvents` | Get events by MessageId |
| 5 | `CorrelationPropagator_InjectTraceContext_ReturnsEnvelope` | Inject trace context into envelope |
| 6 | `MultipleStages_OrderedByRecordedAt` | Multiple stages ordered chronologically |
| 7 | `E2E_MockEndpoint_RecordEventsAsEnvelopesFlow` | End-to-end lifecycle event recording |

> 💻 [`tests/TutorialLabs/Tutorial38/Lab.cs`](../tests/TutorialLabs/Tutorial38/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial38.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullLifecycleTracking_ThroughMockEndpoint` | 🟢 Starter | Full lifecycle tracking through MockEndpoint |
| 2 | `Challenge2_WhereIsInspection_WithMockedServices` | 🟡 Intermediate | Where-is inspection with mocked services |
| 3 | `Challenge3_CreateSnapshot_FromEnvelope` | 🔴 Advanced | Create snapshot from envelope |

> 💻 [`tests/TutorialLabs/Tutorial38/Exam.cs`](../tests/TutorialLabs/Tutorial38/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial38.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial38.ExamAnswers"
```

---

**Previous: [← Tutorial 37 — File Connector](37-connector-file.md)** | **Next: [Tutorial 39 — Message Lifecycle →](39-message-lifecycle.md)**
