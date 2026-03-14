# Temporal Configuration

## Overview

This document describes the Temporal.io configuration for the Enterprise Integration Platform, including namespace setup, task queue design, worker configuration, timeout policies, retry strategies, and search attributes.

## Namespace Setup

### Production Namespace

```
Namespace: eip-production
  ├── Retention Period: 30 days (workflow execution history)
  ├── Global Namespace: false (single region initially)
  ├── History Archival: enabled → S3/Blob storage
  └── Visibility Archival: enabled → S3/Blob storage
```

### Development Namespace

```
Namespace: eip-development
  ├── Retention Period: 3 days
  ├── History Archival: disabled
  └── Visibility Archival: disabled
```

### Namespace Per Environment

| Environment | Namespace           | Retention | Archival |
|-------------|---------------------|-----------|----------|
| Development | `eip-development`   | 3 days    | Disabled |
| Staging     | `eip-staging`       | 7 days    | Enabled  |
| Production  | `eip-production`    | 30 days   | Enabled  |
| DR          | `eip-dr`            | 30 days   | Enabled  |

### Namespace Registration

```bash
temporal operator namespace create \
  --namespace eip-production \
  --retention 30d \
  --history-archival-state enabled \
  --visibility-archival-state enabled \
  --description "Enterprise Integration Platform - Production"
```

## Task Queues

### Task Queue Design

Task queues separate different types of work, enabling independent scaling and isolation.

| Task Queue                     | Purpose                                    | Workers |
|--------------------------------|--------------------------------------------|---------|
| `eip-integration-workflows`   | Standard integration workflow execution    | 4–8     |
| `eip-batch-workflows`         | Batch processing (fan-out/fan-in)          | 2–4     |
| `eip-validation-activities`   | Schema and business rule validation        | 4–8     |
| `eip-transform-activities`    | Message transformation and mapping         | 4–8     |
| `eip-delivery-activities`     | Outbound connector delivery                | 8–16    |
| `eip-enrichment-activities`   | External data lookup and augmentation      | 4–8     |

### Task Queue Isolation

- **Workflow queues** are separate from **activity queues** to prevent head-of-line blocking.
- **CPU-bound activities** (transformation) have separate queues from **I/O-bound activities** (delivery).
- Per-tenant task queues can be created for strict tenant isolation when required.

## Worker Configuration

### Workflow Worker

```csharp
var workerOptions = new TemporalWorkerOptions("eip-integration-workflows")
{
    MaxConcurrentWorkflowTaskPolls = 10,
    MaxConcurrentWorkflowTasks = 100,
    MaxConcurrentActivityTaskPolls = 0,  // No activities on workflow workers
    StickyScheduleToStartTimeout = TimeSpan.FromSeconds(5),
    WorkflowCacheSize = 600
};
```

### Activity Worker

```csharp
var workerOptions = new TemporalWorkerOptions("eip-delivery-activities")
{
    MaxConcurrentActivityTaskPolls = 20,
    MaxConcurrentActivities = 100,
    MaxConcurrentLocalActivities = 50,
    MaxConcurrentWorkflowTaskPolls = 0,  // No workflows on activity workers
    MaxHeartbeatThrottleInterval = TimeSpan.FromSeconds(30),
    DefaultHeartbeatThrottleInterval = TimeSpan.FromSeconds(10)
};
```

### Worker Scaling Guidelines

| Metric                                    | Scale Trigger          | Action                    |
|-------------------------------------------|------------------------|---------------------------|
| Task queue schedule-to-start latency      | > 1 second sustained   | Add workers               |
| Worker CPU utilization                    | > 70% sustained        | Add workers               |
| Activity slots available                  | < 20% capacity         | Add workers               |
| Workflow task queue backlog               | > 100 pending          | Add workflow workers      |

## Workflow and Activity Timeouts

### Timeout Hierarchy

```
Workflow Execution Timeout (max lifetime of the workflow)
  └── Workflow Run Timeout (max single run, before continue-as-new)
        └── Workflow Task Timeout (max time for a single workflow task)

Activity Schedule-to-Close Timeout (max total time including retries)
  ├── Activity Schedule-to-Start Timeout (max wait time in queue)
  └── Activity Start-to-Close Timeout (max single execution time)
        └── Activity Heartbeat Timeout (max time between heartbeats)
```

### Default Timeout Configuration

| Timeout Type                    | Default Value | Notes                                    |
|---------------------------------|---------------|------------------------------------------|
| Workflow Execution Timeout      | 24 hours      | Maximum workflow lifetime                 |
| Workflow Run Timeout            | 1 hour        | Single run before continue-as-new        |
| Workflow Task Timeout           | 10 seconds    | Workflow decision processing time        |
| Activity Schedule-to-Close      | 10 minutes    | Total time including retries             |
| Activity Schedule-to-Start      | 2 minutes     | Maximum wait time in task queue          |
| Activity Start-to-Close        | 2 minutes     | Single activity execution                |
| Activity Heartbeat              | 30 seconds    | Long-running activity heartbeat interval |

