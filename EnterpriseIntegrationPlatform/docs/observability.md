# Observability

## Overview

The Enterprise Integration Platform uses OpenTelemetry as its observability foundation, providing distributed tracing, structured logging, and metrics collection across all services. Observability is a first-class concern — every message is traceable from ingress to delivery without additional configuration.

## Three Pillars

### Distributed Tracing

Every message processed by the platform generates a distributed trace that spans all services, Kafka consumers, Temporal workflows, and outbound connector calls.

**Trace Structure:**

```
Trace: IngressAPI.ReceiveMessage
  └── Span: Kafka.Produce (ingestion topic)
        └── Span: KafkaConsumer.Consume
              └── Span: Temporal.StartWorkflow
                    ├── Span: Activity.Validate
                    ├── Span: Activity.Transform
                    ├── Span: Activity.Route
                    └── Span: Activity.Deliver
                          └── Span: HttpConnector.Send
```

**Trace Context Propagation:**

Trace context (W3C TraceContext format) is propagated across all boundaries:

| Boundary          | Propagation Method                                          |
|--------------------|-------------------------------------------------------------|
| HTTP → Kafka       | `traceparent` header serialized into Kafka message headers  |
| Kafka → Temporal   | Trace context extracted from Kafka headers, injected into Temporal workflow context |
| Temporal → Activity| Temporal SDK propagates trace context automatically         |
| Activity → HTTP    | `traceparent` header included in outbound HTTP requests     |
| Activity → Cassandra| Trace context attached to Cassandra query spans            |

**Key Span Attributes:**

All spans include standardized attributes for filtering and analysis:

- `eip.tenant_id` — Tenant identifier
- `eip.envelope_id` — Unique envelope identifier
- `eip.correlation_id` — Correlation ID for request chain
- `eip.message_type` — Logical message type
- `eip.connector_type` — Connector protocol (HTTP, SFTP, Email, File)
- `eip.workflow_id` — Temporal workflow ID
- `eip.activity_type` — Activity type (Validate, Transform, Route, Deliver)

### Structured Logging

All services emit structured log events in JSON format, correlated with the active trace context.

**Log Format:**

```json
{
  "timestamp": "2025-01-15T10:30:45.123Z",
  "level": "Information",
  "message": "Message transformation completed",
  "traceId": "abc123def456",
  "spanId": "span789",
  "properties": {
    "tenantId": "tenant-acme",
    "envelopeId": "env-001",
    "correlationId": "corr-001",
    "transformId": "transform-order-to-invoice",
    "durationMs": 45
  }
}
```

**Log Levels:**

| Level       | Usage                                                   |
|-------------|---------------------------------------------------------|
| Trace       | Detailed diagnostic information (disabled in production)|
| Debug       | Internal state useful during development                |
| Information | Normal operational events (message received, delivered) |
| Warning     | Unexpected conditions that don't prevent processing     |
| Error       | Failures that affect individual messages                |
| Critical    | System-level failures requiring immediate attention     |

**Log Enrichment:**

The `OpenTelemetry.Instrumentation.AspNetCore` and custom middleware automatically enrich logs with:
- Request/response metadata
- Tenant context from authentication
- Envelope and correlation IDs from message headers

### Metrics Collection

Metrics are collected using OpenTelemetry Metrics API and exported to Prometheus-compatible backends.

**Platform Metrics:**

| Metric Name                          | Type      | Description                               |
|--------------------------------------|-----------|-------------------------------------------|
| `eip.messages.received`             | Counter   | Total messages received by ingress        |
| `eip.messages.processed`            | Counter   | Total messages successfully processed     |
| `eip.messages.failed`               | Counter   | Total messages routed to DLQ              |
| `eip.messages.duration_ms`          | Histogram | End-to-end processing duration            |
| `eip.kafka.consumer_lag`            | Gauge     | Consumer group lag per partition           |
| `eip.kafka.produce_duration_ms`     | Histogram | Kafka produce latency                     |
| `eip.temporal.workflow_active`      | Gauge     | Currently active workflow executions      |
| `eip.temporal.activity_duration_ms` | Histogram | Activity execution duration               |
| `eip.connector.requests`            | Counter   | Outbound connector requests               |
| `eip.connector.duration_ms`         | Histogram | Connector call duration                   |
| `eip.connector.errors`              | Counter   | Connector delivery failures               |
| `eip.cassandra.query_duration_ms`   | Histogram | Cassandra query latency                   |
| `eip.dedup.hits`                    | Counter   | Duplicate messages detected               |

