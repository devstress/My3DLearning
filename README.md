# My3DLearning

## Enterprise Integration Platform

A modern, AI-driven enterprise integration platform built on **.NET 10**, replacing legacy middleware (BizTalk Server) with a cloud-native, horizontally scalable architecture. The platform implements [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/) using configurable message brokers, durable workflow orchestration, and a self-hosted RAG knowledge system.

### Key Capabilities

- **Configurable Message Brokers** — Kafka for event streaming; NATS JetStream (default) or Apache Pulsar Key_Shared for task delivery.
- **Temporal Workflow Orchestration** — Long-running, stateful workflows with automatic retry, compensation, and saga support.
- **AI-Powered Observability (OpenClaw)** — Natural-language message tracing backed by Grafana Loki, with self-hosted RAG (RagFlow + Ollama) for developer knowledge retrieval.
- **.NET Aspire** — Single-command local orchestration of all services, brokers, and infrastructure containers.
- **OpenTelemetry** — Distributed tracing, Prometheus metrics, and structured logging across every layer.

### Quick Start

```bash
cd EnterpriseIntegrationPlatform
dotnet restore
dotnet build
dotnet test
cd src/AppHost && dotnet run
```

### Documentation

All detailed documentation lives inside the [`EnterpriseIntegrationPlatform/`](EnterpriseIntegrationPlatform/) directory:

- [Platform README](EnterpriseIntegrationPlatform/README.md) — Architecture, tech stack, project structure
- [Architecture Overview](EnterpriseIntegrationPlatform/docs/architecture-overview.md) — High-level flow and component diagrams
- [Developer Setup Guide](EnterpriseIntegrationPlatform/docs/developer-setup.md) — Prerequisites and getting started
- [Milestones](EnterpriseIntegrationPlatform/rules/milestones.md) — Development phases and chunk status
- [Coding Standards](EnterpriseIntegrationPlatform/rules/coding-standards.md) — Code quality rules and conventions

### Technology Stack

| Component | Technology |
|---|---|
| Runtime | .NET 10 / C# 14 |
| Orchestration | .NET Aspire 13.1.2 |
| Event Streaming | Apache Kafka |
| Queue Broker | NATS JetStream / Apache Pulsar |
| Workflow Engine | Temporal.io |
| Storage | Apache Cassandra |
| Observability | OpenTelemetry + Grafana Loki |
| AI Runtime | Ollama (within RagFlow) |
| Testing | NUnit 4.4.0 + NSubstitute 5.3.0 |

### License

This project is available under the terms specified in the repository.