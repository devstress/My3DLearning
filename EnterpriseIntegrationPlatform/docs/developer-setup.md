# Developer Setup Guide

## Prerequisites

### .NET 10 SDK (Required)

This project targets **.NET 10**. You must have the .NET 10 SDK installed.

**Check your installed version:**

```bash
dotnet --version
```

If the output does not start with `10.`, follow the installation steps below.

**Install .NET 10 SDK:**

- **Windows / macOS / Linux** — Download from the official .NET site:
  <https://dotnet.microsoft.com/download/dotnet/10.0>

- **Windows (winget):**

  ```powershell
  winget install Microsoft.DotNet.SDK.10
  ```

- **macOS (Homebrew):**

  ```bash
  brew install dotnet-sdk@10
  ```

- **Ubuntu / Debian:**

  ```bash
  sudo apt-get update
  sudo apt-get install -y dotnet-sdk-10.0
  ```

- **Fedora / RHEL:**

  ```bash
  sudo dnf install dotnet-sdk-10.0
  ```

**Verify installation:**

```bash
dotnet --list-sdks
```

You should see an entry starting with `10.0.x`.

### .NET Aspire Templates (Required)

Install the latest Aspire project templates:

```bash
dotnet new install Aspire.ProjectTemplates
```

### Docker (Recommended)

Docker is needed to run infrastructure dependencies locally (Kafka, NATS, Temporal, Cassandra, Ollama) via .NET Aspire container orchestration.

- **Install Docker Desktop:** <https://www.docker.com/products/docker-desktop/>

