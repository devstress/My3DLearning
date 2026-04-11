# Installation Guide

> Comprehensive guide for installing and deploying the Enterprise Integration Platform across all environments: local development, Docker Compose, and Kubernetes.

---

## Table of Contents

1. [System Requirements](#1-system-requirements)
2. [Local Development Setup (Aspire)](#2-local-development-setup-aspire)
3. [Docker Compose Deployment](#3-docker-compose-deployment)
4. [Kubernetes Deployment](#4-kubernetes-deployment)
5. [Broker Configuration](#5-broker-configuration)
6. [Infrastructure Services](#6-infrastructure-services)
7. [Admin Web Frontend](#7-admin-web-frontend)
8. [Security Configuration](#8-security-configuration)
9. [Observability Stack](#9-observability-stack)
10. [Verification](#10-verification)
11. [Upgrading](#11-upgrading)
12. [Uninstalling](#12-uninstalling)

---

## 1. System Requirements

### Minimum Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 4 cores | 8+ cores |
| RAM | 8 GB | 16+ GB |
| Disk | 20 GB free | 50+ GB SSD |
| OS | Windows 10+, macOS 12+, Ubuntu 20.04+ | Latest stable release |

### Software Prerequisites

| Tool | Version | Required For | Install |
|------|---------|-------------|---------|
| .NET SDK | 10.0+ | Build & run platform | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Docker Desktop | Latest | Infrastructure containers | [docker.com](https://www.docker.com/products/docker-desktop/) |
| Node.js | 20+ | Admin.Web Vue 3 frontend | [nodejs.org](https://nodejs.org/) |
| .NET Aspire Templates | Latest | Local orchestration | `dotnet new install Aspire.ProjectTemplates` |

### Optional Tools

| Tool | Purpose |
|------|---------|
| kubectl | Kubernetes deployment management |
| Helm | Kubernetes package management |
| k9s | Kubernetes cluster terminal UI |
| Temporal CLI | Workflow management and debugging |

---

## 2. Local Development Setup (Aspire)

This is the recommended approach for development and evaluation.

### Step 1 — Install .NET 10 SDK

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

**Fedora/RHEL:**
```bash
sudo dnf install dotnet-sdk-10.0
```

**Verify:**
```bash
dotnet --version
# Should output 10.0.x
```

### Step 2 — Install Docker Desktop

Download and install from <https://www.docker.com/products/docker-desktop/>.

Start Docker Desktop and verify:

```bash
docker --version
docker compose version
```

**Docker resource settings** (recommended):
- CPUs: 4+
- Memory: 8 GB+
- Disk: 40 GB+

### Step 3 — Install Node.js

Download from <https://nodejs.org/> or use a version manager:

```bash
# Using nvm (macOS/Linux)
nvm install 20
nvm use 20

# Using fnm (Windows/macOS/Linux)
fnm install 20
fnm use 20
```

Verify:
```bash
node --version
npm --version
```

### Step 4 — Install Aspire Templates

```bash
dotnet new install Aspire.ProjectTemplates
```

### Step 5 — Clone and Build

```bash
git clone <repository-url>
cd My3DLearning/EnterpriseIntegrationPlatform

# Restore NuGet packages
dotnet restore

# Build the entire solution (50 projects)
dotnet build
```

Expected output: `Build succeeded. 0 Warning(s) 0 Error(s)`

### Step 6 — Install Vue Frontend Dependencies

```bash
cd src/Admin.Web/clientapp
npm install
cd ../../..
```

### Step 7 — Start the Platform

```bash
cd src/AppHost
dotnet run
```

Aspire will:
1. Pull and start all Docker containers
2. Configure networking between services
3. Start all .NET platform services
4. Open the Aspire Dashboard

### Step 8 — Verify

Open the Aspire Dashboard URL shown in the console. All services should show as "Running":

| Service | Description |
|---------|-------------|
| gateway-api | Inbound message gateway |
| admin-api | Administration REST API |
| admin-web | Vue 3 admin dashboard |
| openclaw-web | Message tracking UI |
| nats | NATS JetStream broker |
| kafka | Apache Kafka broker |
| temporal | Temporal workflow server |
| cassandra | Apache Cassandra storage |
| ollama | Ollama AI runtime |
| loki | Log aggregation |
| grafana | Metrics dashboards |

---

## 3. Docker Compose Deployment

For staging or small production deployments.

### Step 1 — Build Container Images

```bash
cd EnterpriseIntegrationPlatform

# Build all service images
docker compose build
```

### Step 2 — Configure Environment

Create a `.env` file in the project root:

```env
# Broker configuration
BROKER_TYPE=nats

# NATS
NATS_URL=nats://nats:4222

# Kafka
KAFKA_BOOTSTRAP_SERVERS=kafka:9092

# Temporal
TEMPORAL_ADDRESS=temporal:7233
TEMPORAL_NAMESPACE=eip-production

# Cassandra
CASSANDRA_CONTACT_POINTS=cassandra:9042
CASSANDRA_KEYSPACE=eip_platform

# Ollama
OLLAMA_BASE_ADDRESS=http://ollama:11434

# Loki
LOKI_BASE_ADDRESS=http://loki:3100

# Admin API
ADMIN_API_KEY=your-secure-api-key-here

# Ports
GATEWAY_PORT=15100
ADMIN_API_PORT=15180
ADMIN_WEB_PORT=15200
OPENCLAW_PORT=15300
```

### Step 3 — Start Services

```bash
# Start all services in detached mode
docker compose up -d

# Watch logs
docker compose logs -f

# Check service health
docker compose ps
```

### Step 4 — Verify

```bash
# Check all services are running
docker compose ps

# Test Gateway API
curl http://localhost:15100/health/ready

# Test Admin API
curl http://localhost:15180/health/ready
```

### Stopping

```bash
# Stop all services (preserve data)
docker compose down

# Stop and remove all data
docker compose down -v
```

---

## 4. Kubernetes Deployment

For production deployments with high availability.

### Prerequisites

- Kubernetes cluster (v1.28+)
- kubectl configured
- Helm v3 installed
- Container registry accessible from the cluster

### Step 1 — Build and Push Images

```bash
# Build images
docker compose build

# Tag for your registry
docker tag eip-gateway-api your-registry/eip-gateway-api:latest
docker tag eip-admin-api your-registry/eip-admin-api:latest
docker tag eip-admin-web your-registry/eip-admin-web:latest

# Push to registry
docker push your-registry/eip-gateway-api:latest
docker push your-registry/eip-admin-api:latest
docker push your-registry/eip-admin-web:latest
```

### Step 2 — Deploy Infrastructure

Deploy infrastructure components using Helm charts:

```bash
# Add Helm repositories
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo add strimzi https://strimzi.io/charts/
helm repo add nats https://nats-io.github.io/k8s/
helm repo add temporal https://go.temporal.io/helm-charts
helm repo update

# Deploy Cassandra (StatefulSet, RF=3)
helm install cassandra bitnami/cassandra \
  --set replicaCount=3 \
  --set persistence.size=50Gi \
  --namespace eip-infra --create-namespace

# Deploy NATS JetStream (default broker)
helm install nats nats/nats \
  --set nats.jetstream.enabled=true \
  --set nats.jetstream.memStorage.size=2Gi \
  --set nats.jetstream.fileStorage.size=10Gi \
  --namespace eip-infra

# Deploy Temporal
helm install temporal temporal/temporal \
  --set server.replicaCount=3 \
  --namespace eip-infra

# Deploy Ollama
kubectl apply -f deploy/kustomize/ollama.yaml \
  --namespace eip-infra
```

### Step 3 — Deploy Platform Services

Using Kustomize:

```bash
# Apply namespace and ConfigMaps
kubectl apply -k deploy/kustomize/base

# Apply platform services
kubectl apply -k deploy/kustomize/overlays/production
```

Or using Helm:

```bash
helm install eip deploy/helm/eip \
  --set broker.type=nats \
  --set cassandra.contactPoints=cassandra.eip-infra:9042 \
  --set temporal.address=temporal-frontend.eip-infra:7233 \
  --namespace eip --create-namespace
```

### Step 4 — Configure Ingress

```yaml
# deploy/kustomize/overlays/production/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: eip-ingress
  namespace: eip
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  ingressClassName: nginx
  tls:
    - hosts:
        - eip.yourdomain.com
      secretName: eip-tls
  rules:
    - host: eip.yourdomain.com
      http:
        paths:
          - path: /api/gateway
            pathType: Prefix
            backend:
              service:
                name: gateway-api
                port:
                  number: 80
          - path: /api/admin
            pathType: Prefix
            backend:
              service:
                name: admin-api
                port:
                  number: 80
          - path: /admin
            pathType: Prefix
            backend:
              service:
                name: admin-web
                port:
                  number: 80
          - path: /openclaw
            pathType: Prefix
            backend:
              service:
                name: openclaw-web
                port:
                  number: 80
```

### Step 5 — Verify

```bash
# Check all pods are running
kubectl get pods -n eip
kubectl get pods -n eip-infra

# Check service health
kubectl port-forward svc/gateway-api 15100:80 -n eip
curl http://localhost:15100/health/ready

# Check Admin Dashboard
kubectl port-forward svc/admin-web 15200:80 -n eip
# Open http://localhost:15200
```

### Production Checklist

- [ ] Cassandra: 3+ nodes with RF=3 and anti-affinity rules
- [ ] NATS/Kafka: Clustered with replication for fault tolerance
- [ ] Temporal: 3+ frontend replicas with separate history/matching/worker services
- [ ] Gateway: 2+ replicas with HPA based on CPU/request rate
- [ ] Admin API: 2+ replicas behind internal service
- [ ] TLS: Enabled for all external endpoints
- [ ] Secrets: Managed via Kubernetes Secrets or external vault
- [ ] Monitoring: Prometheus + Grafana deployed (see [Observability Stack](#9-observability-stack))
- [ ] Backup: Cassandra snapshots and broker replication configured
- [ ] Resource limits: CPU and memory limits set for all pods

---

## 5. Broker Configuration

The platform supports four message brokers. Choose based on your workload.

### NATS JetStream (Default)

Best for: Local development, cloud deployments, low-latency task delivery.

```json
{
  "Broker": {
    "Type": "nats",
    "Nats": {
      "Url": "nats://localhost:4222"
    }
  }
}
```

### Apache Kafka

Best for: High-throughput event streaming, audit logs, analytics fan-out.

```json
{
  "Broker": {
    "Type": "kafka",
    "Kafka": {
      "BootstrapServers": "localhost:9092"
    }
  }
}
```

### Apache Pulsar

Best for: Large-scale production with recipient-based ordering via Key_Shared subscriptions.

```json
{
  "Broker": {
    "Type": "pulsar",
    "Pulsar": {
      "ServiceUrl": "pulsar://localhost:6650"
    }
  }
}
```

### PostgreSQL

Best for: Simpler deployments without dedicated broker infrastructure.

```json
{
  "Broker": {
    "Type": "postgres",
    "Postgres": {
      "ConnectionString": "Host=localhost;Database=eip;Username=eip;Password=secret"
    }
  }
}
```

### Switching Brokers

The broker is a deployment-time configuration choice. Integration code runs unchanged on all four brokers. Change the `Broker:Type` setting in `appsettings.json` or via environment variable:

```bash
export Broker__Type=kafka
```

---

## 6. Infrastructure Services

### Cassandra

The distributed storage layer for message payloads, audit logs, and workflow metadata.

**Keyspace creation** (auto-provisioned on first run in development):
```cql
CREATE KEYSPACE IF NOT EXISTS eip_platform
WITH REPLICATION = {
  'class': 'SimpleStrategy',
  'replication_factor': 1
};
```

**Production keyspace** (NetworkTopologyStrategy):
```cql
CREATE KEYSPACE IF NOT EXISTS eip_platform
WITH REPLICATION = {
  'class': 'NetworkTopologyStrategy',
  'datacenter1': 3
};
```

### Temporal

Workflow orchestration engine. Manages durable workflow execution with automatic retry, compensation, and resume-after-failure.

**Namespace creation:**
```bash
temporal operator namespace create eip-production
```

### Ollama

Self-hosted AI runtime for RAG-powered knowledge retrieval and message trace analysis.

**Verify Ollama is available:**
```bash
curl http://localhost:15434/api/version
```

---

## 7. Admin Web Frontend

The Admin.Web is a Vue 3 single-page application with 19 pages.

### Development Mode

```bash
cd src/Admin.Web/clientapp
npm install
npm run dev
```

The dev server proxies API calls to Admin.Api automatically.

### Production Build

```bash
cd src/Admin.Web/clientapp
npm run build
```

The built files are served by the Admin.Web ASP.NET host.

### Running Frontend Tests

```bash
cd src/Admin.Web/clientapp
npx vitest run
```

Expected: 100 tests passing across 16 test files.

---

## 8. Security Configuration

### API Key Authentication

The Admin API requires an API key for all requests:

```json
{
  "AdminApi": {
    "ApiKey": "your-secure-api-key-minimum-32-characters"
  }
}
```

Pass the key in requests:
```bash
curl -H "X-API-Key: your-secure-api-key" http://localhost:15180/api/admin/status
```

### Secret Management

Configure a secret provider for production:

**Azure Key Vault:**
```json
{
  "Security": {
    "Secrets": {
      "Provider": "AzureKeyVault",
      "VaultUri": "https://your-vault.vault.azure.net/"
    }
  }
}
```

**HashiCorp Vault:**
```json
{
  "Security": {
    "Secrets": {
      "Provider": "HashiCorpVault",
      "Address": "https://vault.internal:8200",
      "MountPath": "secret/eip"
    }
  }
}
```

### TLS Configuration

For production, enable TLS on all endpoints:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/certs/tls.crt",
          "KeyPath": "/certs/tls.key"
        }
      }
    }
  }
}
```

---

## 9. Observability Stack

### Components

| Component | Purpose | Port |
|-----------|---------|------|
| OpenTelemetry Collector | Receives traces, metrics, logs | 4317 (gRPC), 4318 (HTTP) |
| Loki | Log aggregation and querying | 3100 |
| Grafana | Dashboards and alerting | 3000 |
| Jaeger/Tempo | Distributed trace storage and UI | 16686 |
| Prometheus | Metrics scraping and storage | 9090 |

### Grafana Dashboards

Pre-built dashboards are in `deploy/grafana/`:

- **Platform Overview** — Message throughput, error rates, latency percentiles
- **Broker Health** — Consumer lag, partition distribution, throughput per topic
- **Workflow Metrics** — Active workflows, execution duration, failure rates
- **Connector Health** — Delivery success rate, latency, circuit breaker status

### Import Dashboards

```bash
# Copy dashboards to Grafana provisioning directory
cp deploy/grafana/*.json /var/lib/grafana/dashboards/
```

Or import via Grafana UI: Dashboards → Import → Upload JSON.

---

## 10. Verification

After installation, verify the platform is working correctly.

### Health Checks

```bash
# Gateway API
curl http://localhost:15100/health/ready
# Expected: {"status":"Healthy"}

# Admin API
curl http://localhost:15180/health/ready
# Expected: {"status":"Healthy"}
```

### Submit a Test Message

```bash
curl -X POST http://localhost:15100/api/gateway/submit \
  -H "Content-Type: application/json" \
  -d '{
    "messageType": "HealthCheck",
    "payload": { "test": true },
    "businessKey": "install-verify",
    "priority": "Normal",
    "intent": "Event"
  }'
```

### Run Tests

```bash
# Unit tests (1919 tests)
dotnet test tests/UnitTests

# Contract tests (57 tests)
dotnet test tests/ContractTests

# Tutorial labs (526 tests)
dotnet test tests/TutorialLabs

# Vue frontend tests (100 tests)
cd src/Admin.Web/clientapp && npx vitest run
```

### Open the Admin Dashboard

Navigate to the Admin.Web URL (shown in Aspire Dashboard or `http://localhost:15200`). Verify:

- [ ] Dashboard page loads with platform metrics
- [ ] Sidebar navigation works (19 pages)
- [ ] Dark/light theme toggle works
- [ ] Message Flow page shows recent messages

---

## 11. Upgrading

### Minor Version Upgrade

```bash
# Pull latest code
git pull origin main

# Restore and build
dotnet restore
dotnet build

# Restart Aspire (local dev)
cd src/AppHost && dotnet run

# Or rebuild containers (Docker/K8s)
docker compose build && docker compose up -d
```

### Major Version Upgrade

1. Read the release notes for breaking changes
2. Back up Cassandra data: `nodetool snapshot eip_platform`
3. Apply database migrations if any
4. Build and deploy new version
5. Verify health checks pass
6. Run integration tests against the new version

---

## 12. Uninstalling

### Local Development

```bash
# Stop Aspire (Ctrl+C in the terminal running dotnet run)

# Remove Docker containers and volumes
docker compose down -v

# Remove Docker images
docker image prune -f
```

### Kubernetes

```bash
# Remove platform services
kubectl delete -k deploy/kustomize/overlays/production
kubectl delete namespace eip

# Remove infrastructure
helm uninstall cassandra -n eip-infra
helm uninstall nats -n eip-infra
helm uninstall temporal -n eip-infra
kubectl delete namespace eip-infra
```

---

## Next Steps

| Guide | Description |
|-------|-------------|
| [Quick Start](quickstart.md) | 15-minute first message tutorial |
| [Admin UI Guide](admin-ui-guide.md) | Walkthrough of all 19 Admin Dashboard pages |
| [Platform Usage Guide](platform-usage-guide.md) | Configuration, connectors, routing, multi-tenancy |
| [Developer Setup](developer-setup.md) | IDE setup, project structure, technology stack |
| [Tutorial Course](../tutorials/README.md) | 50 hands-on tutorials with labs and exams |
| [Operations Runbook](operations-runbook.md) | Monitoring, alerting, troubleshooting, DR |
| [Onboarding Checklist](onboarding-checklist.md) | Structured checklist for new team members |
