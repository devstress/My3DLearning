# Enterprise Integration Patterns Mapping

## Overview

This document maps the canonical Enterprise Integration Patterns (EIP), as defined by Gregor Hohpe and Bobby Woolf in *Enterprise Integration Patterns: Designing, Building, and Deploying Messaging Solutions*, to their implementations in the Enterprise Integration Platform.

The platform was designed with these patterns as first-class concerns. Each pattern maps to a specific component, namespace, or infrastructure capability.

## Pattern Mapping Table

| EIP Pattern              | Platform Component                    | Implementation Details                                                                 |
|--------------------------|---------------------------------------|----------------------------------------------------------------------------------------|
| **Message Channel**      | Configured Message Broker             | Each logical channel maps to the configured broker — Kafka topics for streaming, NATS subjects or Pulsar topics for task delivery. Partitioned by tenant and type. |
| **Message**              | `IntegrationEnvelope`                 | The canonical message wrapper carrying payload, metadata, headers, and correlation ID. |
| **Pipes and Filters**    | Temporal Activity Chain               | Workflows compose activities as sequential or parallel filters in a processing pipeline.|
| **Content-Based Router** | `Processing.Routing`                  | Routes messages to different Kafka topics or workflows based on message content/type.  |
| **Recipient List**       | `Processing.Routing`                  | Dynamically resolves a list of target connectors based on routing rules and message metadata.|
| **Message Translator**   | `Processing.Translator`               | Transforms message payloads between formats (JSON↔XML, CSV→JSON, schema mapping).     |
| **Splitter**             | `Processing.Splitter`                 | Splits a batch message into individual messages, each published as a separate envelope.|
| **Aggregator**           | `Processing.Aggregator`               | Collects related messages by correlation ID and produces a combined output message.    |
| **Resequencer**          | `Workflow.Temporal`                   | Temporal workflows reorder out-of-sequence messages using sequence numbers and buffering.|
| **Dead Letter Channel**  | `Processing.DeadLetter`               | Failed messages are routed to DLQ topics/subjects with diagnostic metadata. Admin API provides inspection, replay, and discard. |
| **Retry**                | `Processing.Retry`                    | Configurable retry policies with exponential backoff, max attempts, and non-retryable error classification. |
| **Replay**               | `Processing.Replay`                   | Replay failed or DLQ messages back into the processing pipeline with audit trail. |
| **Idempotent Receiver**  | `Storage.Cassandra` Dedup Table       | Message IDs are checked against a Cassandra deduplication table before processing.     |
| **Correlation Identifier**| `IntegrationEnvelope.CorrelationId`  | Every envelope carries a GUID correlation ID linking related messages across the pipeline.|
| **Process Manager**      | Temporal Workflows                    | Long-running, stateful workflows coordinate multi-step integration processes.          |
| **Claim Check**          | `Storage.Cassandra`                   | Large payloads are stored in Cassandra; envelopes carry a reference key for retrieval. |
| **Wire Tap**             | OpenTelemetry / Observability         | All messages are traced via OpenTelemetry; audit events are emitted to dedicated topics.|
| **Message Filter**       | `Processing.Routing`                  | Filters discard messages that do not match configured predicates before further processing.|
| **Envelope Wrapper**     | `IntegrationEnvelope`                 | Wraps raw payloads with standardized metadata, routing headers, and processing context.|
| **Normalizer**           | Ingress Adapters                      | Each protocol adapter normalizes incoming data into the canonical envelope format.     |
| **Guaranteed Delivery**  | Kafka + Temporal                      | Kafka durability plus Temporal workflow persistence ensures no message is lost.         |
| **Return Address**       | `IntegrationEnvelope.ReplyTo`         | The envelope's ReplyTo field specifies where response messages should be sent.         |
| **Message Expiration**   | Kafka Retention + Envelope TTL        | Messages carry a TTL; Kafka retention policies enforce topic-level expiration.         |
| **Channel Adapter**      | Connector.Http / Connector.Sftp / Connector.Email / Connector.File | Protocol-specific adapters bridge external systems to/from the configured message broker. |
| **Ack/Nack Notification**| Ack/Nack Loopback                     | Every integration implements atomic notification semantics: all-or-nothing. On success, publish an Ack. On failure, publish a Nack. Downstream systems subscribe to Ack/Nack queues. |
| **Saga / Compensation**  | Temporal Saga Workflows               | Multi-step workflows with compensation logic. On failure after partial completion, compensation activities undo prior steps. |
| **Multi-Tenant Isolation**| `MultiTenancy`                       | Tenant resolution and isolation guards ensure data separation across tenants. Per-tenant rate limiting and quotas. |

## Detailed Pattern Implementations

### Content-Based Router

The `Processing.Routing` namespace implements content-based routing through a rules engine:

```
Message arrives → Evaluate routing rules → Select target topic/workflow → Publish
```

Routing rules are defined as JSON configurations specifying field paths, operators, and target destinations. Rules are evaluated in priority order; the first match determines the route. A default route handles unmatched messages.

