# Cassandra Data Model

## Overview

Apache Cassandra serves as the primary data store for the Enterprise Integration Platform. The data model is designed for write-heavy workloads, time-series access patterns, and horizontal scalability. All tables use partition key strategies that distribute data evenly and support the platform's primary query patterns.

## Keyspace Design

### Production Keyspace

```cql
CREATE KEYSPACE eip_platform
WITH replication = {
    'class': 'NetworkTopologyStrategy',
    'dc1': 3,
    'dc2': 3
}
AND durable_writes = true;
```

### Development Keyspace

```cql
CREATE KEYSPACE eip_platform_dev
WITH replication = {
    'class': 'SimpleStrategy',
    'replication_factor': 1
};
```

### Keyspace Conventions

- **Production:** `NetworkTopologyStrategy` with RF=3 per datacenter for high availability.
- **Development:** `SimpleStrategy` with RF=1 for local development simplicity.
- **Multi-tenant:** Single keyspace with `tenant_id` in partition keys (not per-tenant keyspaces) to simplify operations.

## Table Schemas

### Messages Table

Stores message payloads and metadata for audit and replay.

```cql
CREATE TABLE eip_platform.messages (
    tenant_id       TEXT,
    message_date    DATE,
    envelope_id     UUID,
    correlation_id  UUID,
    causation_id    UUID,
    message_type    TEXT,
    content_type    TEXT,
    payload         BLOB,
    headers         MAP<TEXT, TEXT>,
    source_system   TEXT,
    status          TEXT,
    created_at      TIMESTAMP,
    updated_at      TIMESTAMP,
    payload_size    INT,
    PRIMARY KEY ((tenant_id, message_date), envelope_id)
) WITH CLUSTERING ORDER BY (envelope_id ASC)
  AND default_time_to_live = 7776000   -- 90 days
  AND compaction = {
    'class': 'TimeWindowCompactionStrategy',
    'compaction_window_size': 1,
    'compaction_window_unit': 'DAYS'
  };
```

**Partition Key:** `(tenant_id, message_date)` — Partitions by tenant and day, preventing unbounded partition growth and enabling time-based queries.

**Query Patterns:**
- Get all messages for a tenant on a specific date
- Get a specific message by tenant, date, and envelope ID
- Scan recent messages for a tenant (iterate over recent dates)

### Messages by Correlation ID

Supports lookups by correlation ID for tracing related messages.

```cql
CREATE TABLE eip_platform.messages_by_correlation (
    tenant_id       TEXT,
    correlation_id  UUID,
    envelope_id     UUID,
    message_type    TEXT,
    status          TEXT,
    created_at      TIMESTAMP,
    PRIMARY KEY ((tenant_id, correlation_id), created_at, envelope_id)
) WITH CLUSTERING ORDER BY (created_at ASC, envelope_id ASC)
  AND default_time_to_live = 7776000;
```

**Query Patterns:**
- Get all messages sharing a correlation ID (trace a processing chain)
- Ordered by creation time for chronological analysis

### Workflow State Table

