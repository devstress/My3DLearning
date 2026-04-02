# Enterprise Integration Platform

A modern, AI-driven enterprise integration platform built on .NET 10, replacing legacy middleware (BizTalk Server) with a cloud-native, horizontally scalable architecture. It implements [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/) using configurable message brokers, durable workflow orchestration, and a self-hosted RAG knowledge system.

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
│   ├── AppHost/                  # .NET Aspire orchestrator
│   ├── ServiceDefaults/          # Shared OpenTelemetry & health-check config
│   ├── Contracts/                # Canonical IntegrationEnvelope & shared interfaces
│   ├── Ingestion/                # Broker abstraction (IMessageBrokerProducer/Consumer)
│   ├── Ingestion.Kafka/          # Kafka provider
│   ├── Ingestion.Nats/           # NATS JetStream provider (default)
│   ├── Ingestion.Pulsar/         # Apache Pulsar Key_Shared provider
│   ├── Workflow.Temporal/        # Temporal workflow worker
│   ├── Activities/               # Stateless workflow activities
│   ├── Processing.Routing/       # Content-based routing
│   ├── Processing.Translator/    # Message transformation logic
│   ├── Processing.Splitter/      # Message splitter
│   ├── Processing.Aggregator/    # Message aggregator
│   ├── Processing.DeadLetter/    # Dead letter queue management
│   ├── Processing.Retry/         # Retry framework
│   ├── Processing.Replay/        # Replay framework
│   ├── Connector.Http/           # HTTP connector
│   ├── Connector.Sftp/           # SFTP connector
│   ├── Connector.Email/          # Email connector
│   ├── Connector.File/           # File connector
│   ├── Storage.Cassandra/        # Cassandra data access layer
│   ├── Security/                 # Input sanitization, payload guards, encryption
│   ├── MultiTenancy/             # Tenant resolution and isolation
│   ├── AI.Ollama/                # Ollama AI integration
│   ├── AI.RagFlow/               # RagFlow RAG client
│   ├── Observability/            # Lifecycle recording, Loki storage, OpenClaw API
│   ├── OpenClaw.Web/             # "Where is my message?" web UI & RAG knowledge API
│   ├── Admin.Api/                # Administration REST API
│   └── Demo.Pipeline/            # End-to-end demo pipeline
├── tests/
│   ├── UnitTests/                # Fast, isolated unit tests (402 tests)
│   ├── ContractTests/            # Contract verification tests (29 tests)
│   ├── WorkflowTests/            # Temporal workflow tests (24 tests)
│   ├── IntegrationTests/         # Testcontainers-based integration tests (17 tests)
│   ├── PlaywrightTests/          # End-to-end browser tests for OpenClaw UI (13 tests)
│   └── LoadTests/                # Performance and load tests (5 tests)
├── docs/                         # Architecture, ADRs, runbooks, and design docs
└── rules/                        # Development milestones, coding standards, architecture rules
```

## Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Workflow engine | Temporal over Durable Functions | Portable, language-agnostic, mature saga/compensation support |
| Broker strategy | Kafka + NATS/Pulsar | Kafka for streaming; NATS/Pulsar for task delivery without head-of-line blocking |
| Observability store | Grafana Loki | LogQL queries, lightweight, pairs with OpenTelemetry |
| AI provider | Self-hosted RAG (RagFlow + Ollama) | Context retrieval on-premises; developers use their own AI provider (Copilot, Codex, Claude Code) for code generation |

See [`docs/adr/`](docs/adr/) for full Architecture Decision Records.

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