Ensure Docker is running before starting the Aspire AppHost.

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd EnterpriseIntegrationPlatform
```

### 2. Restore Dependencies

```bash
dotnet restore EnterpriseIntegrationPlatform.sln
```

### 3. Build the Solution

```bash
dotnet build EnterpriseIntegrationPlatform.sln
```

### 4. Run Tests

```bash
dotnet test EnterpriseIntegrationPlatform.sln
```

### 5. Run the Aspire AppHost

```bash
cd src/AppHost
dotnet run
```

This launches the Aspire dashboard at the URL shown in the console output. The dashboard provides a unified view of all platform services, logs, traces, and metrics.

## IDE Setup

### Visual Studio 2022 (v17.12+)

- Open `EnterpriseIntegrationPlatform.sln`
- Set `AppHost` as the startup project
- Press F5 to launch with the Aspire dashboard

### Visual Studio Code

Install these extensions:

- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- [.NET Aspire](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-aspire)

### JetBrains Rider (2024.3+)

- Open the solution file
- Aspire support is built-in

## Project Structure

```
EnterpriseIntegrationPlatform/
├── src/
│   ├── AppHost/                     # .NET Aspire orchestrator
│   ├── ServiceDefaults/             # Shared service configuration (OpenTelemetry, health checks)
│   ├── Contracts/                   # Shared message contracts and interfaces
│   ├── Ingestion/                   # Broker abstraction (IMessageBrokerProducer/Consumer)
│   ├── Ingestion.Kafka/             # Kafka streaming provider
│   ├── Ingestion.Nats/              # NATS JetStream provider (default)
│   ├── Ingestion.Pulsar/            # Apache Pulsar Key_Shared provider
│   ├── Workflow.Temporal/           # Temporal workflow worker
│   ├── Activities/                  # Workflow activity implementations
│   ├── Processing.Routing/          # Content-based routing logic
│   ├── Processing.Translator/       # Message transformation logic
│   ├── Processing.Transform/        # Payload pipeline — JSON↔XML, regex, JSONPath
│   ├── Processing.Splitter/         # Message splitter
│   ├── Processing.Aggregator/       # Message aggregator
│   ├── Processing.ScatterGather/    # Scatter-Gather pattern
│   ├── Processing.CompetingConsumers/ # Competing Consumers with autoscaling
│   ├── Processing.DeadLetter/       # Dead letter queue management
│   ├── Processing.Retry/            # Retry framework
│   ├── Processing.Replay/           # Replay framework
│   ├── Processing.Throttle/         # Token-bucket throttle
│   ├── Processing.Dispatcher/       # Message Dispatcher & Service Activator
│   ├── Processing.RequestReply/     # Request-Reply correlator
│   ├── Processing.Resequencer/      # Resequencer — reorder out-of-sequence messages
│   ├── RuleEngine/                  # Business rule evaluation
│   ├── EventSourcing/               # Event store, snapshots, projection engine
│   ├── Connector.Http/              # HTTP connector
│   ├── Connector.Sftp/              # SFTP connector
│   ├── Connector.Email/             # Email connector
│   ├── Connector.File/              # File connector
│   ├── Connectors/                  # Unified connector registry & factory
│   ├── Storage.Cassandra/           # Cassandra data access layer
│   ├── Configuration/               # Dynamic config store, feature flags
│   ├── Security/                    # Input sanitization, payload guards, encryption
│   ├── Security.Secrets/            # Secret providers (Azure KV, Vault), rotation
│   ├── MultiTenancy/                # Tenant resolution and isolation
│   ├── MultiTenancy.Onboarding/     # Self-service tenant provisioning & quotas
│   ├── DisasterRecovery/            # Failover, replication, RPO/RTO, DR drills
│   ├── Performance.Profiling/       # CPU/memory profiling, GC tuning, benchmarks
│   ├── Observability/               # Lifecycle recording, Loki storage, OpenClaw API
│   ├── AI.Ollama/                   # Ollama AI integration
│   ├── AI.RagFlow/                  # RagFlow RAG client
│   ├── AI.RagKnowledge/             # RAG knowledge base parser & query matcher
│   ├── SystemManagement/            # Control Bus, Message Store, Smart Proxy, Test Message
│   ├── OpenClaw.Web/                # "Where is my message?" web UI & RAG knowledge API
│   ├── Admin.Web/                   # Vue 3 admin dashboard (proxies to Admin.Api)
│   ├── Gateway.Api/                 # API gateway (Messaging Gateway)
│   ├── Admin.Api/                   # Administration REST API
│   └── Demo.Pipeline/               # End-to-end demo pipeline
├── tests/
│   ├── UnitTests/               # Fast, isolated unit tests (402 tests)
│   ├── ContractTests/           # Contract verification tests (29 tests)
│   ├── WorkflowTests/           # Temporal workflow tests (24 tests)
│   ├── IntegrationTests/        # Testcontainers-based integration tests (17 tests)
│   ├── PlaywrightTests/         # End-to-end browser tests for Admin dashboard & OpenClaw UI (24 tests)
│   └── LoadTests/               # Performance and load tests (5 tests)
├── docs/                        # Architecture and design documentation
└── rules/                       # Development standards and milestones
```

## Technology Stack

| Component           | Technology              | Version   |
|---------------------|-------------------------|-----------|
| Runtime             | .NET                    | 10        |
| Language            | C#                      | 14        |
| Orchestration       | .NET Aspire             | 13.1.2    |
| Event Streaming     | Apache Kafka            | Latest    |
| Queue Broker        | NATS JetStream (default)| Latest    |
| Workflow Engine     | Temporal.io             | Latest    |
| Storage             | Apache Cassandra        | Latest    |
| Observability       | OpenTelemetry           | 1.14.0    |
| AI Runtime          | Ollama                  | Latest    |
| Testing             | NUnit + NSubstitute    | 4.4.0 / 5.3.0 |

## Troubleshooting

### `global.json` SDK version mismatch

If you see an error like:

```
The SDK 'Microsoft.NET.Sdk' specified could not be found
```

Ensure .NET 10 SDK is installed and matches the version range in `global.json`. The `rollForward: latestMinor` policy allows any 10.x SDK.

### Aspire AppHost fails to start

1. Ensure Docker Desktop is running
2. Check that no other process is using the ports defined in `src/AppHost/Properties/launchSettings.json`
3. Run `dotnet restore` to ensure all NuGet packages are resolved

### Tests fail with missing SDK

If tests fail with framework errors, verify that `net10.0` is available:

```bash
dotnet --list-runtimes
```

You should see `Microsoft.NETCore.App 10.x.x` and `Microsoft.AspNetCore.App 10.x.x`.
