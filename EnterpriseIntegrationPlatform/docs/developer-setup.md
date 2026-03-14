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
│   ├── AppHost/                 # .NET Aspire orchestrator
│   ├── ServiceDefaults/         # Shared service configuration (OpenTelemetry, health checks)
│   ├── Gateway.Api/             # HTTP ingress API
│   ├── Ingestion.Kafka/         # Message broker ingestion service (Kafka streaming + NATS/Pulsar queuing)
│   ├── Contracts/               # Shared message contracts and interfaces
│   ├── Workflow.Temporal/       # Temporal workflow worker
│   ├── Activities/              # Workflow activity implementations
│   ├── Connectors/              # Outbound connector plugins
│   ├── Processing.Transform/    # Message transformation logic
│   ├── Processing.Routing/      # Content-based routing logic
│   ├── Storage.Cassandra/       # Cassandra data access layer
│   ├── AI.Ollama/               # Ollama AI integration
│   ├── RuleEngine/              # Business rules engine
│   ├── Admin.Api/               # Administration REST API
│   ├── Admin.Web/               # Administration web UI
│   └── Observability/           # OpenTelemetry configuration
├── tests/
│   ├── UnitTests/
│   ├── IntegrationTests/
│   ├── ContractTests/
│   ├── WorkflowTests/
│   └── LoadTests/
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
| Testing             | xUnit + FluentAssertions| Latest    |

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