### Message Translator (Processing.Translator)

Transformations are implemented as Temporal activities. Each transformation activity receives an `IntegrationEnvelope`, applies a mapping definition, and returns a new envelope with the transformed payload. Supported transformation types include:

- **Schema mapping** — Field-to-field mapping with type conversion
- **Format conversion** — JSON to XML, XML to JSON, CSV to JSON
- **Enrichment** — Augment messages with data from external lookups
- **Template-based** — Apply Liquid or Handlebars templates to produce output

### Splitter and Aggregator

The `Processing.Splitter` project breaks a batch envelope into individual envelopes, each published to the configured message broker with a shared `CorrelationId` and unique `SequenceNumber`. The `Processing.Aggregator` project collects envelopes by `CorrelationId`, waits for the expected count (carried in the `TotalCount` header), and produces a combined output envelope.

### Dead Letter Queue (Processing.DeadLetter)

When a message exhausts its retry budget (configured per-activity in Temporal), the workflow publishes it to the corresponding DLQ topic. The `Processing.DeadLetter` project manages DLQ storage, inspection, and administration. DLQ messages include:

- Original envelope (unmodified)
- Error details (exception type, message, stack trace)
- Processing history (which activities succeeded/failed)
- Retry metadata (attempt count, last attempt timestamp)

An admin API provides endpoints to inspect, replay, or discard DLQ messages.

### Process Manager (Temporal Workflows)

Temporal workflows implement the Process Manager pattern for complex integration scenarios:

- **Multi-step orchestration** — Coordinate validation, transformation, routing, and delivery.
- **Saga compensation** — If a downstream delivery fails after partial completion, compensation activities undo prior steps.
- **Human-in-the-loop** — Workflows can pause and wait for manual approval signals before proceeding.
- **Scheduled processing** — Workflows can include timer-based delays for batch windows or rate limiting.

### Claim Check

For messages exceeding a configurable size threshold (default: 256 KB), the ingress adapter stores the full payload in Cassandra and replaces it with a claim check reference in the envelope. Downstream activities retrieve the payload on demand using the claim check key, avoiding large message overhead on the message broker.

### Wire Tap (Observability)

Every processing step emits OpenTelemetry spans and structured log events. The Wire Tap pattern is implemented transparently — no explicit tapping configuration is needed. All message processing is observable by default through:

- Distributed traces linking ingress to delivery
- Audit events published to dedicated Kafka audit topics (Kafka is used specifically for audit streaming)
- Metrics exported to Prometheus-compatible backends

### Retry Framework (Processing.Retry)

The `Processing.Retry` project implements configurable retry policies for transient failures. Each retry policy specifies:

- **Max attempts** — Maximum number of retry attempts before routing to DLQ.
- **Initial interval** — Delay before the first retry.
- **Backoff coefficient** — Multiplier applied to each subsequent retry interval.
- **Max interval** — Upper bound on retry delay.
- **Non-retryable errors** — Error types that should immediately route to DLQ (e.g., authentication failures, schema validation errors).

Retry state is tracked per envelope, ensuring that retries survive process restarts via Temporal's durable execution model.

### Replay Framework (Processing.Replay)

The `Processing.Replay` project enables operators to replay failed or DLQ messages back into the processing pipeline. Replay operations:

- Retrieve the original envelope from DLQ storage.
- Optionally apply corrections (header updates, routing overrides).
- Republish to the configured message broker with a new `ReplayId` header for audit trail.
- Track replay status and outcome.

### Ack/Nack Notification Loopback

Every integration implements atomic notification semantics — all-or-nothing delivery with feedback:

- **Ack (Acknowledgement)** — Published when an integration completes successfully. Downstream systems subscribe to Ack topics to confirm delivery.
- **Nack (Negative Acknowledgement)** — Published on any failure. Downstream systems subscribe to Nack topics to trigger rollback, send error notifications back to the sender, or initiate compensation workflows.

This pattern ensures that external systems always know the outcome of every submitted message, enabling closed-loop integration patterns.

### Saga Compensation

Multi-step integrations use the Saga pattern for distributed transactions. When a downstream delivery fails after partial completion:

1. The Temporal workflow detects the failure.
2. Compensation activities are executed in reverse order to undo prior steps.
3. A Nack notification is published with compensation details.
4. The original envelope is preserved in DLQ for audit and potential replay.

### Connectors (Channel Adapters)

Four connector types implement the Channel Adapter pattern as individual projects:

- **Connector.Http** — REST API delivery with OAuth 2.0, Bearer, API Key, and Client Certificate authentication.
- **Connector.Sftp** — SFTP file delivery with SSH key authentication and atomic rename.
- **Connector.Email** — SMTP/SMTPS email delivery with Liquid templates and attachments.
- **Connector.File** — Local/NFS/SMB file writing with atomic write and configurable encoding.

Each connector emits OpenTelemetry spans, implements health checks, and supports configurable retry policies.
