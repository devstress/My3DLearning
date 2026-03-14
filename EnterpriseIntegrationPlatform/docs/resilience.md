# Resilience Patterns

## Overview

The Enterprise Integration Platform implements multiple resilience patterns to ensure continued operation under adverse conditions. These patterns protect against transient failures, cascading outages, and resource exhaustion across the distributed system.

## Circuit Breaker

Circuit breakers prevent repeated calls to a failing dependency, allowing it time to recover while protecting the caller from resource exhaustion.

### Implementation

Each outbound connector wraps its communication in a circuit breaker:

```
State: CLOSED (normal operation)
  → Failure count exceeds threshold
State: OPEN (calls rejected immediately)
  → After timeout period
State: HALF-OPEN (single probe call allowed)
  → Probe succeeds → CLOSED
  → Probe fails → OPEN
```

### Configuration

| Parameter                | Default   | Description                                      |
|--------------------------|-----------|--------------------------------------------------|
| `FailureThreshold`       | 5         | Consecutive failures before opening the circuit  |
| `OpenDurationSeconds`    | 30        | Time the circuit stays open before probing       |
| `HalfOpenMaxAttempts`    | 1         | Number of probe calls in half-open state         |
| `SamplingDurationSeconds`| 60        | Rolling window for counting failures             |

### Monitored Operations

- HTTP connector outbound calls
- SFTP connector file transfers
- Email connector SMTP delivery
- Cassandra read/write operations
- External API enrichment calls

## Retries with Exponential Backoff

Transient failures (network glitches, temporary unavailability) are automatically retried with increasing delay.

### Retry Strategy

```
Attempt 1: immediate
Attempt 2: wait 1s
Attempt 3: wait 2s
Attempt 4: wait 4s
Attempt 5: wait 8s (capped at maxInterval)
```

### Configuration

- **MaxAttempts:** 5 (configurable per activity and connector)
- **InitialInterval:** 1 second
- **BackoffCoefficient:** 2.0
- **MaxInterval:** 60 seconds
- **Jitter:** ±20% randomization to prevent thundering herd

### Non-Retryable Errors

Certain errors are classified as permanent and bypass retry logic:

- `400 Bad Request` — Invalid input that won't improve on retry
- `401 Unauthorized` / `403 Forbidden` — Authentication/authorization failures
- `ValidationException` — Schema or business rule violations
- `SchemaNotFoundException` — Referenced schema does not exist
- `DeserializationException` — Payload cannot be parsed

## Bulkhead Isolation

Bulkheads isolate failures in one component from affecting others, preventing a single slow dependency from consuming all available resources.

### Implementation Levels

1. **Process isolation** — Each service type (Ingress, Worker, Admin) runs as a separate process/container.
2. **Thread pool isolation** — Within a service, each connector type has its own thread pool with a bounded concurrency limit.
3. **Kafka consumer group isolation** — Different streaming concerns use separate Kafka consumer groups; task delivery uses NATS/Pulsar consumer isolation (per-subject queue groups or Key_Shared subscriptions).
4. **Temporal task queue isolation** — Different workflow types use separate task queues with independent worker pools.
5. **Tenant isolation** — Resource quotas per tenant prevent a noisy neighbor from starving others.

### Concurrency Limits

| Component              | Max Concurrent Operations | Queued Limit |
|------------------------|---------------------------|--------------|
| HTTP Connector         | 100                       | 500          |
| SFTP Connector         | 20                        | 100          |
| Email Connector        | 50                        | 200          |
| File Connector         | 30                        | 150          |
| Cassandra Operations   | 200                       | 1000         |

## Timeouts

Every external call has a configured timeout to prevent indefinite blocking.

### Timeout Hierarchy

```
Workflow Execution Timeout (e.g., 1 hour)
  └── Activity Execution Timeout (e.g., 5 minutes)
        └── Connector Call Timeout (e.g., 30 seconds)
              └── TCP Connection Timeout (e.g., 10 seconds)
```

### Default Timeouts

