# API Reference

> Complete endpoint reference for all three API services in the
> Enterprise Integration Platform: Admin.Api, Gateway.Api, and OpenClaw.Web.

---

## Table of Contents

1. [Admin.Api](#1-adminapi)
   - [Platform Status](#11-platform-status)
   - [Message Queries](#12-message-queries)
   - [Fault Queries](#13-fault-queries)
   - [Observability Events](#14-observability-events)
   - [Dead Letter Queue](#15-dead-letter-queue)
   - [Throttle Management](#16-throttle-management)
   - [Rate Limit Status](#17-rate-limit-status)
   - [Configuration](#18-configuration)
   - [Feature Flags](#19-feature-flags)
   - [Tenant Management](#110-tenant-management)
   - [Disaster Recovery](#111-disaster-recovery)
   - [Performance Profiling](#112-performance-profiling)
2. [Gateway.Api](#2-gatewayapi)
3. [OpenClaw.Web](#3-openclawweb)
   - [Message Inspection](#31-message-inspection)
   - [Health Checks](#32-health-checks)
   - [RAG & Code Generation](#33-rag--code-generation)

---

## Authentication

### Admin.Api

All Admin.Api endpoints require the `X-Api-Key` header:

```
X-Api-Key: <your-api-key>
```

Configure the API key via environment variable or appsettings:

```json
{
  "AdminApi": {
    "ApiKey": "<your-api-key>"
  }
}
```

**Rate Limiting**: Fixed window, 60 requests/minute per API key (configurable).

### Gateway.Api

The Gateway API handles authentication for downstream services and propagates correlation headers.

### OpenClaw.Web

OpenClaw.Web endpoints are unauthenticated (operator-facing internal tool).

---

## 1. Admin.Api

**Base URL**: `http://localhost:5180` (configurable via `AdminApi:BaseAddress`)

### 1.1 Platform Status

#### GET /api/admin/status

Returns the overall platform status.

**Response** `200 OK`:

```json
{
  "status": "healthy",
  "services": {
    "cassandra": "connected",
    "broker": "connected",
    "temporal": "connected"
  },
  "uptime": "02:15:30",
  "messageCount": 12450,
  "activeWorkflows": 23
}
```

---

### 1.2 Message Queries

#### GET /api/admin/messages/correlation/{correlationId}

Get messages by correlation ID.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `correlationId` | GUID | Path | Correlation identifier |

**Response** `200 OK`:

```json
{
  "correlationId": "a1b2c3d4-...",
  "messages": [
    {
      "messageId": "e5f6g7h8-...",
      "messageType": "OrderCreated",
      "status": "Delivered",
      "timestamp": "2026-04-04T10:15:30Z",
      "source": "Gateway",
      "stage": "Delivery"
    }
  ]
}
```

#### GET /api/admin/messages/{messageId}

Get a specific message by ID.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `messageId` | GUID | Path | Message identifier |

**Response** `200 OK`:

```json
{
  "messageId": "e5f6g7h8-...",
  "correlationId": "a1b2c3d4-...",
  "messageType": "OrderCreated",
  "payload": { },
  "metadata": { },
  "status": "Delivered",
  "timestamp": "2026-04-04T10:15:30Z"
}
```

#### PATCH /api/admin/messages/{messageId}/status

Update a message's delivery status.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `messageId` | GUID | Path | Message identifier |

**Request body**:

```json
{
  "status": "Delivered"
}
```

Valid statuses: `Pending`, `InFlight`, `Delivered`, `Failed`, `Retrying`, `DeadLettered`.

**Response** `200 OK`: Updated message record.

---

### 1.3 Fault Queries

#### GET /api/admin/faults/correlation/{correlationId}

Get fault (DLQ) entries by correlation ID.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `correlationId` | GUID | Path | Correlation identifier |

**Response** `200 OK`:

```json
{
  "correlationId": "a1b2c3d4-...",
  "faults": [
    {
      "faultId": "f1g2h3i4-...",
      "messageType": "OrderCreated",
      "errorType": "HttpRequestException",
      "errorMessage": "Connection refused",
      "failedAt": "2026-04-04T10:15:30Z",
      "retryCount": 3,
      "stage": "Delivery"
    }
  ]
}
```

---

### 1.4 Observability Events

#### GET /api/admin/events/correlation/{correlationId}

Get observability lifecycle events by correlation ID.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `correlationId` | GUID | Path | Correlation identifier |

**Response** `200 OK`:

```json
{
  "events": [
    {
      "messageId": "e5f6g7h8-...",
      "correlationId": "a1b2c3d4-...",
      "messageType": "OrderCreated",
      "source": "Gateway",
      "stage": "Ingestion",
      "status": "Pending",
      "businessKey": "order-123",
      "details": "Message received from gateway",
      "recordedAt": "2026-04-04T10:15:30Z"
    }
  ]
}
```

#### GET /api/admin/events/business/{businessKey}

Get observability events by business key (order number, shipment ID, etc.).

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `businessKey` | string | Path | Business key identifier |

**Response**: Same structure as correlation query.

---

### 1.5 Dead Letter Queue

#### POST /api/admin/dlq/resubmit

Resubmit messages from the dead letter queue.

**Request body**:

```json
{
  "correlationId": "a1b2c3d4-...",
  "messageType": "OrderCreated",
  "from": "2026-04-01T00:00:00Z",
  "to": "2026-04-04T23:59:59Z"
}
```

All fields are optional filters. If no filters are provided, all eligible DLQ messages are resubmitted.

**Response** `200 OK`:

```json
{
  "resubmitted": 5,
  "correlationIds": ["a1b2c3d4-...", "..."]
}
```

---

### 1.6 Throttle Management

Throttle policies control message processing throughput per tenant, queue, or endpoint. This is independent of HTTP rate limiting.

#### GET /api/admin/throttle/policies

List all throttle policies.

**Response** `200 OK`:

```json
[
  {
    "policyId": "high-volume-orders",
    "name": "High Volume Order Processing",
    "partition": {
      "tenantId": "tenant-A",
      "queue": "orders",
      "endpoint": "order-processor"
    },
    "maxMessagesPerSecond": 500,
    "burstCapacity": 1000,
    "maxWaitTime": "00:00:30",
    "isEnabled": true,
    "rejectOnBackpressure": false
  }
]
```

#### GET /api/admin/throttle/policies/{policyId}

Get a specific throttle policy.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `policyId` | string | Path | Policy identifier |

**Response** `200 OK`: Single policy object.

#### PUT /api/admin/throttle/policies

Create or update a throttle policy.

**Request body**:

```json
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

**Response** `200 OK`: Saved policy object.

#### DELETE /api/admin/throttle/policies/{policyId}

Delete a throttle policy.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `policyId` | string | Path | Policy identifier |

**Response** `204 No Content`.

---

### 1.7 Rate Limit Status

#### GET /api/admin/ratelimit/status

Get current HTTP rate limiting configuration and status.

**Response** `200 OK`:

```json
{
  "windowType": "FixedWindow",
  "windowSize": "00:01:00",
  "permitLimit": 60,
  "currentRequests": 23,
  "remainingPermits": 37
}
```

---

### 1.8 Configuration

#### GET /api/admin/config

List all configuration entries.

**Response** `200 OK`:

```json
[
  {
    "key": "Broker:Type",
    "value": "nats",
    "source": "appsettings"
  }
]
```

#### GET /api/admin/config/{key}

Get a specific configuration value.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `key` | string | Path | Configuration key (URL-encoded) |

**Response** `200 OK`:

```json
{
  "key": "Broker:Type",
  "value": "nats"
}
```

#### PUT /api/admin/config/{key}

Set a configuration value.

**Request body**:

```json
{
  "value": "kafka"
}
```

**Response** `200 OK`: Updated configuration entry.

#### DELETE /api/admin/config/{key}

Delete a configuration entry.

**Response** `204 No Content`.

---

### 1.9 Feature Flags

#### GET /api/admin/features

List all feature flags.

**Response** `200 OK`:

```json
[
  {
    "name": "enable-ai-analysis",
    "isEnabled": true,
    "rolloutPercentage": 100,
    "targetTenants": [],
    "variants": {}
  }
]
```

#### GET /api/admin/features/{name}

Get a specific feature flag.

**Response** `200 OK`: Single feature flag object.

#### PUT /api/admin/features/{name}

Create or update a feature flag.

**Request body**:

```json
{
  "isEnabled": true,
  "rolloutPercentage": 50,
  "targetTenants": ["tenant-A", "tenant-B"],
  "variants": {
    "control": { "weight": 50 },
    "treatment": { "weight": 50 }
  }
}
```

**Response** `200 OK`: Updated feature flag.

#### DELETE /api/admin/features/{name}

Delete a feature flag.

**Response** `204 No Content`.

---

### 1.10 Tenant Management

#### POST /api/admin/tenants/onboard

Provision a new tenant.

**Request body**:

```json
{
  "tenantId": "tenant-C",
  "name": "Contoso Ltd",
  "maxMessagesPerSecond": 1000,
  "maxConcurrentWorkflows": 50,
  "enabledConnectors": ["http", "sftp"]
}
```

**Response** `200 OK`:

```json
{
  "tenantId": "tenant-C",
  "status": "Provisioning",
  "provisionedAt": "2026-04-04T10:15:30Z"
}
```

#### DELETE /api/admin/tenants/{tenantId}

Deprovision a tenant.

**Response** `200 OK`: Deprovisioning confirmation.

#### GET /api/admin/tenants/{tenantId}/status

Get tenant onboarding status.

**Response** `200 OK`:

```json
{
  "tenantId": "tenant-C",
  "status": "Active",
  "provisionedAt": "2026-04-04T10:15:30Z",
  "brokerTopics": 5,
  "activeWorkflows": 3
}
```

#### GET /api/admin/tenants/{tenantId}/quota

Get tenant quota.

**Response** `200 OK`:

```json
{
  "tenantId": "tenant-C",
  "maxMessagesPerSecond": 1000,
  "currentMessagesPerSecond": 234,
  "maxConcurrentWorkflows": 50,
  "currentConcurrentWorkflows": 12
}
```

#### PUT /api/admin/tenants/{tenantId}/quota

Update tenant quota.

**Request body**:

```json
{
  "maxMessagesPerSecond": 2000,
  "maxConcurrentWorkflows": 100
}
```

**Response** `200 OK`: Updated quota.

---

### 1.11 Disaster Recovery

#### GET /api/admin/dr/regions

List all registered DR regions.

**Response** `200 OK`:

```json
[
  {
    "regionId": "us-east-1",
    "name": "US East (Virginia)",
    "status": "Active",
    "isPrimary": true
  }
]
```

#### POST /api/admin/dr/regions

Register a new DR region.

**Request body**:

```json
{
  "regionId": "eu-west-1",
  "name": "EU West (Ireland)",
  "connectionString": "..."
}
```

**Response** `200 OK`: Registered region.

#### POST /api/admin/dr/failover/{targetRegionId}

Initiate failover to a target region.

**Response** `200 OK`:

```json
{
  "failoverId": "fo-123",
  "targetRegion": "eu-west-1",
  "status": "InProgress",
  "initiatedAt": "2026-04-04T10:15:30Z"
}
```

#### POST /api/admin/dr/failback/{regionId}

Failback to a region after failover.

**Response** `200 OK`: Failback status.

#### GET /api/admin/dr/replication

Get cross-region replication status.

**Response** `200 OK`:

```json
{
  "primaryRegion": "us-east-1",
  "replicas": [
    {
      "regionId": "eu-west-1",
      "lagSeconds": 2,
      "status": "InSync"
    }
  ]
}
```

#### GET /api/admin/dr/objectives

Get recovery objectives (RTO/RPO targets).

**Response** `200 OK`:

```json
[
  {
    "objectiveId": "tier-1",
    "rtoSeconds": 300,
    "rpoSeconds": 60,
    "description": "Critical integrations"
  }
]
```

#### POST /api/admin/dr/objectives

Register a recovery objective.

**Request body**:

```json
{
  "objectiveId": "tier-2",
  "rtoSeconds": 900,
  "rpoSeconds": 300,
  "description": "Standard integrations"
}
```

**Response** `200 OK`: Registered objective.

#### POST /api/admin/dr/drills

Run a DR drill scenario.

**Request body**:

```json
{
  "scenarioId": "cassandra-failover",
  "targetRegion": "eu-west-1"
}
```

**Response** `200 OK`:

```json
{
  "drillId": "drill-456",
  "scenarioId": "cassandra-failover",
  "status": "Completed",
  "durationSeconds": 45,
  "result": "Pass"
}
```

#### GET /api/admin/dr/drills/history

Get DR drill execution history.

**Response** `200 OK`:

```json
[
  {
    "drillId": "drill-456",
    "scenarioId": "cassandra-failover",
    "executedAt": "2026-04-04T10:15:30Z",
    "durationSeconds": 45,
    "result": "Pass"
  }
]
```

---

### 1.12 Performance Profiling

#### POST /api/admin/profiling/snapshot

Capture a performance snapshot.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `label` | string | Query (optional) | Label for the snapshot |

**Response** `200 OK`:

```json
{
  "snapshotId": "snap-789",
  "label": "baseline",
  "capturedAt": "2026-04-04T10:15:30Z",
  "heapSizeBytes": 52428800,
  "gen0Collections": 15,
  "gen1Collections": 3,
  "gen2Collections": 1,
  "threadCount": 24,
  "cpuUsagePercent": 12.5
}
```

#### GET /api/admin/profiling/snapshot/latest

Get the most recent performance snapshot.

**Response** `200 OK`: Snapshot object.

#### GET /api/admin/profiling/snapshots

Get snapshots in a time range.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `from` | DateTime | Query | Start of time range (ISO 8601) |
| `to` | DateTime | Query | End of time range (ISO 8601) |

**Response** `200 OK`: Array of snapshot objects.

#### GET /api/admin/profiling/hotspots

Detect performance hotspots.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `cpuThreshold` | double | Query (optional) | CPU threshold percentage |
| `memoryThreshold` | long | Query (optional) | Memory threshold in bytes |

**Response** `200 OK`:

```json
{
  "hotspots": [
    {
      "component": "Processing.Transform",
      "metric": "cpuUsagePercent",
      "value": 85.2,
      "threshold": 80.0,
      "severity": "Warning"
    }
  ]
}
```

#### GET /api/admin/profiling/operations

Get operation statistics.

**Response** `200 OK`:

```json
{
  "operations": [
    {
      "name": "TransformActivity",
      "count": 15420,
      "avgDurationMs": 12.5,
      "p99DurationMs": 45.2,
      "errorRate": 0.01
    }
  ]
}
```

#### GET /api/admin/profiling/gc

Get garbage collection snapshot.

**Response** `200 OK`:

```json
{
  "gen0Collections": 150,
  "gen1Collections": 30,
  "gen2Collections": 5,
  "heapSizeBytes": 52428800,
  "fragmentationPercent": 3.2,
  "pauseTimeMs": 2.1
}
```

#### GET /api/admin/profiling/gc/recommendations

Get GC tuning recommendations.

**Response** `200 OK`:

```json
{
  "recommendations": [
    {
      "category": "Gen2Frequency",
      "severity": "Info",
      "message": "Gen2 collections are within normal range."
    }
  ]
}
```

#### GET /api/admin/profiling/benchmarks

Get benchmark baselines.

**Response** `200 OK`:

```json
{
  "baselines": [
    {
      "name": "message-throughput",
      "value": 5000,
      "unit": "msg/sec",
      "capturedAt": "2026-04-04T10:15:30Z"
    }
  ]
}
```

---

## 2. Gateway.Api

**Base URL**: Configured via Aspire (typically `http://localhost:15100` range)

### GET /

Gateway metadata and service information.

**Response** `200 OK`:

```json
{
  "service": "EnterpriseIntegrationPlatform.Gateway",
  "version": "1.0.0",
  "healthDocs": "/gateway/health"
}
```

### ALL /api/v{version}/{**rest}

Versioned route resolver and proxy to downstream services.

Supports all HTTP methods (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`). The gateway resolves the downstream service URL from the route configuration, forwards the request with preserved headers and query strings, and returns the downstream response.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `version` | string | Path | API version |
| `rest` | string | Path | Remaining path to forward |

**Headers propagated**:
- All original request headers
- `X-Correlation-Id` — Added if not present
- `X-Request-Id` — Added for tracking

### GET /gateway/health

Aggregated health status for all services.

**Response** `200 OK`:

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "cassandra",
      "status": "Healthy"
    },
    {
      "name": "ollama",
      "status": "Degraded",
      "description": "Ollama is not reachable."
    }
  ]
}
```

---

## 3. OpenClaw.Web

**Base URL**: Configured via Aspire (typically `http://localhost:15300` range)

### 3.1 Message Inspection

#### GET /api/inspect/business/{businessKey}

Inspect message lifecycle by business key.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `businessKey` | string | Path | Business key (order number, shipment ID, etc.) |

**Response** `200 OK` — Message found:

```json
{
  "query": "order-02",
  "found": true,
  "summary": "Message was received at the gateway, routed to the shipment pipeline, transformed, and delivered successfully in 150.3ms.",
  "ollamaAvailable": true,
  "events": [
    {
      "messageId": "e5f6g7h8-...",
      "correlationId": "a1b2c3d4-...",
      "messageType": "OrderShipment",
      "source": "Gateway",
      "stage": "Ingestion",
      "status": "Pending",
      "businessKey": "order-02",
      "details": "Message received from gateway",
      "recordedAt": "2026-04-04T10:05:00Z"
    },
    {
      "messageId": "e5f6g7h8-...",
      "correlationId": "a1b2c3d4-...",
      "messageType": "OrderShipment",
      "source": "Delivery",
      "stage": "Delivery",
      "status": "Delivered",
      "businessKey": "order-02",
      "details": "Delivered successfully in 150.3ms",
      "recordedAt": "2026-04-04T10:05:08Z"
    }
  ],
  "latestStage": "Delivery",
  "latestStatus": "Delivered"
}
```

**Response** `200 OK` — Message not found:

```json
{
  "query": "nonexistent-order",
  "found": false,
  "summary": "No messages found for business key 'nonexistent-order'.",
  "events": []
}
```

**Response** `200 OK` — Ollama unavailable:

```json
{
  "query": "order-02",
  "found": true,
  "summary": "⚠️ Ollama is unavailable. Trace analysis cannot be performed at this time.",
  "ollamaAvailable": false,
  "events": [ ... ],
  "latestStage": "Delivery",
  "latestStatus": "Delivered"
}
```

#### GET /api/inspect/correlation/{correlationId}

Inspect message lifecycle by correlation ID.

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `correlationId` | GUID | Path | Correlation identifier |

**Response**: Same structure as business key inspection.

#### POST /api/inspect/ask

Query message state with a custom query string.

**Request body**:

```json
{
  "query": "order-02"
}
```

**Response**: Same structure as business key inspection.

---

### 3.2 Health Checks

#### GET /api/health/ollama

Check Ollama AI service connectivity.

**Response** `200 OK`:

```json
{
  "available": false,
  "service": "ollama"
}
```

#### GET /api/health/ragflow

Check RagFlow knowledge base service connectivity.

**Response** `200 OK`:

```json
{
  "available": true,
  "service": "ragflow"
}
```

#### GET /api/health/seeder

Check demo data seeding status.

**Response** `200 OK`:

```json
{
  "seeded": true
}
```

#### GET /metrics

Prometheus-format metrics endpoint.

**Response** `200 OK`:

```
# HELP eip_messages_processed_total Total messages processed
# TYPE eip_messages_processed_total counter
eip_messages_processed_total 12450
```

---

### 3.3 RAG & Code Generation

These endpoints retrieve context from the platform's RagFlow knowledge base. Developers use their own AI provider (Copilot, Codex, Claude Code) to generate code using this context.

#### POST /api/generate/integration

Retrieve RAG context for integration code generation.

**Request body**:

```json
{
  "description": "Generate an integration that maps OrderCreated XML to ShipmentRequest JSON, authenticates via OAuth2, and submits to the shipping API"
}
```

**Response** `200 OK`:

```json
{
  "retrievedContext": "The platform uses IntegrationEnvelope<T> as the canonical message wrapper...",
  "contextFound": true
}
```

#### POST /api/generate/connector

Retrieve context for connector code generation.

**Request body**:

```json
{
  "connectorType": "http",
  "targetDescription": "Acme REST API v2 for order submission",
  "authenticationType": "OAuth2",
  "relatedPatterns": ["Content-Based Router", "Message Translator"]
}
```

**Response** `200 OK`:

```json
{
  "connectorType": "http",
  "retrievedContext": "HTTP connectors implement IHttpConnector with token caching...",
  "contextFound": true
}
```

#### POST /api/generate/schema

Retrieve context for message schema generation.

**Request body**:

```json
{
  "messageType": "OrderCreated",
  "format": "json",
  "examplePayload": "{\"orderId\": \"123\", \"items\": [...]}"
}
```

**Response** `200 OK`:

```json
{
  "messageType": "OrderCreated",
  "retrievedContext": "Message schemas follow the IntegrationEnvelope<T> pattern...",
  "contextFound": true
}
```

#### POST /api/generate/chat

Multi-turn chat with RAG-enhanced completion.

**Request body**:

```json
{
  "question": "How do I implement a content-based router for my integration?",
  "conversationId": null
}
```

**Response** `200 OK`:

```json
{
  "answer": "Content-based routing is implemented via the ContentBasedRouter class in Processing.Routing...",
  "conversationId": "conv-abc123",
  "referenceCount": 3
}
```

Use the returned `conversationId` for follow-up questions in the same conversation.

#### GET /api/generate/datasets

List available RAG knowledge datasets.

**Response** `200 OK`:

```json
[
  {
    "datasetId": "eip-patterns",
    "name": "Enterprise Integration Patterns",
    "documentCount": 65
  },
  {
    "datasetId": "platform-architecture",
    "name": "Platform Architecture",
    "documentCount": 11
  }
]
```

---

## Admin.Web Proxy Endpoints

The Admin.Web Vue 3 frontend proxies all API requests through local endpoints to Admin.Api. This avoids exposing the API key to the browser.

| Admin.Web Proxy | Proxied To (Admin.Api) |
|-----------------|------------------------|
| `GET /api/admin/status` | `GET /api/admin/status` |
| `GET /api/admin/messages/{id}` | `GET /api/admin/messages/{id}` |
| `GET /api/admin/messages/correlation/{id}` | `GET /api/admin/messages/correlation/{id}` |
| `GET /api/admin/faults/correlation/{id}` | `GET /api/admin/faults/correlation/{id}` |
| `POST /api/admin/dlq/resubmit` | `POST /api/admin/dlq/resubmit` |
| `GET /api/admin/throttle/policies` | `GET /api/admin/throttle/policies` |
| `GET /api/admin/throttle/policies/{id}` | `GET /api/admin/throttle/policies/{id}` |
| `PUT /api/admin/throttle/policies` | `PUT /api/admin/throttle/policies` |
| `DELETE /api/admin/throttle/policies/{id}` | `DELETE /api/admin/throttle/policies/{id}` |
| `GET /api/admin/ratelimit/status` | `GET /api/admin/ratelimit/status` |
| `GET /api/admin/dr/regions` | `GET /api/admin/dr/regions` |
| `POST /api/admin/dr/drills` | `POST /api/admin/dr/drills` |
| `GET /api/admin/dr/drills/history` | `GET /api/admin/dr/drills/history` |
| `POST /api/admin/profiling/snapshot` | `POST /api/admin/profiling/snapshot` |
| `GET /api/admin/profiling/snapshot/latest` | `GET /api/admin/profiling/snapshot/latest` |
| `GET /api/admin/profiling/gc` | `GET /api/admin/profiling/gc` |
| `GET /api/admin/events/business/{key}` | `GET /api/admin/events/business/{key}` |

The proxy handles `HttpRequestException` gracefully — when Admin.Api is unavailable, endpoints return appropriate fallback responses (e.g., empty arrays for list endpoints).