### Per-Activity Timeout Overrides

| Activity Type       | Start-to-Close | Heartbeat  | Schedule-to-Close |
|---------------------|---------------|------------|-------------------|
| Validate            | 30 seconds    | None       | 2 minutes         |
| Transform           | 1 minute      | None       | 5 minutes         |
| Deliver (HTTP)      | 30 seconds    | None       | 5 minutes         |
| Deliver (SFTP)      | 5 minutes     | 30 seconds | 15 minutes        |
| Deliver (Email)     | 30 seconds    | None       | 5 minutes         |
| Enrich              | 30 seconds    | None       | 5 minutes         |

## Retry Policies

### Default Retry Policy

```csharp
var defaultRetryPolicy = new RetryPolicy
{
    InitialInterval = TimeSpan.FromSeconds(1),
    BackoffCoefficient = 2.0,
    MaximumAttempts = 5,
    MaximumInterval = TimeSpan.FromMinutes(1),
    NonRetryableErrorTypes = new[]
    {
        "ValidationException",
        "SchemaNotFoundException",
        "AuthenticationException",
        "AuthorizationException",
        "DeserializationException"
    }
};
```

### Per-Activity Retry Overrides

| Activity Type    | Max Attempts | Initial Interval | Max Interval | Backoff |
|------------------|-------------|-------------------|-------------|---------|
| Validate         | 1           | N/A               | N/A         | N/A     |
| Transform        | 2           | 1 second          | 10 seconds  | 2.0     |
| Deliver (HTTP)   | 5           | 1 second          | 60 seconds  | 2.0     |
| Deliver (SFTP)   | 3           | 5 seconds         | 60 seconds  | 2.0     |
| Deliver (Email)  | 3           | 5 seconds         | 60 seconds  | 2.0     |
| Enrich           | 3           | 1 second          | 30 seconds  | 2.0     |

## Search Attributes

Search attributes enable querying workflow executions from the Temporal UI and API.

### Custom Search Attributes

```bash
temporal operator search-attribute create \
  --namespace eip-production \
  --name TenantId --type Keyword \
  --name EnvelopeId --type Keyword \
  --name CorrelationId --type Keyword \
  --name MessageType --type Keyword \
  --name ConnectorType --type Keyword \
  --name WorkflowCategory --type Keyword \
  --name ProcessingStatus --type Keyword \
  --name ErrorType --type Keyword \
  --name MessageSize --type Int \
  --name ProcessingDurationMs --type Int
```

### Search Attribute Usage

Setting search attributes in workflow code:

```csharp
Workflow.UpsertTypedSearchAttributes(new SearchAttributeUpdate[]
{
    SearchAttribute.Create("TenantId", envelope.TenantId),
    SearchAttribute.Create("EnvelopeId", envelope.EnvelopeId.ToString()),
    SearchAttribute.Create("CorrelationId", envelope.CorrelationId.ToString()),
    SearchAttribute.Create("MessageType", envelope.MessageType),
    SearchAttribute.Create("ProcessingStatus", "Processing")
});
```

### Common Queries

```sql
-- Find all failed workflows for a tenant
TenantId = "acme" AND ProcessingStatus = "Failed"

-- Find workflows by message type in a time range
MessageType = "order.created" AND StartTime > "2025-01-01T00:00:00Z"

-- Find large message workflows
MessageSize > 1000000

-- Find workflows with specific error type
ErrorType = "TimeoutException" AND TenantId = "acme"
```

## Operational Procedures

### Workflow Termination

For stuck or runaway workflows:

```bash
temporal workflow terminate \
  --namespace eip-production \
  --workflow-id "wf-{envelope-id}" \
  --reason "Manual termination: workflow stuck in delivery retry loop"
```

### Workflow Signal

For human-in-the-loop approval:

```bash
temporal workflow signal \
  --namespace eip-production \
  --workflow-id "wf-{envelope-id}" \
  --name "ApprovalSignal" \
  --input '{"approved": true, "approvedBy": "admin@company.com"}'
```

### Workflow Reset

To replay a workflow from a specific point:

```bash
temporal workflow reset \
  --namespace eip-production \
  --workflow-id "wf-{envelope-id}" \
  --event-id 10 \
  --reason "Reset to re-process after connector fix"
```

## Infrastructure Requirements

### Temporal Server

| Component          | Instances | CPU    | Memory  | Storage              |
|--------------------|-----------|--------|---------|----------------------|
| Frontend           | 2+        | 2 cores| 4 GB    | None                 |
| History            | 3+        | 4 cores| 8 GB    | None                 |
| Matching           | 2+        | 2 cores| 4 GB    | None                 |
| Worker             | 2+        | 2 cores| 4 GB    | None                 |
| Persistence (DB)   | 3+ (HA)  | 4 cores| 16 GB   | SSD, 100+ GB per node|
| Visibility (ES)    | 3+ (HA)  | 4 cores| 16 GB   | SSD, 200+ GB per node|