Stores queryable workflow execution metadata (supplementing Temporal's internal state).

```cql
CREATE TABLE eip_platform.workflow_state (
    tenant_id       TEXT,
    workflow_date   DATE,
    workflow_id     TEXT,
    envelope_id     UUID,
    workflow_type   TEXT,
    status          TEXT,
    started_at      TIMESTAMP,
    completed_at    TIMESTAMP,
    duration_ms     BIGINT,
    activity_count  INT,
    error_message   TEXT,
    error_type      TEXT,
    PRIMARY KEY ((tenant_id, workflow_date), workflow_id)
) WITH CLUSTERING ORDER BY (workflow_id ASC)
  AND default_time_to_live = 7776000
  AND compaction = {
    'class': 'TimeWindowCompactionStrategy',
    'compaction_window_size': 1,
    'compaction_window_unit': 'DAYS'
  };
```

**Query Patterns:**
- List workflows for a tenant on a specific date
- Filter by status (completed, failed, running)
- Dashboard: workflow count and success rate by day

### Audit Log Table

Immutable record of all processing events for compliance and debugging.

```cql
CREATE TABLE eip_platform.audit_log (
    tenant_id       TEXT,
    audit_date      DATE,
    event_id        TIMEUUID,
    envelope_id     UUID,
    correlation_id  UUID,
    event_type      TEXT,
    actor           TEXT,
    details         TEXT,
    metadata        MAP<TEXT, TEXT>,
    created_at      TIMESTAMP,
    PRIMARY KEY ((tenant_id, audit_date), event_id)
) WITH CLUSTERING ORDER BY (event_id DESC)
  AND default_time_to_live = 31536000   -- 365 days
  AND compaction = {
    'class': 'TimeWindowCompactionStrategy',
    'compaction_window_size': 1,
    'compaction_window_unit': 'DAYS'
  };
```

**Query Patterns:**
- List audit events for a tenant on a specific date (most recent first)
- Filter by event type (message.received, workflow.completed, delivery.success)
- Compliance queries: all events for a specific envelope ID

### Deduplication Table

Ensures idempotent processing by tracking processed message IDs.

```cql
CREATE TABLE eip_platform.deduplication (
    tenant_id       TEXT,
    message_id      UUID,
    processed_at    TIMESTAMP,
    envelope_id     UUID,
    processor_id    TEXT,
    PRIMARY KEY ((tenant_id, message_id))
) WITH default_time_to_live = 604800    -- 7 days
  AND compaction = {
    'class': 'LeveledCompactionStrategy'
  };
```

**Query Patterns:**
- Check if a message ID has been processed (exact key lookup)
- TTL ensures automatic cleanup of old deduplication entries

### Claim Check Table

Stores large payloads referenced by claim check keys.

```cql
CREATE TABLE eip_platform.claim_check (
    tenant_id       TEXT,
    claim_key       UUID,
    payload         BLOB,
    content_type    TEXT,
    payload_size    INT,
    created_at      TIMESTAMP,
    PRIMARY KEY ((tenant_id, claim_key))
) WITH default_time_to_live = 2592000   -- 30 days
  AND compaction = {
    'class': 'SizeTieredCompactionStrategy'
  };
```

**Query Patterns:**
- Retrieve payload by tenant and claim key (exact key lookup)

### Connector State Table

Tracks connector health and circuit breaker state.

```cql
CREATE TABLE eip_platform.connector_state (
    tenant_id           TEXT,
    connector_id        TEXT,
    health_status       TEXT,
    circuit_state       TEXT,
    last_success_at     TIMESTAMP,
    last_failure_at     TIMESTAMP,
    consecutive_failures INT,
    updated_at          TIMESTAMP,
    PRIMARY KEY ((tenant_id, connector_id))
);
```

## Partition Key Strategies

### Time-Based Partitioning

Most tables use `(tenant_id, date)` compound partition keys to:

- **Prevent unbounded growth** — Each partition is bounded to one day's data per tenant.
- **Enable TTL cleanup** — Time-windowed compaction efficiently removes expired data.
- **Support range queries** — Applications iterate over date values to query time ranges.

### Estimated Partition Sizes

| Table              | Avg Record Size | Records/Day/Tenant | Partition Size |
|--------------------|-----------------|---------------------|----------------|
| messages           | 5 KB            | 100,000             | ~500 MB        |
| audit_log          | 500 bytes       | 500,000             | ~250 MB        |
| workflow_state     | 1 KB            | 50,000              | ~50 MB         |
| deduplication      | 100 bytes       | 100,000             | ~10 MB         |

All partitions stay well within Cassandra's recommended 100 MB–1 GB range.

## Query Patterns Summary

| Use Case                          | Table                       | Access Pattern              |
|-----------------------------------|-----------------------------|-----------------------------|
| View message details              | messages                    | Partition key + clustering  |
| Trace related messages            | messages_by_correlation     | Partition key + range scan  |
| List workflows by date            | workflow_state              | Partition key + range scan  |
| Check for duplicate               | deduplication               | Exact partition key lookup  |
| Retrieve large payload            | claim_check                 | Exact partition key lookup  |
| View audit trail                  | audit_log                   | Partition key + range scan  |
| Check connector health            | connector_state             | Exact partition key lookup  |

## Data Retention

| Table              | Default TTL  | Rationale                                        |
|--------------------|-------------|--------------------------------------------------|
| messages           | 90 days     | Sufficient for audit and replay                  |
| messages_by_correlation | 90 days | Aligned with messages table                     |
| workflow_state     | 90 days     | Aligned with messages table                      |
| audit_log          | 365 days    | Compliance requirement                           |
| deduplication      | 7 days      | Covers retry windows; short to minimize storage  |
| claim_check        | 30 days     | Large payloads; shorter retention                |
| connector_state    | None        | Current state only; small data volume            |

## Consistency Levels

| Operation            | Consistency Level | Rationale                                         |
|----------------------|-------------------|---------------------------------------------------|
| Dedup check (read)   | LOCAL_QUORUM      | Must see recent writes to avoid false negatives   |
| Dedup insert (write) | LOCAL_QUORUM      | Must be durable before processing proceeds        |
| Message write        | LOCAL_QUORUM      | Data durability required                          |
| Message read         | LOCAL_ONE         | Slightly stale reads acceptable for display       |
| Audit write          | LOCAL_QUORUM      | Compliance data must be durable                   |
| Audit read           | LOCAL_ONE         | Slightly stale reads acceptable for display       |
| Claim check write    | LOCAL_QUORUM      | Payload must be readable immediately after write  |
| Claim check read     | LOCAL_QUORUM      | Must return the most recent payload               |
