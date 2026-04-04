# Platform Usage Guide

> End-to-end guide for operating, configuring, and extending the
> Enterprise Integration Platform. Covers getting started, configuration,
> deployment, connector setup, throttle/rate-limit tuning, multi-tenancy,
> security, and observability.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Configuration](#2-configuration)
3. [Deployment](#3-deployment)
4. [Submitting Messages](#4-submitting-messages)
5. [Connector Setup](#5-connector-setup)
6. [Routing & Transformation](#6-routing--transformation)
7. [Throttle & Rate-Limit Tuning](#7-throttle--rate-limit-tuning)
8. [Dead Letter Queue Management](#8-dead-letter-queue-management)
9. [Multi-Tenancy](#9-multi-tenancy)
10. [Security](#10-security)
11. [Observability & Monitoring](#11-observability--monitoring)
12. [Disaster Recovery](#12-disaster-recovery)
13. [AI-Driven Integration Generation](#13-ai-driven-integration-generation)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. Getting Started

### Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 10.0+ | Build and run the platform |
| Docker | Latest | Container infrastructure (Cassandra, Kafka, NATS, etc.) |
| Node.js | 20+ | Admin.Web Vue 3 frontend build |
| .NET Aspire | 13.1.2 | Local orchestration |

### Quick Start

```bash
# Clone and navigate to the platform
cd EnterpriseIntegrationPlatform

# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build

# Run the Aspire AppHost (starts all services locally)
dotnet run --project src/AppHost
```

The Aspire dashboard opens automatically at the configured port, showing all running services, their health status, and links to individual service endpoints.

### Running Tests

```bash
# Run all unit tests
dotnet test tests/UnitTests

# Run contract tests
dotnet test tests/ContractTests

# Run workflow tests (requires Temporal dev server)
dotnet test tests/WorkflowTests

# Run Playwright E2E tests (requires browsers installed)
dotnet test tests/PlaywrightTests

# Run Admin.Web frontend tests
cd src/Admin.Web/clientapp && npm run test
```

### Project Structure Overview

```
EnterpriseIntegrationPlatform/
├── src/
│   ├── AppHost/                    # .NET Aspire orchestrator
│   ├── Gateway.Api/                # Inbound message gateway with rate limiting
│   ├── Admin.Api/                  # Administration API
│   ├── Admin.Web/                  # Vue 3 admin dashboard
│   ├── OpenClaw.Web/               # "Where is my message?" UI
│   ├── Ingestion/                  # Broker abstraction layer
│   ├── Ingestion.Kafka/            # Kafka broker implementation
│   ├── Ingestion.Nats/             # NATS JetStream implementation
│   ├── Ingestion.Pulsar/           # Pulsar implementation
│   ├── Processing.*/               # Message processing components
│   ├── Connector.*/                # Outbound delivery connectors
│   ├── Workflow.Temporal/          # Temporal workflow orchestration
│   ├── Storage.Cassandra/          # Cassandra persistence
│   ├── Observability/              # OpenTelemetry integration
│   ├── AI.Ollama/                  # Ollama AI service
│   ├── AI.RagFlow/                 # RagFlow RAG service
│   └── AI.RagKnowledge/            # XML-based RAG knowledge index
├── tests/                          # 6 test projects
├── docs/                           # Documentation
└── rules/                          # Development rules and milestones
```

---

## 2. Configuration

### Broker Selection

The platform supports three brokers. The broker choice is a deployment-time configuration switch per message flow category.

| Configuration | Broker | Best For |
|---------------|--------|----------|
| `Broker:Type = "nats"` | NATS JetStream (default) | Local dev, testing, cloud deployments |
| `Broker:Type = "kafka"` | Apache Kafka | High-throughput event streams, audit logs |
| `Broker:Type = "pulsar"` | Apache Pulsar | Large-scale production, recipient-based ordering |

Configuration in `appsettings.json`:

```json
{
  "Broker": {
    "Type": "nats",
    "Nats": {
      "Url": "nats://localhost:4222"
    },
    "Kafka": {
      "BootstrapServers": "localhost:9092"
    },
    "Pulsar": {
      "ServiceUrl": "pulsar://localhost:6650"
    }
  }
}
```

### Aspire Service Ports

The platform uses the 15xxx port range to avoid conflicts:

| Service | Default Port |
|---------|-------------|
| Gateway.Api | 15100-range |
| Admin.Api | 15180 |
| Admin.Web | 15200-range |
| OpenClaw.Web | 15300-range |
| Ollama | 15434 |
| Cassandra | 15942 |
| Temporal | 15233 |

### Environment Variables

Services accept configuration from Aspire environment variables:

- `Ollama__BaseAddress` — Ollama API URL
- `Loki__BaseAddress` — Loki logging URL
- `RagFlow__BaseAddress` — RagFlow RAG API URL
- `AdminApi__BaseAddress` — Admin API URL for proxy
- `AdminApi__ApiKey` — API key for Admin API authentication

---

## 3. Deployment

### Local Development (Aspire)

```bash
dotnet run --project src/AppHost
```

Aspire starts all services and infrastructure containers (Cassandra, Kafka/NATS, Temporal, Ollama, Loki, Grafana) with health checks and dependency ordering.

### Docker Compose

Each service has a Dockerfile. The `docker-compose.yml` in the AppHost project orchestrates the full stack:

```bash
docker compose up -d
```

### Kubernetes

For production deployment:

1. Build container images for each service
2. Apply Kubernetes manifests (Deployments, Services, ConfigMaps)
3. Configure Cassandra as a StatefulSet with persistent volumes
4. Deploy Kafka/NATS/Pulsar as the chosen broker
5. Deploy Temporal server with its dependencies
6. Configure Ingress for Gateway.Api and Admin.Web

Key Kubernetes considerations:
- **Cassandra**: StatefulSet with 3 replicas (RF=3), anti-affinity rules
- **Temporal**: Use the official Temporal Helm chart
- **Broker**: Use the official operator (Strimzi for Kafka, NATS Operator, Pulsar Operator)
- **Processing services**: Deployments with horizontal pod autoscaling based on queue depth

---

## 4. Submitting Messages

### Via Gateway API

Submit messages through the Gateway API:

```
POST /api/gateway/submit
Content-Type: application/json

{
  "messageType": "OrderCreated",
  "payload": { ... },
  "businessKey": "order-123",
  "priority": "Normal",
  "intent": "Event"
}
```

The gateway:
1. Validates the request
2. Applies rate limiting
3. Wraps the payload in an `IntegrationEnvelope`
4. Publishes to the configured broker
5. Returns the assigned `MessageId` and `CorrelationId`

### Tracking Messages

Use OpenClaw ("Where is my message?") to track message lifecycle:

```
GET /api/inspect/business/{businessKey}
```

Returns the full lifecycle timeline, current status, and AI-powered trace analysis (when Ollama is available).

---

## 5. Connector Setup

### HTTP Connector

Configure outbound HTTP delivery:

```json
{
  "connectorType": "http",
  "targetUrl": "https://api.target-system.com/receive",
  "authentication": {
    "type": "OAuth2",
    "tokenEndpoint": "https://auth.target-system.com/token",
    "clientId": "integration-platform",
    "clientSecret": "{{secret:http-connector-client-secret}}",
    "scopes": ["api.write"]
  },
  "headers": {
    "X-Source": "EIP"
  },
  "timeout": "00:00:30",
  "retryPolicy": {
    "maxAttempts": 3,
    "initialInterval": "00:00:02",
    "backoffCoefficient": 2.0
  }
}
```

Authentication types: OAuth2 (with token caching), Bearer, ApiKey, ClientCertificate, Basic.

### SFTP Connector

Configure SFTP file delivery:

```json
{
  "connectorType": "sftp",
  "host": "sftp.target-system.com",
  "port": 22,
  "username": "integration",
  "authentication": {
    "type": "SshKey",
    "privateKeyPath": "/secrets/sftp-key"
  },
  "remotePath": "/incoming/{timestamp:yyyyMMdd}/{messageId}.json",
  "atomicRename": true
}
```

### Email Connector

Configure SMTP email delivery:

```json
{
  "connectorType": "email",
  "smtpHost": "smtp.company.com",
  "smtpPort": 587,
  "useTls": true,
  "from": "integrations@company.com",
  "to": ["recipient@partner.com"],
  "subject": "Integration: {messageType}",
  "bodyTemplate": "email-template.liquid"
}
```

### File Connector

Configure local/network file delivery:

```json
{
  "connectorType": "file",
  "outputPath": "/data/outbound/{messageType}/{timestamp:yyyyMMdd}/{messageId}.json",
  "encoding": "utf-8",
  "atomicWrite": true
}
```

### Connector Health Checks

All connectors expose health checks. Monitor via the Admin Dashboard or the `/health` endpoint. The `ConnectorHealthAggregator` in the unified `Connectors` project provides a single view of all connector health states.

---

## 6. Routing & Transformation

### Content-Based Routing

Define routing rules to direct messages based on content:

```json
{
  "routes": [
    {
      "name": "high-priority-orders",
      "condition": {
        "field": "$.priority",
        "operator": "equals",
        "value": "High"
      },
      "destination": "fast-track-pipeline"
    },
    {
      "name": "international-shipments",
      "condition": {
        "field": "$.payload.country",
        "operator": "notEquals",
        "value": "US"
      },
      "destination": "international-pipeline"
    }
  ],
  "defaultDestination": "standard-pipeline"
}
```

### Message Transformation

Transform messages between formats using Processing.Transform activities:

- **Content Enricher**: Augment messages with external data (HTTP lookups, database queries)
- **Content Filter**: Remove sensitive or unnecessary fields (PII masking, field whitelisting)
- **Message Normalizer**: Convert from source format to canonical format (XML→JSON, CSV→JSON)
- **Message Translator**: Apply schema mapping (field renaming, type conversion, structural transformation)

### Routing Slip

Attach a routing slip for multi-step processing:

```json
{
  "routingSlip": [
    "validate-schema",
    "enrich-customer-data",
    "transform-to-canonical",
    "route-to-destination",
    "deliver"
  ]
}
```

Each step processes the message and advances to the next step in the slip.

---

## 7. Throttle & Rate-Limit Tuning

### Managing Throttle Policies

Use the Admin Dashboard (Admin.Web) or Admin API to manage throttle policies:

**Create/Update a throttle policy:**

```
PUT /api/admin/throttle/policies
Content-Type: application/json

{
  "policyId": "high-volume-orders",
  "name": "High Volume Order Processing",
  "tenantId": "tenant-A",
  "queue": "orders",
  "endpoint": "order-processor",
  "maxMessagesPerSecond": 500,
  "burstCapacity": 1000,
  "maxWaitTimeSeconds": 30,
  "isEnabled": true,
  "rejectOnBackpressure": false
}
```

**List all throttle policies:**

```
GET /api/admin/throttle/policies
```

**Delete a throttle policy:**

```
DELETE /api/admin/throttle/policies/{policyId}
```

### Rate Limiting at the Gateway

The Gateway API enforces rate limiting per tenant and per endpoint. Rate limit status is available at:

```
GET /api/admin/ratelimit/status
```

### Tuning Guidelines

| Scenario | MaxMsgPerSec | BurstCapacity | RejectOnBackpressure |
|----------|-------------|---------------|---------------------|
| Low-latency APIs | 100-500 | 2× MaxMsgPerSec | true |
| Batch processing | 1000-5000 | 3× MaxMsgPerSec | false |
| Event streams | 5000-10000 | 2× MaxMsgPerSec | false |
| Development/test | 50 | 100 | false |

---

## 8. Dead Letter Queue Management

### Inspecting DLQ Messages

Via the Admin Dashboard (DLQ page) or Admin API:

```
GET /api/admin/faults/correlation/{correlationId}
```

DLQ entries include:
- Original message envelope (unmodified)
- Error details (exception type, message, stack trace)
- Processing history (which steps succeeded/failed)
- Retry metadata (attempt count, last attempt timestamp)

### Resubmitting Messages

Resubmit a DLQ message back into the pipeline:

```
POST /api/admin/dlq/resubmit
Content-Type: application/json

{
  "correlationId": "abc123-...",
  "messageType": "OrderCreated"
}
```

The message is republished with a `ReplayId` header for audit trail.

### DLQ Best Practices

- Monitor DLQ depth as a key metric — rising counts indicate systemic issues
- Investigate root causes before bulk resubmission
- Use the Admin Dashboard for individual message inspection
- Set alerts for DLQ accumulation (see Observability section)

---

## 9. Multi-Tenancy

### Tenant Isolation

The platform enforces tenant isolation at multiple levels:

- **Broker level**: Separate topics/subjects per tenant (e.g., `tenantA.orders`, `tenantB.orders`)
- **Storage level**: Cassandra partition keys include tenant ID for data isolation
- **Processing level**: Tenant context propagated through the `IntegrationEnvelope.Metadata` dictionary
- **Rate limiting**: Per-tenant throttle policies and rate limits
- **Security**: Tenant resolution via JWT claims or API key scoping

### Tenant Onboarding

The `MultiTenancy.Onboarding` project provides automated tenant provisioning:

1. Create tenant record with configuration
2. Provision broker topics/subjects
3. Configure default throttle policies
4. Set up tenant-specific routing rules
5. Assign connector configurations

### Tenant Configuration

```json
{
  "tenantId": "tenant-A",
  "name": "Acme Corp",
  "maxMessagesPerSecond": 1000,
  "maxConcurrentWorkflows": 50,
  "enabledConnectors": ["http", "sftp"],
  "routingOverrides": {}
}
```

---

## 10. Security

### Authentication

The platform supports multiple authentication methods:

| Method | Use Case | Configuration |
|--------|----------|---------------|
| JWT Bearer | API access, user authentication | `Security:Jwt:Authority`, `Security:Jwt:Audience` |
| API Key | Service-to-service, Admin API | `AdminApi:ApiKey` header |
| mTLS | Service mesh, internal communication | Certificate configuration |
| OAuth 2.0 | Connector outbound auth | Per-connector configuration |

### Secrets Management

Secrets are managed via `Security.Secrets` with support for:

- **Azure Key Vault**: Production secrets storage
- **HashiCorp Vault**: On-premises secrets management
- **Configuration**: Development-only (appsettings.json)

Secret references use the `{{secret:key-name}}` syntax in configuration files. The `ISecretProvider` resolves secrets at runtime.

### Security Best Practices

- Never store secrets in source code or configuration files
- Rotate API keys and certificates on a regular schedule
- Use mTLS for all internal service-to-service communication
- Enable audit logging for all administrative operations
- Apply the principle of least privilege for tenant access

---

## 11. Observability & Monitoring

### Three Pillars

| Pillar | Technology | Purpose |
|--------|-----------|---------|
| **Distributed Tracing** | OpenTelemetry → Jaeger/Tempo | End-to-end message flow visibility |
| **Structured Logging** | OpenTelemetry → Loki | Searchable event logs |
| **Metrics** | Prometheus + Grafana | Real-time performance dashboards |

### Key Metrics to Monitor

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| `eip_messages_processed_total` | Total messages processed | N/A (counter) |
| `eip_messages_failed_total` | Total processing failures | > 1% of processed |
| `eip_dlq_depth` | Messages in dead letter queue | > 100 |
| `eip_consumer_lag` | Broker consumer lag | > 10,000 |
| `eip_workflow_duration_seconds` | Workflow execution time | p99 > 30s |
| `eip_connector_latency_seconds` | Connector delivery latency | p99 > 10s |

### OpenClaw — "Where Is My Message?"

OpenClaw provides operator-facing message tracking:

- **Web UI**: Enter a business key, order number, or correlation ID
- **API**: `GET /api/inspect/business/{key}` or `GET /api/inspect/correlation/{id}`
- **AI Analysis**: When Ollama is available, provides AI-generated trace analysis summaries
- **Timeline View**: Visual lifecycle timeline showing every processing step with timestamps

### Admin Dashboard

The Admin.Web Vue 3 dashboard provides:

- **Dashboard**: Platform status overview
- **Throttle Management**: CRUD for throttle policies
- **DLQ Inspector**: View and resubmit failed messages
- **DR Drills**: Execute and review disaster recovery drills
- **Message Inspector**: Search messages by ID or correlation
- **Profiling**: Memory snapshots and GC diagnostics
- **Rate Limiting**: View current rate limit status

---

## 12. Disaster Recovery

### DR Drill Execution

Execute DR drills via the Admin Dashboard or API:

```
POST /api/admin/dr/drills
Content-Type: application/json

{
  "scenarioId": "cassandra-failover",
  "targetRegion": "us-east-1"
}
```

### DR Drill History

```
GET /api/admin/dr/drills/history
```

### Backup Strategy

| Component | Backup Method | Frequency | Retention |
|-----------|--------------|-----------|-----------|
| Cassandra | Snapshot + incremental | Daily + continuous | 30 days |
| Kafka | Topic replication (RF=3) | Continuous | Configurable |
| Temporal | Workflow history persistence | Continuous | 90 days |
| Configuration | Git-versioned | On change | Indefinite |

### Recovery Procedures

1. **Cassandra node failure**: Automatic repair with RF=3; no data loss
2. **Broker partition failure**: Replica promotion; consumer group rebalance
3. **Temporal worker failure**: Workflow resumes on healthy worker; no state loss
4. **Full region failure**: Failover to secondary region; restore from latest backups

---

## 13. AI-Driven Integration Generation

### Self-Hosted RAG System

The platform includes a self-hosted RAG system (RagFlow + Ollama):

- **RagFlow**: Indexes docs, rules, and source code as the knowledge base
- **Ollama**: Provides local LLM inference for embeddings and retrieval
- **AI.RagKnowledge**: XML-based knowledge documents covering all 65 EIP patterns

### Developer Workflow

Developers use their preferred AI provider (GitHub Copilot, Codex, Claude Code) connecting to the platform's RAG API:

1. Developer describes the integration needed
2. AI provider calls the platform's `/api/generate/integration` endpoint
3. RagFlow retrieves relevant platform context (patterns, conventions, examples)
4. AI provider generates production-ready integration code using the context

### RAG API Endpoints

| Endpoint | Purpose |
|----------|---------|
| `POST /api/generate/integration` | Retrieve context for integration generation |
| `POST /api/generate/connector` | Retrieve context for connector generation |
| `POST /api/generate/schema` | Retrieve context for schema generation |
| `POST /api/generate/chat` | Multi-turn chat with knowledge retrieval |
| `GET /api/generate/datasets` | List available knowledge datasets |

---

## 14. Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Message not processing | Consumer lag or stalled workflow | Check consumer lag metrics; inspect Temporal UI for stuck workflows |
| DLQ accumulating | Downstream system unavailable | Check connector health; verify target system availability |
| High latency | Overloaded processing pipeline | Scale consumers; adjust throttle policies; check resource utilization |
| Auth failures | Expired tokens or misconfigured secrets | Rotate credentials; verify secret provider configuration |
| Ollama unavailable | Service not running or unreachable | Check Ollama container status; verify base address configuration |

### Diagnostic Endpoints

| Endpoint | Service | Purpose |
|----------|---------|---------|
| `/health` | All services | Liveness check |
| `/health/ready` | All services | Readiness check |
| `/api/health/ollama` | OpenClaw.Web | Ollama connectivity |
| `/api/health/ragflow` | OpenClaw.Web | RagFlow connectivity |
| `/api/health/seeder` | OpenClaw.Web | Demo data seeder status |
| `/metrics` | OpenClaw.Web | Prometheus metrics |
| `/api/admin/status` | Admin.Web | Platform status overview |

### Log Analysis

Use Loki/Grafana for log analysis:

```
{service="gateway-api"} |= "error" | logfmt
{service="processing-routing"} |= "DLQ" | json
{service="connector-http"} |= "timeout" | logfmt
```