| Operation                    | Timeout    |
|------------------------------|------------|
| Workflow execution           | 1 hour     |
| Activity execution           | 5 minutes  |
| Activity start-to-close      | 2 minutes  |
| HTTP connector request       | 30 seconds |
| SFTP file transfer           | 5 minutes  |
| Email SMTP send              | 30 seconds |
| Cassandra query              | 5 seconds  |
| Kafka produce                | 10 seconds |
| NATS/Pulsar publish          | 10 seconds |

## Dead Letter Queues

Messages that exhaust all retry attempts are routed to dead letter queues for manual review and reprocessing.

### DLQ Flow

```
Processing fails → Retries exhausted → Route to DLQ topic
                                             │
                                     ┌───────┴────────┐
                                     │ DLQ Message    │
                                     │ • Original msg │
                                     │ • Error detail │
                                     │ • Retry count  │
                                     │ • Timestamp    │
                                     └───────┬────────┘
                                             │
                                     Admin reviews via
                                     Admin API / Dashboard
                                             │
                                     ┌───────┴────────┐
                                     │   Resolution   │
                                     │ • Replay       │
                                     │ • Discard      │
                                     │ • Edit & Retry │
                                     └────────────────┘
```

### DLQ Topic Naming

Each processing topic has a corresponding DLQ topic: `{topic-name}.dlq`

## Saga Compensation

For multi-step integration workflows, saga compensation ensures consistency when a late step fails after earlier steps have completed.

### Compensation Flow

```
Step 1: Validate message        ✓ (no compensation needed)
Step 2: Transform payload       ✓ (no compensation needed)
Step 3: Deliver to System A     ✓ → Compensation: Notify System A of reversal
Step 4: Deliver to System B     ✗ FAILED
         │
         ▼
Execute compensation:
  → Compensate Step 3: Send reversal to System A
  → Route original message to DLQ with compensation record
```

### Compensation Activities

Each deliverable activity can define a compensation activity:

- **HTTP delivery** → Send DELETE or reversal request
- **SFTP delivery** → Delete uploaded file
- **Database write** → Execute reversal query
- **Email delivery** → Send correction/retraction notice (best effort)

## Graceful Degradation

When dependencies are unavailable, the platform degrades gracefully rather than failing completely.

### Degradation Strategies

| Dependency Failure | Degradation Behavior                                              |
|--------------------|-------------------------------------------------------------------|
| Kafka unavailable  | Streaming workloads degrade; ingress returns 503 for streaming; buffered messages retry on recovery. Task delivery via NATS/Pulsar continues. |
| NATS/Pulsar unavailable | Ingress returns 503 for task delivery; Kafka streaming continues       |
| Temporal unavailable| Messages queue in Kafka; processing resumes when Temporal returns |
| Cassandra unavailable| Dedup checks bypassed (log warning); writes queued for retry     |
| Ollama unavailable | AI features disabled; manual development continues unaffected    |
| Single connector down| Other connectors continue; failed deliveries queue for retry    |

### Health Check Integration

Each dependency has a health check that feeds into the service's overall health status:

- **Healthy** — All dependencies responsive
- **Degraded** — Non-critical dependencies unavailable (e.g., Ollama)
- **Unhealthy** — Critical dependencies unavailable (e.g., Kafka, Temporal)

Health status is exposed via `/health` endpoints and consumed by Kubernetes liveness/readiness probes.

## Backpressure

When downstream systems cannot keep up, backpressure mechanisms prevent unbounded resource growth:

- **Kafka consumer pause** — Consumers pause fetching when in-memory buffers exceed thresholds.
- **Temporal rate limiting** — Task queue rate limits prevent worker overload.
- **Ingress rate limiting** — Per-tenant rate limits reject excess requests with 429 status.
- **Connection pooling** — Bounded connection pools for HTTP, SFTP, and database connections.

## Resilience Testing

### Scenarios

1. **Dependency failure** — Stop each infrastructure component and verify graceful degradation.
2. **Slow dependency** — Inject latency and verify timeouts and circuit breakers activate.
3. **Intermittent failure** — Inject random errors and verify retry logic handles them.
4. **Resource exhaustion** — Consume thread pools and verify bulkhead isolation.
5. **Network partition** — Partition network segments and verify recovery behavior.
