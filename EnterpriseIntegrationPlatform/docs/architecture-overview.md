# Architecture Overview

## Introduction

The Enterprise Integration Platform is an event-driven, workflow-orchestrated integration system built on .NET 10. It processes messages from diverse sources, transforms and routes them through configurable pipelines, and delivers them to target systems with guaranteed reliability.

## High-Level Flow

```
                         Enterprise Integration Platform
┌──────────────────────────────────────────────────────────────────────────┐
│                                                                          │
│  ┌──────────┐    ┌───────────┐    ┌───────────┐    ┌──────────────────┐  │
│  │          │    │           │    │           │    │                  │  │
│  │ Ingress  │───▶│   Kafka   │───▶│ Temporal  │───▶│   Activities     │  │
│  │ Adapters │    │  Topics   │    │ Workflows │    │ (Transform/Route)│  │
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
│  │  (Storage)   │    │  (Observability) │    │   (Code Generation)   │ │
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
│  │ • HTTP Recv │  │ • Routes    │  │ • Temporal │  │ • Code │ │
│  │ • SFTP Poll │  │ • Tenants   │  │   Workers  │  │   Gen  │ │
│  │ • Email Mon │  │ • Config    │  │ • Kafka    │  │ • Docs │ │
│  │ • File Watch│  │ • Monitor   │  │   Consumer │  │   Gen  │ │
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
│   Kafka    │  │  Temporal  │  │ Cassandra  │  │  Ollama  │
│  Cluster   │  │  Server    │  │  Cluster   │  │  Server  │
└────────────┘  └────────────┘  └────────────┘  └──────────┘
```

## Layer Descriptions

### Ingress Layer

The ingress layer is the entry point for all external messages. It supports multiple protocols and normalizes every incoming payload into the platform's canonical `IntegrationEnvelope` format. Each adapter runs independently, enabling protocol-specific scaling. Ingress adapters publish envelopes to Kafka ingestion topics, decoupling reception from processing.

### Kafka Event Backbone

Apache Kafka provides the durable, ordered, and partitioned event backbone. All inter-service communication flows through Kafka topics. This decoupling ensures that producers and consumers can scale independently, fail independently, and be deployed independently. Topic partitioning by tenant ID and message type ensures ordered processing within logical boundaries.

### Temporal Workflow Orchestration

Temporal.io orchestrates the processing lifecycle of every message. When a Kafka consumer picks up a new envelope, it starts a Temporal workflow that coordinates validation, transformation, routing, and delivery as a series of durable activities. Temporal guarantees that workflows run to completion even across process restarts, infrastructure failures, or long-running operations.

### Activity Processing

Activities are discrete, stateless units of work invoked by Temporal workflows. Each activity performs a single responsibility — validate a schema, apply a transformation map, evaluate a routing rule, or enrich data from an external source. Activities are independently testable and deployable.

### Connectors and Integrations

Connectors deliver processed messages to target systems. Each connector type (HTTP, SFTP, Email, File) encapsulates protocol-specific logic including authentication, serialization, delivery confirmation, and error handling. Connectors are designed as plugins, making it straightforward to add new target system support.

### Storage (Cassandra)

Apache Cassandra provides the distributed, highly-available storage layer. It stores message payloads, workflow metadata, deduplication keys, and audit logs. The data model is designed for write-heavy workloads with time-based partitioning to support efficient querying and automatic data aging.

### Observability

OpenTelemetry is embedded in every layer, providing distributed tracing that follows a message from ingress through Kafka, Temporal workflows, activities, and connector delivery. Structured logs are correlated with trace and span IDs. Metrics cover throughput, latency percentiles, error rates, and resource utilization.

### AI Runtime (Ollama)

Ollama runs locally to provide AI-powered code generation. It indexes the platform's source code, documentation, and rules to generate new connectors, workflow definitions, transformation logic, and documentation summaries — accelerating development while keeping all data on-premises.

## Data Flow Summary

1. External system sends a message via HTTP, drops a file on SFTP, or sends an email.
2. The appropriate ingress adapter receives the message and wraps it in an `IntegrationEnvelope`.
3. The envelope is published to a Kafka ingestion topic.
4. A Kafka consumer picks up the envelope and initiates a Temporal workflow.
5. The workflow executes activities: validation → transformation → routing.
6. Based on routing rules, the workflow invokes the appropriate connector(s).
7. Connectors deliver the message to target system(s) and report success or failure.
8. On success, the workflow records completion in Cassandra and emits an audit event.
9. On failure, retry policies are applied; permanent failures route to DLQ topics.
10. OpenTelemetry captures traces, logs, and metrics at every step.
