# Enterprise Integration Platform

A modern, AI-driven enterprise integration platform built on .NET 10, replacing legacy middleware (BizTalk Server and Apache Camel) with a cloud-native, horizontally scalable architecture.

The platform implements the patterns defined in [*Enterprise Integration Patterns: Designing, Building, and Deploying Messaging Solutions*](https://www.enterpriseintegrationpatterns.com/) by **Gregor Hohpe** and **Bobby Woolf** — the definitive reference for messaging-based integration. The full pattern catalog ([Table of Contents](https://www.enterpriseintegrationpatterns.com/patterns/messaging/toc.html)) is used as the design blueprint: every pattern from Messaging Channels, Message Construction, Message Routing, Message Transformation, Messaging Endpoints, and System Management is mapped to a platform component. See [`docs/eip-mapping.md`](docs/eip-mapping.md) for the complete pattern-to-implementation mapping.

The platform goes beyond the original EIP book by adding **interchangeable message brokers** (Kafka, NATS JetStream, Apache Pulsar) and **[Temporal.io](https://temporal.io/)** for durable, stateful workflow orchestration — enabling fan-out at scale, atomic transactions via saga compensation, and zero-message-loss guarantees that surpass what BizTalk or Camel can offer.

---

## Highlights

- **Configurable Message Brokers** — Kafka for event streaming and audit; NATS JetStream (default) or Apache Pulsar Key_Shared for task delivery. Recipient A never blocks Recipient B, even at 1 million recipients.
- **Temporal Workflow Orchestration** — Long-running, stateful workflows with automatic retry, compensation, and saga support via [Temporal.io](https://temporal.io/).
- **AI-Powered Observability (OpenClaw)** — Ask "where is my message?" in natural language. Backed by Grafana Loki for event storage. Self-hosted RAG (RagFlow + Ollama) provides knowledge retrieval for developers.
- **.NET Aspire** — Single-command local orchestration of all services, brokers, and infrastructure containers.
- **OpenTelemetry** — Distributed tracing, Prometheus metrics, and structured logging across every layer.

## Architecture

```
                         Enterprise Integration Platform
┌──────────────────────────────────────────────────────────────────────────┐
│                                                                          │
│  ┌──────────┐    ┌───────────┐    ┌───────────┐    ┌──────────────────┐  │
│  │ Ingress  │───▶│  Broker   │───▶│ Temporal  │───▶│   Activities     │  │
│  │ Adapters │    │  Layer    │    │ Workflows │    │ (Transform/Route)│  │
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

## Tech Stack

| Component | Technology | Version |
|---|---|---|
| Runtime | .NET | 10 |
| Language | C# | 14 |
| Orchestration | .NET Aspire | 13.1.2 |
| Event Streaming | Apache Kafka | Latest |
| Queue Broker | NATS JetStream / Apache Pulsar | Latest |
| Workflow Engine | Temporal.io | Latest |
| Storage | Apache Cassandra | Latest |
| Observability | OpenTelemetry + Grafana Loki | 1.14.0 |
| AI Runtime | Ollama | Latest |
| Testing | NUnit 4.4.0, NSubstitute 5.3.0, Testcontainers | Latest |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for infrastructure containers)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/devstress/My3DLearning.git
cd My3DLearning/EnterpriseIntegrationPlatform

# Restore and build
dotnet restore
dotnet build

# Run the tests
dotnet test

# Launch all services via Aspire
cd src/AppHost
dotnet run
```

The Aspire dashboard opens automatically, giving you a unified view of all services, logs, traces, and metrics.

## Project Structure

```
EnterpriseIntegrationPlatform/
├── src/
│   ├── AppHost/                     # .NET Aspire orchestrator
│   ├── ServiceDefaults/             # Shared OpenTelemetry & health-check config
│   ├── Contracts/                   # Canonical IntegrationEnvelope & shared interfaces
│   ├── Ingestion/                   # Broker abstraction (IMessageBrokerProducer/Consumer)
│   ├── Ingestion.Kafka/             # Kafka provider
│   ├── Ingestion.Nats/              # NATS JetStream provider (default)
│   ├── Ingestion.Pulsar/            # Apache Pulsar Key_Shared provider
│   ├── Workflow.Temporal/           # Temporal workflow worker & pipeline workflow
│   ├── Activities/                  # Stateless workflow activities & service interfaces
│   ├── Processing.Routing/          # Content-based routing (EIP: Content-Based Router)
│   ├── Processing.Translator/       # Message transformation (EIP: Message Translator)
│   ├── Processing.Transform/        # Payload pipeline — JSON↔XML, regex, JSONPath
│   ├── Processing.Splitter/         # EIP: Splitter
│   ├── Processing.Aggregator/       # EIP: Aggregator
│   ├── Processing.ScatterGather/    # EIP: Scatter-Gather
│   ├── Processing.CompetingConsumers/ # EIP: Competing Consumers with autoscaling
│   ├── Processing.DeadLetter/       # EIP: Dead Letter Channel
│   ├── Processing.Retry/            # Retry framework with exponential backoff
│   ├── Processing.Replay/           # Replay failed/DLQ messages
│   ├── Processing.Throttle/         # Token-bucket throttle with per-tenant partitioning
│   ├── Processing.Dispatcher/       # EIP: Message Dispatcher & Service Activator
│   ├── Processing.RequestReply/     # EIP: Request-Reply correlator
│   ├── Processing.Resequencer/      # EIP: Resequencer — reorder out-of-sequence messages
│   ├── RuleEngine/                  # Business rule evaluation (conditions, AND/OR, actions)
│   ├── EventSourcing/               # Event store, snapshots, projection engine
│   ├── Connector.Http/              # HTTP connector (EIP: Channel Adapter)
│   ├── Connector.Sftp/              # SFTP connector (EIP: Channel Adapter)
│   ├── Connector.Email/             # Email connector (EIP: Channel Adapter)
│   ├── Connector.File/              # File connector (EIP: Channel Adapter)
│   ├── Connectors/                  # Unified connector registry & factory
│   ├── Storage.Cassandra/           # Cassandra data access (EIP: Claim Check, Message Store)
│   ├── Configuration/               # Dynamic config store, feature flags
│   ├── Security/                    # Input sanitization, payload guards, encryption
│   ├── Security.Secrets/            # Secret providers (Azure KV, Vault), rotation
│   ├── MultiTenancy/                # Tenant resolution and isolation (EIP: Multi-Tenant)
│   ├── MultiTenancy.Onboarding/     # Self-service tenant provisioning & quotas
│   ├── DisasterRecovery/            # Failover, replication, RPO/RTO, DR drills
│   ├── Performance.Profiling/       # CPU/memory profiling, GC tuning, benchmarks
│   ├── Observability/               # Lifecycle recording, Loki storage, OpenClaw API
│   ├── AI.Ollama/                   # Ollama AI integration
│   ├── AI.RagFlow/                  # RagFlow RAG client
│   ├── AI.RagKnowledge/             # RAG knowledge base parser & query matcher
│   ├── SystemManagement/            # EIP: Control Bus, Message Store, Smart Proxy, Test Message
│   ├── OpenClaw.Web/                # "Where is my message?" web UI & RAG knowledge API
│   ├── Admin.Web/                   # Vue 3 admin dashboard (proxies to Admin.Api)
│   ├── Gateway.Api/                 # API gateway (EIP: Messaging Gateway)
│   ├── Admin.Api/                   # Administration REST API (EIP: Control Bus)
│   └── Demo.Pipeline/               # End-to-end demo pipeline
├── tests/
│   ├── UnitTests/                   # Fast, isolated unit tests (969 tests)
│   ├── ContractTests/               # Contract verification tests (29 tests)
│   ├── WorkflowTests/               # Temporal workflow tests (24 tests)
│   ├── IntegrationTests/            # Testcontainers-based integration tests (17 tests)
│   ├── PlaywrightTests/             # End-to-end browser tests for Admin dashboard & OpenClaw UI (24 tests)
│   └── LoadTests/                   # Performance and load tests (10 tests)
├── docs/                            # Architecture, ADRs, runbooks, and design docs
├── deploy/                          # Helm charts, Kustomize overlays, K8s manifests
└── rules/                           # Development milestones, coding standards, architecture rules
```

## Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Workflow engine | Temporal over Durable Functions | Portable, language-agnostic, mature saga/compensation support |
| Broker strategy | Kafka + NATS/Pulsar | Kafka for streaming; NATS/Pulsar for task delivery without head-of-line blocking |
| Observability store | Grafana Loki | LogQL queries, lightweight, pairs with OpenTelemetry |
| AI provider | Self-hosted RAG (RagFlow + Ollama) | Context retrieval on-premises; developers use their own AI provider (Copilot, Codex, Claude Code) for code generation |

See [`docs/adr/`](docs/adr/) for full Architecture Decision Records.

## Enterprise Integration Patterns Coverage

This platform systematically implements the patterns from the [EIP book](https://www.enterpriseintegrationpatterns.com/patterns/messaging/toc.html) by Gregor Hohpe and Bobby Woolf. The table below shows the mapping from each book chapter to the platform component that implements it.

| EIP Category | Patterns Implemented | Platform Components |
|---|---|---|
| **Messaging Systems** | Message Channel, Message, Pipes and Filters, Message Router, Message Translator, Message Endpoint | Ingestion broker layer, `IntegrationEnvelope`, Temporal activity chains, `Processing.Routing`, `Processing.Translator` / `Processing.Transform` |
| **Messaging Channels** | Point-to-Point, Pub-Sub, Datatype Channel, Dead Letter Channel, Guaranteed Delivery, Channel Adapter, Invalid Message Channel, Messaging Bridge, Message Bus | Kafka topics, NATS subjects, Pulsar subscriptions, `Processing.DeadLetter`, `Connector.Http/Sftp/Email/File` |
| **Message Construction** | Command/Document/Event Message, Request-Reply, Return Address, Correlation Identifier, Message Sequence, Message Expiration, Format Indicator | `IntegrationEnvelope` fields (`CorrelationId`, `CausationId`, `MessageType`, `SchemaVersion`, metadata headers) |
| **Message Routing** | Content-Based Router, Message Filter, Dynamic Router, Recipient List, Splitter, Aggregator, Resequencer, Scatter-Gather, Routing Slip, Process Manager, Composed Message Processor | `Processing.Routing`, `Processing.Splitter`, `Processing.Aggregator`, `Processing.ScatterGather`, `RuleEngine`, Temporal Workflows |
| **Message Transformation** | Envelope Wrapper, Content Enricher, Content Filter, Claim Check, Normalizer, Canonical Data Model | `IntegrationEnvelope`, `Processing.Transform`, `Storage.Cassandra` (claim check) |
| **Messaging Endpoints** | Messaging Gateway, Transactional Client, Polling Consumer, Event-Driven Consumer, Competing Consumers, Selective Consumer, Durable Subscriber, Idempotent Receiver, Service Activator, Message Dispatcher | `Gateway.Api`, Kafka/NATS/Pulsar consumers, `Processing.CompetingConsumers`, `Storage.Cassandra` (dedup) |
| **System Management** | Control Bus, Detour, Wire Tap, Message History, Message Store, Smart Proxy, Test Message, Channel Purger | `Admin.Api`, OpenTelemetry / `Observability`, `Storage.Cassandra` |

For the complete pattern-to-component mapping with implementation details, see [`docs/eip-mapping.md`](docs/eip-mapping.md).

## Documentation

- [Architecture Overview](docs/architecture-overview.md)
- [Detailed Architecture](docs/architecture.md)
- [Developer Setup Guide](docs/developer-setup.md)
- [Domain Model](docs/domain-model.md)
- [EIP Pattern Mapping](docs/eip-mapping.md)
- [Connector Architecture](docs/connectors.md)
- [Observability](docs/observability.md)
- [Security Architecture](docs/security.md)
- [Reliability](docs/reliability.md)
- [Resilience](docs/resilience.md)
- [Scalability Strategy](docs/scalability.md)
- [Kafka Topology](docs/kafka-topology.md)
- [Temporal Workflows](docs/workflows.md)
- [Temporal Configuration](docs/temporal-workflows.md)
- [Cassandra Data Model](docs/cassandra-data-model.md)
- [AI Strategy](docs/ai-strategy.md)
- [AI Code Generation](docs/ai-code-generation.md)
- [Operations Runbook](docs/operations-runbook.md)
- [Migration from BizTalk](docs/migration-from-biztalk.md)
- [System Context (C4)](docs/system-context.md)

## Contributing

Contributions are welcome. Please read the [coding standards](rules/coding-standards.md) and [architecture rules](rules/architecture-rules.md) before submitting a pull request.

## License

This project is available under the terms specified in the repository. See the root of the repository for license details.
