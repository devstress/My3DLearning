# Quick Start — Your First Integration in 15 Minutes

> From zero to a working message flow in 15 minutes. No prior EIP experience required.

---

## What You'll Build

By the end of this guide you will have:

1. A running Enterprise Integration Platform on your machine
2. A message submitted through the Gateway API
3. That message routed, transformed, and tracked in the Admin Dashboard

---

## Step 1 — Install Prerequisites (5 minutes)

### .NET 10 SDK

```bash
# Check if installed
dotnet --version
# Should start with 10. If not, install:
```

| OS | Install Command |
|----|-----------------|
| **Windows** | `winget install Microsoft.DotNet.SDK.10` |
| **macOS** | `brew install dotnet-sdk@10` |
| **Ubuntu/Debian** | `sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0` |
| **Fedora/RHEL** | `sudo dnf install dotnet-sdk-10.0` |

Or download from <https://dotnet.microsoft.com/download/dotnet/10.0>.

### Docker Desktop

Install from <https://www.docker.com/products/docker-desktop/> and start it.

Docker provides the infrastructure services (Kafka, NATS, Temporal, Cassandra, Ollama) via .NET Aspire container orchestration.

### Node.js (v20+)

Required for the Admin.Web Vue 3 frontend:

```bash
node --version
# Should be 20.x or higher
```

Install from <https://nodejs.org/> if needed.

### .NET Aspire Templates

```bash
dotnet new install Aspire.ProjectTemplates
```

---

## Step 2 — Clone and Build (3 minutes)

```bash
# Clone the repository
git clone <repository-url>
cd My3DLearning/EnterpriseIntegrationPlatform

# Restore NuGet packages
dotnet restore

# Build the entire solution
dotnet build
```

You should see `Build succeeded. 0 Warning(s) 0 Error(s)`.

### Install Vue Frontend Dependencies

```bash
cd src/Admin.Web/clientapp
npm install
cd ../../..
```

---

## Step 3 — Start the Platform (2 minutes)

```bash
cd src/AppHost
dotnet run
```

Watch the console output. Aspire will:

1. Pull and start Docker containers (Kafka, NATS, Temporal, Cassandra, Ollama, Loki, Grafana)
2. Start all platform services (Gateway.Api, Admin.Api, Admin.Web, OpenClaw.Web, workers)
3. Display the **Aspire Dashboard URL** — open it in your browser

The Aspire Dashboard shows all running services, their health status, logs, and traces.

> **Tip:** First run takes longer because Docker pulls container images. Subsequent starts are much faster.

---

## Step 4 — Submit Your First Message (2 minutes)

Open a new terminal and send a message to the Gateway API:

```bash
curl -X POST http://localhost:15100/api/gateway/submit \
  -H "Content-Type: application/json" \
  -d '{
    "messageType": "OrderCreated",
    "payload": {
      "orderId": "ORD-001",
      "customer": "Alice",
      "total": 99.99,
      "items": [
        { "sku": "WIDGET-A", "quantity": 2, "price": 49.99 }
      ]
    },
    "businessKey": "ORD-001",
    "priority": "Normal",
    "intent": "Event"
  }'
```

You'll get a response like:

```json
{
  "messageId": "abc123-...",
  "correlationId": "def456-...",
  "status": "Accepted"
}
```

The Gateway has:
- ✅ Validated your request
- ✅ Applied rate limiting
- ✅ Wrapped the payload in an `IntegrationEnvelope`
- ✅ Published to the message broker
- ✅ Returned the `MessageId` and `CorrelationId` for tracking

---

## Step 5 — Track Your Message (3 minutes)

### Via OpenClaw ("Where Is My Message?")

Open the OpenClaw web UI (check the Aspire Dashboard for its URL, typically `http://localhost:15300`).

Enter your business key `ORD-001` or the `correlationId` from the response. You'll see:

- Full lifecycle timeline (received → validated → routed → transformed → delivered)
- Current status
- AI-generated trace analysis (when Ollama is available)

### Via the Admin Dashboard

Open the Admin.Web dashboard (check the Aspire Dashboard for its URL, typically `http://localhost:15200`).

Navigate to:

1. **Dashboard** — See the message count increment in real-time
2. **Message Flow** — See your message's flow through the pipeline on a visual timeline
3. **Messages** — Search by message ID or correlation ID to inspect the full envelope
4. **In-Flight** — See currently processing messages (if you send multiple quickly)

### Via the Admin API

```bash
# Track by correlation ID
curl http://localhost:15180/api/admin/messages/correlation/{correlationId}

# Track by business key
curl http://localhost:15180/api/admin/messages/business/ORD-001
```

---

## What Just Happened?

Here's the end-to-end flow your message took:

```
Your curl request
    │
    ▼
┌──────────────────┐
│  Gateway.Api     │  Validated, rate-limited, wrapped in IntegrationEnvelope
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Message Broker  │  Published to NATS JetStream (default broker)
│  (NATS/Kafka)    │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Temporal        │  Workflow started: validate → route → transform → deliver
│  Workflow        │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Activities      │  Content-based routing, transformations, enrichment
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Connector       │  Delivered to target system (HTTP, SFTP, Email, or File)
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Cassandra       │  Message, audit trail, and lifecycle events stored
└──────────────────┘
```

Every step was:
- **Traced** — OpenTelemetry captured distributed traces across all services
- **Logged** — Structured logs correlated with trace IDs stored in Loki
- **Metered** — Prometheus metrics recorded throughput, latency, and error rates

---

## Next Steps

| What | Where |
|------|-------|
| Explore the Admin Dashboard | [Admin UI Guide](admin-ui-guide.md) |
| Set up your development environment | [Developer Setup](developer-setup.md) |
| Complete hands-on tutorials | [Tutorial Course](../tutorials/README.md) (50 tutorials with labs & exams) |
| Understand the architecture | [Architecture Overview](architecture-overview.md) |
| Migrate from BizTalk | [BizTalk Migration Guide](migration-from-biztalk.md) |
| Configure connectors and routing | [Platform Usage Guide](platform-usage-guide.md) |
| Deploy to production | [Installation Guide](installation-guide.md) |
| Run the full onboarding checklist | [Onboarding Checklist](onboarding-checklist.md) |

---

## Troubleshooting

### Aspire fails to start

1. **Docker not running** — Start Docker Desktop first
2. **Port conflict** — Check that nothing is using the 15xxx port range
3. **Missing SDK** — Run `dotnet --list-sdks` and verify .NET 10.x is installed

### curl returns connection refused

The Gateway.Api may still be starting. Wait 30 seconds after Aspire starts and try again. Check the Aspire Dashboard for service health.

### "No brokers available" error

Docker containers are still starting. Wait for the Aspire Dashboard to show all services as "Running" before submitting messages.

### Vue frontend not loading

```bash
cd src/Admin.Web/clientapp
npm install
npm run build
```

Then restart the Aspire AppHost.
