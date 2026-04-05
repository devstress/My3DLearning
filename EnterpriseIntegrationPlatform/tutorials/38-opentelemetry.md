# Tutorial 38 — OpenTelemetry

## What You'll Learn

- The EIP Wire Tap pattern applied to distributed tracing
- OpenTelemetry integration for traces, metrics, and logs
- `PlatformActivitySource` for creating spans across pipeline stages
- `PlatformMeters` for throughput, latency, and error counters
- `DiagnosticsConfig`, `CorrelationPropagator`, and `TraceEnricher`
- Distributed traces from ingress to delivery

---

## EIP Pattern: Wire Tap

> *"Insert a simple Recipient List into the channel that publishes each incoming message to both the main channel and a secondary channel — the Wire Tap captures a copy for diagnostics without altering the flow."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Ingress     │────▶│  Pipeline Stage   │────▶│  Delivery    │
  └──────────────┘     └────────┬─────────┘     └──────────────┘
                                │ (Wire Tap)
                                ▼
                       ┌──────────────────┐
                       │  OpenTelemetry   │
                       │  Collector       │
                       │  (traces/metrics/│
                       │   logs)          │
                       └──────────────────┘
```

Every pipeline stage emits telemetry as a side effect. The Wire Tap pattern ensures observability data flows to the collector without affecting message processing.

---

## Platform Implementation

### PlatformActivitySource

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

`PlatformActivitySource` does **not** directly expose an `ActivitySource` property. Instead it delegates to `DiagnosticsConfig.ActivitySource` internally and provides two factory methods. The generic overload automatically enriches the span with envelope metadata via `TraceEnricher`.

Each pipeline stage starts an `Activity` (OpenTelemetry span):
- `eip.ingress.receive` — message received at Gateway API
- `eip.router.evaluate` — routing decision
- `eip.transform.execute` — transformation applied
- `eip.connector.send` — outbound delivery

### PlatformMeters

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

All instruments are created from `DiagnosticsConfig.Meter`. The static helper methods ensure consistent tagging (e.g. `eip.message.type`, `eip.message.source`) and manage the `MessagesInFlight` gauge automatically.

### DiagnosticsConfig

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

### CorrelationPropagator

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

`CorrelationPropagator` is a **static** class. `InjectTraceContext<T>` captures the current `Activity`'s trace and span IDs into envelope metadata and returns the same envelope. `ExtractAndStart<T>` reads those headers back, creates a parent `ActivityContext`, and starts a new `Activity` linked to the upstream trace. When a message crosses service boundaries (broker → consumer), this preserves the distributed trace chain.

### TraceEnricher

The `TraceEnricher` adds business context tags to spans: `eip.message.id`, `eip.correlation.id`, `eip.source`, `eip.message.type`, and `eip.tenant.id`. These tags enable filtering traces by business context in Jaeger, Zipkin, or the Aspire dashboard.

---

## Scalability Dimension

OpenTelemetry adds **minimal overhead** — spans are sampled at the configured rate, and metrics use lightweight counters. The OTLP exporter sends data asynchronously in batches. In high-throughput deployments, reduce `SamplingRate` to control collector load.

---

## Atomicity Dimension

Telemetry is a **best-effort side channel** — if the collector is down, message processing continues. The `CorrelationPropagator` ensures trace context travels with the message, providing end-to-end visibility from ingress to delivery.

---

## Exercises

1. A message flows through ingress → router → transformer → HTTP connector. Draw the expected span hierarchy in a trace viewer.

2. The OTLP collector is unreachable. What happens to message processing? What telemetry is lost?

3. Why does the platform propagate W3C `traceparent` through envelope metadata rather than relying on broker-level header propagation?

---

**Previous: [← Tutorial 37 — File Connector](37-connector-file.md)** | **Next: [Tutorial 39 — Message Lifecycle →](39-message-lifecycle.md)**
