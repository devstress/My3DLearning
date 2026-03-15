# Architecture Overview

## Introduction

The Enterprise Integration Platform is a workflow-orchestrated integration system with configurable message brokers, built on .NET 10. It processes messages from diverse sources, transforms and routes them through configurable pipelines, and delivers them to target systems with guaranteed reliability.

## High-Level Flow

```
                         Enterprise Integration Platform
┌──────────────────────────────────────────────────────────────────────────┐
│                                                                          │
│  ┌──────────┐    ┌───────────┐    ┌───────────┐    ┌──────────────────┐  │
│  │          │    │           │    │           │    │                  │  │
│  │ Ingress  │───▶│  Broker   │───▶│ Temporal  │───▶│   Activities     │  │
│  │ Adapters │    │  Layer    │    │ Workflows │    │ (Transform/Route)│  │
│  │          │    │           │    │           │    │                  │  │
│  └──────────┘    └───────────┘    └───────────┘    └────────┬─────────┘  │
│       ▲                │                                    │            │
│       │                ▼                                    ▼            │
│  ┌────┴─────┐    ┌───────────┐                     ┌──────────────────┐ │
│  │ External │    │   DLQ     │                     │   Connectors     │ │
│  │ Systems  │    │  Topics   │                     │ (HTTP/SFTP/Email)│ │
│  └──────────┘    └───────────┘                     └────────┬─────────┘ │
│                                                             │           │
│  ┌──────────────────────────────────────────────────────────┘           │
│  │                                                                      │
│  ▼                                                                      │
│  ┌──────────────┐    ┌──────────────────┐    ┌────────────────────────┐ │
│  │  Cassandra   │    │  OpenTelemetry   │    │   Ollama AI Runtime   │ │
│  │  (Storage)   │    │  (Observability) │    │   (RAG Retrieval)     │ │
│  └──────────────┘    └──────────────────┘    └────────────────────────┘ │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        .NET Aspire Host                         │
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌────────────┐  ┌────────┐ │
│  │ Ingress.API │  │ Admin.API   │  │ Worker.Svc │  │ AI.Svc │ │
│  │             │  │             │  │            │  │        │ │
│  │ • HTTP Recv │  │ • Routes    │  │ • Temporal │  │ • RAG  │ │
│  │ • SFTP Poll │  │ • Tenants   │  │   Workers  │  │  Retrv │ │
│  │ • Email Mon │  │ • Config    │  │ • Broker   │  │ • Know │ │
│  │ • File Watch│  │ • Monitor   │  │   Consumer │  │  ledge │ │
│  └──────┬──────┘  └──────┬──────┘  └─────┬──────┘  └───┬────┘ │
│         │                │               │              │      │
│         ▼                ▼               ▼              ▼      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Shared Infrastructure Layer                │   │
│  │  • IntegrationEnvelope (canonical message format)       │   │
│  │  • OpenTelemetry instrumentation                        │   │
│  │  • Configuration & secret management                    │   │
│  │  • Health check endpoints                               │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
         │              │               │              │
         ▼              ▼               ▼              ▼
┌────────────┐  ┌────────────┐  ┌────────────┐  ┌──────────┐
│ Kafka/NATS │  │  Temporal  │  │ Cassandra  │  │  Ollama  │
│  Brokers   │  │  Server    │  │  Cluster   │  │  Server  │
└────────────┘  └────────────┘  └────────────┘  └──────────┘
```

## Layer Descriptions

### Ingress Layer

The ingress layer is the entry point for all external messages. It supports multiple protocols and normalizes every incoming payload into the platform's canonical `IntegrationEnvelope` format. Each adapter runs independently, enabling protocol-specific scaling. Ingress adapters publish envelopes to the configured message broker, decoupling reception from processing.

### Configurable Broker Layer

The platform uses a configurable message broker layer. Apache Kafka handles broadcast event streaming, audit log streaming, and fan-out analytics — workloads where Kafka's partitioned log model excels. Task-oriented message delivery (ingestion, routing, DLQ) uses NATS JetStream (default) or Apache Pulsar with Key_Shared subscriptions (switchable for large-scale production). This separation avoids Kafka's per-partition serialization (head-of-line blocking), ensuring that Recipient A never blocks Recipient B, even at 1 million recipients. All inter-service communication flows through the appropriate broker, ensuring that producers and consumers can scale independently, fail independently, and be deployed independently.

### Temporal Workflow Orchestration

Temporal.io orchestrates the processing lifecycle of every message. When a broker consumer picks up a new envelope, it starts a Temporal workflow that coordinates validation, transformation, routing, and delivery as a series of durable activities. Temporal guarantees that workflows run to completion even across process restarts, infrastructure failures, or long-running operations.

### Activity Processing

Activities are discrete, stateless units of work invoked by Temporal workflows. Each activity performs a single responsibility — validate a schema, apply a transformation map, evaluate a routing rule, or enrich data from an external source. Activities are independently testable and deployable.

### Connectors and Integrations

Connectors deliver processed messages to target systems. Each connector type (HTTP, SFTP, Email, File) encapsulates protocol-specific logic including authentication, serialization, delivery confirmation, and error handling. Connectors are designed as plugins, making it straightforward to add new target system support.

### Storage (Cassandra)

Apache Cassandra provides the distributed, highly-available storage layer. It stores message payloads, workflow metadata, deduplication keys, and audit logs. The data model is designed for write-heavy workloads with time-based partitioning to support efficient querying and automatic data aging.

### Observability

OpenTelemetry is embedded in every layer, providing distributed tracing that follows a message from ingress through the message broker, Temporal workflows, activities, and connector delivery. Structured logs are correlated with trace and span IDs. Metrics cover throughput, latency percentiles, error rates, and resource utilization.

### AI Runtime (Ollama + RagFlow)

Ollama runs locally to power the self-hosted RAG (Retrieval-Augmented Generation) system. It provides embedding models used by RagFlow to index and retrieve content from the platform's source code, documentation, and rules. Developers use their own preferred AI provider (Copilot, Codex, Claude Code) connecting to the RAG API to get relevant platform context, then use that context to generate integrations. All data stays on-premises — no data leaves the infrastructure.

## Data Flow Summary

1. External system sends a message via HTTP, drops a file on SFTP, or sends an email.
2. The appropriate ingress adapter receives the message and wraps it in an `IntegrationEnvelope`.
3. The envelope is published to the configured message broker (NATS/Pulsar for task delivery, Kafka for streaming).
4. A broker consumer picks up the envelope and initiates a Temporal workflow.
5. The workflow executes activities: validation → transformation → routing.
6. Based on routing rules, the workflow invokes the appropriate connector(s).
7. Connectors deliver the message to target system(s) and report success or failure.
8. On success, the workflow records completion in Cassandra and emits an audit event.
9. On failure, retry policies are applied; permanent failures route to DLQ topics.
10. OpenTelemetry captures traces, logs, and metrics at every step.
