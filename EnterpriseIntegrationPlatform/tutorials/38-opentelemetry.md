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
    public static readonly ActivitySource Source = new("EIP.Platform", "1.0.0");

    public static Activity? StartActivity(
        string operationName,
        ActivityKind kind = ActivityKind.Internal)
    {
        return Source.StartActivity(operationName, kind);
    }
}
```

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
    private static readonly Meter Meter = new("EIP.Platform", "1.0.0");

    public static readonly Counter<long> MessagesReceived =
        Meter.CreateCounter<long>("eip.messages.received");

    public static readonly Counter<long> MessagesDelivered =
        Meter.CreateCounter<long>("eip.messages.delivered");

    public static readonly Counter<long> MessagesFailed =
        Meter.CreateCounter<long>("eip.messages.failed");

    public static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>("eip.processing.duration", "ms");
}
```

### DiagnosticsConfig

```csharp
// src/Observability/DiagnosticsConfig.cs
public sealed class DiagnosticsConfig
{
    public required string ServiceName { get; init; }
    public string? OtlpEndpoint { get; init; }
    public double SamplingRate { get; init; } = 1.0;
    public bool EnableConsoleExporter { get; init; } = false;
    public IReadOnlyList<string>? AdditionalSources { get; init; }
}
```

### CorrelationPropagator

```csharp
// src/Observability/CorrelationPropagator.cs
public sealed class CorrelationPropagator
{
    public void Inject(Activity? activity, IntegrationEnvelope<string> envelope)
    {
        if (activity is null) return;
        envelope.Metadata["traceparent"] = activity.Id!;
        envelope.Metadata["tracestate"] = activity.TraceStateString ?? "";
    }

    public ActivityContext? Extract(IntegrationEnvelope<string> envelope)
    {
        if (!envelope.Metadata.TryGetValue("traceparent", out var traceparent))
            return null;
        return ActivityContext.Parse(traceparent, envelope.Metadata.GetValueOrDefault("tracestate"));
    }
}
```

The propagator embeds W3C `traceparent` and `tracestate` headers into envelope metadata. When a message crosses service boundaries (broker → consumer), the trace context is preserved so all spans for a single message form a connected distributed trace.

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
