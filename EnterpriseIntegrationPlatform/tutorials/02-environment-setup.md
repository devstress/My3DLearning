# Tutorial 02 — Setting Up Your Environment

## What You'll Learn

- Install .NET 10 SDK and Docker Desktop
- Clone and build the platform
- Run the test suite
- Launch the platform with .NET Aspire
- Navigate the project structure

---

## Prerequisites

### .NET 10 SDK

The platform targets **.NET 10** with C# 14. Install the SDK:

**Windows (winget):**
```powershell
winget install Microsoft.DotNet.SDK.10
```

**macOS (Homebrew):**
```bash
brew install dotnet-sdk@10
```

**Ubuntu/Debian:**
```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

**Verify installation:**
```bash
dotnet --version
# Should output 10.0.x
```

### Docker Desktop

Docker is required to run infrastructure containers (Kafka, NATS, Temporal, Cassandra, Ollama) via .NET Aspire:

- Download from [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/)
- Ensure Docker is running before starting the platform

### .NET Aspire Templates

```bash
dotnet new install Aspire.ProjectTemplates
```

---

## Clone and Build

### Step 1: Clone the Repository

```bash
git clone https://github.com/devstress/My3DLearning.git
cd My3DLearning/EnterpriseIntegrationPlatform
```

### Step 2: Restore Dependencies

```bash
dotnet restore EnterpriseIntegrationPlatform.sln
```

This downloads all NuGet packages defined in `Directory.Packages.props` (central package management).

### Step 3: Build the Solution

```bash
dotnet build EnterpriseIntegrationPlatform.sln
```

A clean build should complete with **0 errors**. The solution contains many projects — this takes 30–60 seconds on first build.

### Step 4: Run the Tests

```bash
dotnet test EnterpriseIntegrationPlatform.sln
```

The test suite includes:

| Test Project | Description |
|-------------|-------------|
| UnitTests | Fast, isolated tests for every component (most numerous) |
| ContractTests | Contract verification between services |
| WorkflowTests | Temporal workflow behavior tests |
| IntegrationTests | Testcontainers-based tests with real infrastructure |
| PlaywrightTests | End-to-end browser tests for OpenClaw UI |
| LoadTests | Performance and throughput benchmarks |

> **Note:** IntegrationTests and PlaywrightTests require Docker to be running.

---

## Launch with .NET Aspire

```bash
cd src/AppHost
dotnet run
```

The Aspire dashboard opens automatically in your browser. You'll see:

- **All platform services** — Gateway.Api, Admin.Api, OpenClaw.Web, Demo.Pipeline
- **Infrastructure containers** — Kafka, NATS, Temporal, Cassandra, Ollama
- **Logs** — Structured logs from every service in one view
- **Traces** — Distributed traces showing message flow
- **Metrics** — Prometheus-compatible metrics

### The Aspire Dashboard

The dashboard at `https://localhost:15888` (or the URL shown in console output) shows:

```
┌─────────────────────────────────────────────────────────┐
│                  Aspire Dashboard                         │
│                                                          │
│  Resources:                                              │
│  ✅ gateway-api          Running   :8080                 │
│  ✅ admin-api            Running   :8081                 │
│  ✅ openclaw-web         Running   :8082                 │
│  ✅ kafka                Running   :15092                │
│  ✅ nats                 Running   :15222                │
│  ✅ temporal             Running   :15233                │
│  ✅ cassandra            Running   :15942                │
│  ✅ ollama               Running   :15434                │
│                                                          │
│  [Logs] [Traces] [Metrics] [Structured]                  │
└─────────────────────────────────────────────────────────┘
```

> **Port Range:** The platform uses ports in the 15xxx range to avoid conflicts with existing services on your machine.

---

## Project Structure Overview

```
EnterpriseIntegrationPlatform/
├── src/                          # Source code
│   ├── AppHost/                  # .NET Aspire orchestrator
│   ├── ServiceDefaults/          # Shared OpenTelemetry & health checks
│   ├── Contracts/                # IntegrationEnvelope & shared interfaces
│   ├── Ingestion/                # Broker abstraction layer
│   ├── Ingestion.Kafka/          # Apache Kafka provider
│   ├── Ingestion.Nats/           # NATS JetStream provider
│   ├── Ingestion.Pulsar/         # Apache Pulsar provider
│   ├── Workflow.Temporal/        # Temporal workflow worker
│   ├── Activities/               # Workflow activity implementations
│   ├── Processing.*/             # Message processing patterns
│   ├── Connector.*/              # Protocol-specific connectors
│   ├── Gateway.Api/              # API gateway (Messaging Gateway)
│   ├── Admin.Api/                # Administration REST API (Control Bus)
│   └── ...                       # And more (see full list in README)
│
├── tests/                        # Test projects
│   ├── UnitTests/                # Fast, isolated unit tests
│   ├── ContractTests/            # Contract verification tests
│   ├── WorkflowTests/            # Temporal workflow tests
│   ├── IntegrationTests/         # Testcontainers integration tests
│   ├── PlaywrightTests/          # E2E browser tests
│   └── LoadTests/                # Performance benchmarks
│
├── docs/                         # Architecture & design documentation
├── deploy/                       # Helm charts, Kustomize, K8s manifests
├── rules/                        # Development standards & milestones
└── tutorials/                    # This tutorial course
```