**Metric Labels:**

All metrics include labels for multi-dimensional analysis:
- `tenant_id` — Tenant identifier
- `message_type` — Logical message type
- `connector_type` — Connector protocol
- `status` — Success/failure/retry

## Health Checks

### Endpoint Structure

Each service exposes health check endpoints:

| Endpoint          | Purpose                                           |
|-------------------|---------------------------------------------------|
| `/health/live`    | Liveness probe — is the process running?          |
| `/health/ready`   | Readiness probe — can the service handle requests?|
| `/health/startup` | Startup probe — has initialization completed?     |

### Dependency Health Checks

Readiness probes verify connectivity to all critical dependencies:

- **Kafka** — Producer can connect to at least one broker.
- **Temporal** — Client can describe the configured namespace.
- **Cassandra** — Session can execute a lightweight query (`SELECT now() FROM system.local`).
- **Ollama** — HTTP GET to `/api/tags` returns 200 (non-critical, degraded if unavailable).

### Health Response Format

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "kafka", "status": "Healthy", "duration": "00:00:00.045" },
    { "name": "temporal", "status": "Healthy", "duration": "00:00:00.120" },
    { "name": "cassandra", "status": "Healthy", "duration": "00:00:00.030" },
    { "name": "ollama", "status": "Degraded", "description": "Connection refused" }
  ],
  "totalDuration": "00:00:00.150"
}
```

## Dashboards

### Recommended Dashboard Panels

**Operations Overview:**
- Message throughput (received, processed, failed) over time
- End-to-end latency percentiles (p50, p95, p99)
- Active workflow count
- DLQ depth per topic

**Kafka Health:**
- Consumer group lag by partition
- Produce/consume latency
- Topic throughput
- Broker health status

**Temporal Health:**
- Active workflow count by type
- Activity success/failure rate
- Task queue backlog depth
- Schedule-to-start latency

**Connector Health:**
- Request rate per connector type
- Success/failure ratio
- Response time percentiles
- Circuit breaker state

**Infrastructure:**
- CPU, memory, disk utilization per service
- Cassandra read/write latency
- Network I/O
- Container restart count

## Alerting Rules

| Alert                              | Condition                           | Severity |
|------------------------------------|-------------------------------------|----------|
| High consumer lag                  | Lag > 10,000 for 5 minutes         | Warning  |
| DLQ messages accumulating          | DLQ depth > 100 for 15 minutes     | Warning  |
| Connector circuit breaker open     | Any circuit breaker in OPEN state   | Critical |
| End-to-end latency spike           | p99 > 60 seconds for 5 minutes     | Warning  |
| Cassandra node down                | Node unreachable for 2 minutes     | Critical |
| Temporal workflow failures         | Failure rate > 5% for 10 minutes   | Critical |
| Service unhealthy                  | Health check fails for 3 checks    | Critical |

## OpenTelemetry Configuration

### Collector Setup

The platform exports telemetry to an OpenTelemetry Collector, which routes data to backends:

```
Services → OTLP (gRPC) → OTel Collector → Traces: Jaeger/Tempo
                                         → Metrics: Prometheus
                                         → Logs: Loki/Elasticsearch
```

### Service Configuration

```csharp
// In Aspire AppHost or service startup
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("EIP.Ingress", "EIP.Workflow", "EIP.Connector")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("EIP.Metrics")
        .AddOtlpExporter());
```
