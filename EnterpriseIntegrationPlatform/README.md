# Enterprise Integration Platform

A .NET 10 integration platform built on the [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/) book by Gregor Hohpe and Bobby Woolf, with interchangeable message brokers (NATS JetStream, Kafka, Pulsar, PostgreSQL) and [Temporal.io](https://temporal.io/) workflow orchestration.

---

## Why This Platform Exists

This section is written as an honest engineering analysis — not vendor marketing. If you are evaluating whether to use this platform, adopt Apache Camel, migrate to Azure Logic Apps, or build something custom, the information below should help you make an informed decision.

### The Problem: Enterprise Integration Is Broken in Several Ways

Enterprise integration — connecting systems, transforming data, routing messages — is one of the oldest problems in software. The EIP book (2003) gave us a shared vocabulary for solving it. But in 2025, the tooling landscape around EIP is fractured and problematic.

**1. BizTalk Server is dying, and its replacement isn't equivalent.**

Microsoft BizTalk Server 2020 is the final release. Mainstream support ends April 2028; extended support ends April 2030. After that: no patches, no security fixes, no vendor support. Microsoft directs customers to Azure Logic Apps, but this is not a 1:1 replacement:

- Logic Apps has no native Business Rules Engine (BRE). You must reimplement rules in Azure Functions or custom code.
- Logic Apps has no visual BizTalk Mapper equivalent. Transformations use Liquid templates or code — workable, but a step backwards in discoverability for complex maps.
- Not all BizTalk adapters have Logic Apps connectors, particularly legacy LOB and EDI adapters.
- Long-running stateful orchestrations require careful redesign. Logic Apps stateful workflows exist but don't match BizTalk's dehydration/rehydration model in all scenarios.
- Migration is not "lift and shift." Sandro Pereira (Microsoft MVP) calls it "impossible to migrate a BizTalk Server environment to Logic Apps" — what you really do is re-architect from scratch using a combination of Logic Apps, Azure Functions, Service Bus, and Event Grid.

**The real risk:** Organizations running BizTalk have 2–4 years before they lose security patches. Many have hundreds of orchestrations, maps, and pipelines that are poorly documented. The clock is ticking, and the migration path is expensive and architecturally disruptive.

**2. Apache Camel is powerful but solves a different (narrower) problem.**

Camel is an excellent integration framework with 300+ connectors and a mature implementation of EIP routing patterns. But it has structural limitations for the use cases this platform targets:

| Concern | Apache Camel | This Platform |
|---|---|---|
| **Stateful workflow orchestration** | Not built-in. Camel routes are fundamentally stateless. Long-running processes that survive restarts, need compensation (saga), or run for days/weeks require bolting on a separate workflow engine. | Temporal.io provides durable execution out of the box. Workflows survive process crashes, infrastructure failures, and can run for months. Saga compensation is a first-class concept. |
| **Broker lock-in** | Camel connects to many brokers, but routes are typically written against a specific broker's component. Switching from `camel-kafka` to `camel-nats` means rewriting routes. | Broker is a deployment-time configuration choice. The same integration code runs on NATS, Kafka, Pulsar, or PostgreSQL without code changes. |
| **Language ecosystem** | Java/JVM. Excellent if your team is Java-native. Camel K and Quarkus improve cloud-native support but the ecosystem is Java-centric. | .NET/C#. If your organization runs on .NET — and many BizTalk shops do, because BizTalk is a Microsoft product — this is a natural migration path without a full language/ecosystem shift. |
| **Enterprise management** | No built-in admin dashboard, BAM-equivalent, or tenant isolation. You build these yourself or adopt a commercial wrapper (Red Hat Fuse, etc.). | Includes Admin API, admin dashboard, multi-tenancy, OpenTelemetry observability, and "where is my message?" tracing out of the box. |
| **Security model** | Camel provides individual component-level security, but centralized input sanitization, payload guards, secret rotation, and tenant isolation must be built by the team. | Security, secret management, and multi-tenant isolation are platform-level concerns, not per-route afterthoughts. |

**Honest caveat:** Camel's connector ecosystem (300+ components) is vastly larger than this platform's four connector types (HTTP, SFTP, Email, File). If your primary need is connecting to a wide variety of systems with minimal custom code, Camel is a better choice today. This platform prioritizes depth of pattern implementation and operational completeness over breadth of connectors.

**3. The EIP book patterns are still sound, but the book's tooling context is obsolete.**

The EIP patterns (Content-Based Router, Splitter, Aggregator, Dead Letter Channel, etc.) are technology-agnostic and remain the correct vocabulary for messaging integration. The problem is not the patterns — it is that:

- The book's examples reference JMS, TIBCO, and early SOAP-era middleware. Practitioners must mentally translate these to modern brokers (Kafka, NATS, Pulsar).
- The book doesn't address stateful workflow orchestration, saga compensation, or event sourcing — patterns that are now essential for reliable distributed systems.
- The book doesn't address multi-tenancy, AI-assisted observability, or cloud-native deployment concerns.
- No existing open-source .NET project systematically implements the full EIP catalog as a runnable, tested platform with modern infrastructure.

This platform fills that gap: every EIP pattern from every chapter (Messaging Channels, Message Construction, Message Routing, Message Transformation, Messaging Endpoints, System Management) is mapped to a platform component, implemented in production-quality C#, and covered by tests. See [`docs/eip-mapping.md`](docs/eip-mapping.md) for the complete mapping.

**4. AI in integration: what actually matters vs. what is hype.**

The integration industry is saturated with AI marketing: "AI-powered iPaaS," "AI-driven data mapping," "autonomous integration agents." Most of this is vaporware or thin wrappers around LLM APIs with no durable context.

This platform takes a pragmatic, limited approach to AI:

- **Self-hosted RAG (RagFlow + Ollama)** indexes the platform's own source code, documentation, and rules. This is useful because integration platforms are large and complex — developers spend significant time searching for "how does this platform handle X?" The RAG system provides context retrieval, not autonomous execution.
- **Developers use their own AI provider** (Copilot, Codex, Claude Code) for code generation. The platform provides context; the developer's AI generates code. No data leaves the infrastructure.
- **"Where is my message?" (OpenClaw)** — a natural-language query against the observability layer. This is operationally useful for support teams troubleshooting message flows. It is not "AI-powered integration" — it is AI-assisted search over structured telemetry data.

**What this platform does NOT claim:** It does not claim that AI can autonomously build integrations, replace integration architects, or eliminate the need for understanding EIP patterns. AI assists; humans design and review.

**5. Why not just use Azure Integration Services (Logic Apps + Service Bus + Event Grid)?**

If you are fully committed to Azure and comfortable with vendor lock-in, Azure Integration Services is a reasonable choice. But there are real trade-offs:

| Concern | Azure Integration Services | This Platform |
|---|---|---|
| **Vendor lock-in** | Deep Azure dependency. Your integration logic is expressed in Logic Apps JSON workflow definitions, bound to Azure connectors, and deployed on Azure infrastructure. Moving to AWS or on-premises requires a full rewrite. | Runs anywhere .NET runs: on-premises, any cloud, Kubernetes, or a developer laptop. Broker choice is configuration, not architecture. |
| **Cost at scale** | Logic Apps charges per action execution. At high message volumes (millions/day), costs scale linearly and can become significant. Service Bus charges per message operation. | Infrastructure cost is fixed (you run the brokers). Compute cost scales with your Kubernetes cluster, not per-message pricing. |
| **Observability** | Azure Monitor + Application Insights. Powerful, but Azure-specific. Cross-cloud or on-premises observability requires additional tooling. | OpenTelemetry (vendor-neutral). Traces, metrics, and logs work with any backend: Grafana, Jaeger, Datadog, or Azure Monitor. |
| **Workflow durability** | Logic Apps stateful workflows exist but are less mature than Temporal for complex, long-running orchestrations with compensation. Durable Functions are closer but have their own limitations (function chaining, fan-out patterns). | Temporal.io is purpose-built for durable workflow orchestration with native saga compensation, signals, queries, and continue-as-new for unbounded workflows. |
| **Data residency** | Data flows through Azure regions. Compliance with data sovereignty regulations requires careful region selection and may limit service availability. | All data stays in your infrastructure. Full control over data residency with no cloud provider dependency. |

**Honest caveat:** Azure Integration Services has a massive connector ecosystem (400+ connectors), enterprise support from Microsoft, and minimal operational overhead (serverless). If operational simplicity and breadth of connectors matter more than control and portability, Azure is the easier path.

### What This Platform Actually Is

It is a .NET 10 implementation of the full Enterprise Integration Patterns catalog, with four interchangeable message brokers, Temporal.io for durable workflow orchestration, and a self-hosted RAG system for developer productivity. It is designed for organizations that:

1. **Run on .NET** and want to stay on .NET for integration middleware.
2. **Need broker flexibility** — choose NATS, Kafka, Pulsar, or PostgreSQL at deployment time without code changes.
3. **Need durable orchestration** — long-running workflows with saga compensation that survive infrastructure failures.
4. **Need data sovereignty** — all data stays on your infrastructure, no cloud vendor dependency.
5. **Are migrating from BizTalk** — concept mapping from BizTalk artifacts (orchestrations, maps, pipelines, ports) to platform equivalents is documented in [`docs/migration-from-biztalk.md`](docs/migration-from-biztalk.md).

### What This Platform Is NOT

- It is **not a low-code/no-code tool**. You write C# code. If you need visual drag-and-drop integration, use Logic Apps, MuleSoft, or Boomi.
- It is **not a connector marketplace**. It has four connector types (HTTP, SFTP, Email, File). If you need 300+ pre-built connectors, use Apache Camel or MuleSoft.
- It is **not production-battle-tested at scale**. It is a well-architected, well-tested platform (2,000+ tests), but it has not processed billions of messages in a Fortune 500 production environment. BizTalk, Camel, and MuleSoft have that track record.
- It is **not a replacement for Apache Camel in all scenarios**. If your team is Java-native and your primary need is connecting many heterogeneous systems with minimal code, Camel is likely the better tool.

---

## Highlights

- **Interchangeable Message Brokers** — NATS JetStream (default), Apache Kafka, Apache Pulsar Key_Shared, or PostgreSQL (SKIP LOCKED). Switch at deployment time via configuration. Recipient A never blocks Recipient B.
- **Temporal Workflow Orchestration** — Durable, stateful workflows with automatic retry, saga compensation, and signals via [Temporal.io](https://temporal.io/). Workflows survive process crashes and infrastructure failures.
- **Full EIP Pattern Coverage** — Systematic implementation of every pattern from the Hohpe/Woolf book. See [`docs/eip-mapping.md`](docs/eip-mapping.md).
- **Self-Hosted RAG (OpenClaw)** — RagFlow + Ollama index the platform's source code and docs. Developers retrieve context via API and use their own AI provider for code generation. All data stays on-premises.
- **.NET Aspire** — Single-command local orchestration of all services, brokers, and infrastructure containers.
- **OpenTelemetry** — Vendor-neutral distributed tracing, Prometheus metrics, and structured logging across every layer.

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