### Key Files

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Shared MSBuild properties (target framework, nullable, implicit usings) |
| `Directory.Packages.props` | Central NuGet package version management |
| `global.json` | SDK version constraint with `rollForward: latestMinor` |
| `EnterpriseIntegrationPlatform.sln` | Solution file linking all projects |

---

## IDE Setup

### Visual Studio 2022 (v17.12+)

1. Open `EnterpriseIntegrationPlatform.sln`
2. Set `AppHost` as the startup project
3. Press **F5** to launch with the Aspire dashboard

### Visual Studio Code

Install these extensions:
- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- [.NET Aspire](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-aspire)

### JetBrains Rider (2024.3+)

- Open the `.sln` file — Aspire support is built-in

---

## Verify Your Setup

Run this quick verification:

```bash
# 1. Build should succeed
dotnet build EnterpriseIntegrationPlatform.sln

# 2. Unit tests should pass
dotnet test tests/UnitTests/UnitTests.csproj

# 3. Aspire should start (Ctrl+C to stop)
cd src/AppHost && dotnet run
```

If all three succeed, your environment is ready.

---

## Troubleshooting

### "The SDK 'Microsoft.NET.Sdk' specified could not be found"

Install .NET 10 SDK. The `global.json` requires SDK 10.x.

### Aspire AppHost fails to start

1. Ensure Docker Desktop is running
2. Check that ports in the 15xxx range are free
3. Run `dotnet restore` to ensure NuGet packages are resolved

### Tests fail with framework errors

```bash
dotnet --list-runtimes
```

You need `Microsoft.NETCore.App 10.x.x` and `Microsoft.AspNetCore.App 10.x.x`.

---

## Lab Exercise

**Objective:** Build the solution, run the test suite, and launch the .NET Aspire dashboard to verify your development environment is fully operational.

### Step 1: Build and Run the Test Suite

Open a terminal in the repository root and execute:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

Confirm that the build succeeds with zero errors and that all test projects (UnitTests, ContractTests, WorkflowTests) report their results.

### Step 2: Launch the Aspire Dashboard

Start the orchestrator and open the dashboard:

```bash
cd src/AppHost
dotnet run
```

Open the Aspire dashboard URL printed in the console (typically `https://localhost:15888`). Locate the **Gateway.Api**, **Admin.Api**, and **OpenClaw.Web** resources. Click into each resource and verify its health check endpoint returns a healthy status.

### Step 3: Write a Unit Test

In the `tests/UnitTests/` project, create a test class named `EnvironmentVerificationTests`. Add a test method called `AppHost_ServiceDefaultsAssembly_CanBeLoaded` that uses `typeof(EnterpriseIntegrationPlatform.Contracts.IntegrationEnvelope<string>).Assembly` to verify the Contracts assembly loads successfully. Assert that the assembly is not null and that its `GetTypes()` array contains the `IntegrationEnvelope<T>` type.

## Knowledge Check

1. Which command restores NuGet packages for every project in the solution before building?
   - A) `dotnet build --restore`
   - B) `dotnet restore`
   - C) `dotnet nuget install`
   - D) `dotnet pack`

2. What is the purpose of the `src/AppHost` project in this platform?
   - A) It hosts the production web application behind a reverse proxy
   - B) It is the .NET Aspire orchestrator that starts all services and infrastructure containers in the correct order
   - C) It compiles all projects into a single deployable binary
   - D) It runs database migrations on startup

3. Why does the platform organize tests into separate projects (UnitTests, ContractTests, IntegrationTests) rather than a single test project?
   - A) .NET only allows one test framework per project
   - B) It enables running fast unit tests independently from slow integration tests that require infrastructure like Docker containers
   - C) Each test project must target a different .NET version
   - D) NUnit requires separate assemblies for parallel execution

---

**Previous: [← Tutorial 01 — Introduction](01-introduction.md)** | **Next: [Tutorial 03 — Your First Message →](03-first-message.md)**
