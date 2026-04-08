# Enterprise Integration Platform

A .NET 10 integration platform that implements the complete pattern catalog from [*Enterprise Integration Patterns*](https://www.enterpriseintegrationpatterns.com/) by Gregor Hohpe and Bobby Woolf. The platform provides interchangeable message brokers (NATS JetStream, Kafka, Pulsar, PostgreSQL), durable workflow orchestration via [Temporal.io](https://temporal.io/), and a self-hosted RAG system for developer productivity.

---

## Design Rationale

This section provides an engineering assessment of the integration landscape and explains the design decisions behind this platform. It is intended for architects and technical leads evaluating integration strategies.

### The BizTalk Server End-of-Life Problem

Microsoft BizTalk Server 2020 is the final release. Mainstream support ends in April 2028 and extended support ends in April 2030. After extended support concludes, organizations will receive no further security patches, bug fixes, or vendor support.

Microsoft recommends migrating to Azure Logic Apps as part of Azure Integration Services. However, this migration involves significant re-architecture rather than a direct lift-and-shift:

- **Business Rules Engine (BRE):** Logic Apps has no native equivalent. Business rules must be reimplemented using Azure Functions, custom code, or third-party rule engines.
- **Visual mapping:** Logic Apps uses Liquid templates and code-based transformations. BizTalk's graphical Mapper, while dated, offered a level of visual discoverability for complex transformations that Logic Apps does not replicate.
- **Adapter coverage:** Not all BizTalk adapters have Logic Apps connector equivalents, particularly legacy line-of-business (LOB) and EDI adapters.
- **Stateful orchestrations:** Logic Apps stateful workflows do not fully replicate BizTalk's dehydration/rehydration model for long-running processes.
- **Migration scope:** Industry practitioners, including Microsoft MVPs such as Sandro Pereira, have noted that BizTalk-to-Logic Apps migration effectively requires re-architecting each integration using a combination of Logic Apps, Azure Functions, Service Bus, and Event Grid.

Organizations with large BizTalk estates — often containing hundreds of orchestrations, maps, and pipelines — face a migration window of 2–4 years before security support ends. This platform provides a .NET-native migration target that preserves the BizTalk conceptual model (orchestrations → Temporal workflows, maps → transformation activities, receive/send ports → ingress adapters/connectors) while modernizing the underlying infrastructure. See [`docs/migration-from-biztalk.md`](docs/migration-from-biztalk.md) for the complete BizTalk-to-platform concept mapping.

### Comparison with Apache Camel

Apache Camel is a mature, widely adopted integration framework with over 300 connectors and a comprehensive implementation of EIP routing patterns. It is the standard choice for Java/JVM organizations. This platform differs from Camel in the following areas:

| Concern | Apache Camel | This Platform |
|---|---|---|
| **Workflow orchestration** | Camel routes are stateless. Long-running processes requiring durable state, restart recovery, or saga compensation require a separate workflow engine (e.g., Temporal, Camunda). | Temporal.io is integrated as the workflow engine. Workflows are durable, survive process restarts and infrastructure failures, and support saga compensation natively. |
| **Broker abstraction** | Routes are written against specific broker components (`camel-kafka`, `camel-jms`, etc.). Changing the underlying broker typically requires route modifications. | The broker is a deployment-time configuration choice. Integration code runs unchanged on NATS JetStream, Kafka, Pulsar, or PostgreSQL. |
| **Language ecosystem** | Java/JVM-centric. Camel K and Quarkus provide cloud-native deployment options within the JVM ecosystem. | .NET/C# throughout. This is relevant for organizations already running .NET — particularly former BizTalk environments — where a JVM migration would introduce additional ecosystem complexity. |
| **Operational features** | Administration dashboards, tenant isolation, and business activity monitoring must be built or sourced separately (e.g., Red Hat Fuse, Hawtio). | Includes Admin API, admin dashboard, multi-tenant isolation, OpenTelemetry observability, and message lifecycle tracing. |
| **Security model** | Security is configured at the individual component level. Centralized input sanitization, payload validation, secret rotation, and tenant isolation are left to the implementation team. | Security, secret management (Azure Key Vault, HashiCorp Vault), and multi-tenant isolation are built into the platform as cross-cutting concerns. |

**Where Camel is the stronger choice:** Camel's connector ecosystem (300+ components) is substantially broader than this platform's four connector types (HTTP, SFTP, Email, File). Organizations whose primary requirement is connecting to a wide variety of heterogeneous systems with minimal custom code will find Camel more immediately productive. This platform prioritizes depth of EIP pattern implementation, broker interchangeability, and operational completeness over connector breadth.

### Comparison with Azure Integration Services

Azure Integration Services (Logic Apps, Service Bus, Event Grid, API Management) is Microsoft's recommended cloud integration suite. It offers managed infrastructure, a large connector ecosystem, and serverless scaling. The following comparison addresses scenarios where this platform may be a more appropriate choice:

| Concern | Azure Integration Services | This Platform |
|---|---|---|
| **Infrastructure dependency** | Integration logic is expressed in Logic Apps JSON workflow definitions, deployed on Azure infrastructure, and bound to Azure-specific connectors. Portability to other cloud providers or on-premises requires re-implementation. | Runs on any infrastructure that supports .NET: on-premises, AWS, GCP, Azure, Kubernetes, or a developer workstation. Broker selection is configuration-driven. |
| **Cost model** | Logic Apps charges per action execution; Service Bus charges per message operation. At high message volumes (millions per day), costs scale linearly with throughput. | Infrastructure is self-managed with fixed compute costs. Scaling is driven by cluster capacity rather than per-message pricing. |
| **Observability** | Azure Monitor and Application Insights provide comprehensive telemetry within the Azure ecosystem. Cross-cloud or on-premises observability requires additional tooling. | OpenTelemetry provides vendor-neutral distributed tracing, metrics, and structured logging. Telemetry can be routed to any compatible backend (Grafana, Jaeger, Datadog, Azure Monitor). |
| **Workflow durability** | Logic Apps stateful workflows and Durable Functions provide orchestration capabilities. Complex saga compensation and long-running process patterns (weeks/months) may require careful design. | Temporal.io is purpose-built for durable workflow orchestration with native support for saga compensation, signals, queries, and continue-as-new for unbounded workflows. |
| **Data residency** | Data flows through Azure regions. Compliance with data sovereignty regulations requires careful region selection and may constrain service availability. | All data remains within the organization's own infrastructure. Full control over data residency without cloud provider dependencies. |

**Where Azure Integration Services is the stronger choice:** Azure offers a managed connector ecosystem with 400+ connectors, enterprise support from Microsoft, and minimal operational overhead through serverless execution. Organizations that are committed to the Azure ecosystem and prioritize operational simplicity over infrastructure control will find Azure Integration Services more practical for many scenarios.

### Modernizing the EIP Pattern Catalog

The patterns defined in [*Enterprise Integration Patterns*](https://www.enterpriseintegrationpatterns.com/) (2003) remain foundational to messaging-based integration architecture. The pattern vocabulary — Content-Based Router, Splitter, Aggregator, Dead Letter Channel, and others — is technology-agnostic and continues to be the standard reference for integration design.

However, the book's implementation context predates modern infrastructure:

- Examples reference JMS, TIBCO, and SOAP-era middleware, requiring practitioners to translate concepts to current broker technologies (Kafka, NATS, Pulsar).
- The book does not address patterns that have become essential for distributed systems: durable workflow orchestration, saga compensation, event sourcing, and exactly-once processing guarantees.
- Multi-tenancy, cloud-native deployment, and AI-assisted observability are outside the book's scope.
- No existing open-source .NET project provides a systematic, tested implementation of the complete EIP catalog on modern infrastructure.

This platform addresses that gap. Every pattern from every chapter — Messaging Channels, Message Construction, Message Routing, Message Transformation, Messaging Endpoints, and System Management — is mapped to a platform component, implemented in C#, and covered by automated tests. See [`docs/eip-mapping.md`](docs/eip-mapping.md) for the complete pattern-to-implementation mapping.

### AI Integration Strategy

This platform takes a focused approach to AI, applying it to two specific problem areas rather than positioning AI as a general-purpose integration capability:

1. **Developer context retrieval (RAG):** A self-hosted RagFlow + Ollama deployment indexes the platform's source code, documentation, and architectural rules. Developers query this knowledge base through the OpenClaw API and use the retrieved context with their preferred AI code generation tool (GitHub Copilot, OpenAI Codex, Claude Code, etc.). All data remains on-premises; no source code or documentation is transmitted to external services.

2. **Message lifecycle tracing (OpenClaw):** The OpenClaw web UI accepts natural-language queries such as "where is my message?" and translates them into structured queries against the OpenTelemetry observability layer (backed by Grafana Loki). This is an AI-assisted search interface over structured telemetry data, designed for operations and support teams troubleshooting message flows.

The platform does not position AI as a substitute for integration architecture expertise. AI-generated code follows the same validation pipeline as manually written code: compilation, static analysis, automated tests, and human review.

### Target Use Cases

This platform is designed for organizations that:

1. **Operate on the .NET ecosystem** and require integration middleware that aligns with existing development practices and toolchains.
2. **Require broker flexibility** — the ability to select NATS JetStream, Kafka, Pulsar, or PostgreSQL at deployment time without modifying integration code.
3. **Require durable workflow orchestration** — long-running processes with saga compensation that survive infrastructure failures.
4. **Have data sovereignty requirements** — all data remains within organizational infrastructure, with no external cloud provider dependency.
5. **Are migrating from BizTalk Server** — documented concept mapping from BizTalk artifacts (orchestrations, maps, pipelines, ports) to platform equivalents is provided in [`docs/migration-from-biztalk.md`](docs/migration-from-biztalk.md).

### Scope and Limitations

- **Code-first platform.** All integrations are implemented in C#. This platform does not provide low-code or visual design tooling. Organizations requiring drag-and-drop integration design should evaluate Logic Apps, MuleSoft Anypoint, or Boomi.
- **Limited connector catalog.** The platform includes four connector types: HTTP, SFTP, Email, and File. Organizations requiring broad connectivity to heterogeneous systems (databases, SaaS APIs, mainframes) should evaluate Apache Camel (300+ connectors) or MuleSoft (1,000+ connectors).
- **Production maturity.** The platform is well-architected and covered by 2,000+ automated tests across unit, integration, contract, workflow, browser, and load test suites. However, it has not been validated at scale in high-throughput production environments. BizTalk Server, Apache Camel, and MuleSoft have established production track records measured in billions of messages.
- **Not a universal Camel replacement.** For Java/JVM organizations whose primary requirement is connecting to diverse systems with minimal custom code, Apache Camel remains the more appropriate choice.

---

## Highlights

- **Interchangeable Message Brokers** — NATS JetStream (default), Apache Kafka, Apache Pulsar Key_Shared, or PostgreSQL (SKIP LOCKED). Broker selection is a deployment-time configuration choice. Per-recipient message isolation ensures that slow consumers do not block other recipients.
- **Temporal Workflow Orchestration** — Durable, stateful workflows with automatic retry, saga compensation, and signal handling via [Temporal.io](https://temporal.io/). Workflows survive process crashes and infrastructure failures.
- **Complete EIP Pattern Coverage** — Systematic implementation of every pattern from the Hohpe/Woolf catalog across all six chapters. See [`docs/eip-mapping.md`](docs/eip-mapping.md).
- **Self-Hosted RAG (OpenClaw)** — RagFlow + Ollama index the platform's source code and documentation. Developers retrieve context via API and use their preferred AI provider for code generation. All data remains on-premises.
- **.NET Aspire Orchestration** — Single-command local orchestration of all services, brokers, and infrastructure containers.
- **OpenTelemetry Observability** — Vendor-neutral distributed tracing, Prometheus metrics, and structured logging across every platform layer.

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
| Queue Broker | NATS JetStream / Apache Pulsar / PostgreSQL | Latest |
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
│   ├── Ingestion.Postgres/          # PostgreSQL provider (pg_notify + SKIP LOCKED)
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
│   ├── TutorialLabs/                # 50 tutorials with exercises, labs, and exams (522 tests)
│   ├── BrokerAgnosticTests/         # Cross-broker EIP pattern verification (38 tests)
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
| Broker strategy | Kafka + NATS/Pulsar/PostgreSQL | Kafka for streaming; NATS/Pulsar for task delivery without head-of-line blocking; PostgreSQL for teams that already run Postgres and want to avoid a dedicated broker |
| Observability store | Grafana Loki | LogQL queries, lightweight, pairs with OpenTelemetry |
| AI provider | Self-hosted RAG (RagFlow + Ollama) | Context retrieval on-premises; developers use their own AI provider (Copilot, Codex, Claude Code) for code generation |

See [`docs/adr/`](docs/adr/) for full Architecture Decision Records.

## Enterprise Integration Patterns Coverage

This platform systematically implements the patterns from the [EIP book](https://www.enterpriseintegrationpatterns.com/patterns/messaging/toc.html) by Gregor Hohpe and Bobby Woolf. The table below shows the mapping from each book chapter to the platform component that implements it.

| EIP Category | Patterns Implemented | Platform Components |
|---|---|---|
| **Messaging Systems** | Message Channel, Message, Pipes and Filters, Message Router, Message Translator, Message Endpoint | Ingestion broker layer, `IntegrationEnvelope`, Temporal activity chains, `Processing.Routing`, `Processing.Translator` / `Processing.Transform` |
| **Messaging Channels** | Point-to-Point, Pub-Sub, Datatype Channel, Dead Letter Channel, Guaranteed Delivery, Channel Adapter, Invalid Message Channel, Messaging Bridge, Message Bus | Kafka topics, NATS subjects, Pulsar subscriptions, PostgreSQL tables (pg_notify + SKIP LOCKED), `Processing.DeadLetter`, `Connector.Http/Sftp/Email/File` |
| **Message Construction** | Command/Document/Event Message, Request-Reply, Return Address, Correlation Identifier, Message Sequence, Message Expiration, Format Indicator | `IntegrationEnvelope` fields (`CorrelationId`, `CausationId`, `MessageType`, `SchemaVersion`, metadata headers) |
| **Message Routing** | Content-Based Router, Message Filter, Dynamic Router, Recipient List, Splitter, Aggregator, Resequencer, Scatter-Gather, Routing Slip, Process Manager, Composed Message Processor | `Processing.Routing`, `Processing.Splitter`, `Processing.Aggregator`, `Processing.ScatterGather`, `RuleEngine`, Temporal Workflows |
| **Message Transformation** | Envelope Wrapper, Content Enricher, Content Filter, Claim Check, Normalizer, Canonical Data Model | `IntegrationEnvelope`, `Processing.Transform`, `Storage.Cassandra` (claim check) |
| **Messaging Endpoints** | Messaging Gateway, Transactional Client, Polling Consumer, Event-Driven Consumer, Competing Consumers, Selective Consumer, Durable Subscriber, Idempotent Receiver, Service Activator, Message Dispatcher | `Gateway.Api`, Kafka/NATS/Pulsar/PostgreSQL consumers, `Processing.CompetingConsumers`, `Storage.Cassandra` (dedup) |
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
