# Domain Model

## Overview

The Enterprise Integration Platform's domain model defines the core concepts and their relationships. These entities form the ubiquitous language used across the codebase, documentation, and team communication.

## Core Domain Concepts

### IntegrationEnvelope

The `IntegrationEnvelope` is the canonical message wrapper that flows through the entire platform. Every message, regardless of its origin or protocol, is normalized into an envelope before processing.

**Properties:**
- `EnvelopeId` (Guid) — Unique identifier for this envelope instance.
- `CorrelationId` (Guid) — Links related envelopes across a processing chain.
- `CausationId` (Guid) — References the envelope that caused this one to be created.
- `TenantId` (string) — Identifies the owning tenant for multi-tenant isolation.
- `MessageType` (string) — Logical type identifier (e.g., `order.created`, `invoice.received`).
- `ContentType` (string) — MIME type of the payload (e.g., `application/json`, `application/xml`).
- `Payload` (byte[]) — The raw message content.
- `Headers` (Dictionary<string, string>) — Extensible metadata key-value pairs.
- `CreatedAt` (DateTimeOffset) — Timestamp of envelope creation.
- `ExpiresAt` (DateTimeOffset?) — Optional TTL after which the message should not be processed.
- `SequenceNumber` (long?) — Ordering key for resequencing split messages.
- `TotalCount` (int?) — Expected total for aggregation scenarios.
- `ReplyTo` (string?) — Return address for request-reply patterns.
- `ClaimCheckRef` (string?) — Reference to externally stored payload (claim check pattern).

### Message

A `Message` represents the logical business content within an envelope. While the envelope handles routing and processing metadata, the message carries the business semantics.

**Properties:**
- `MessageId` (Guid) — Business-level unique identifier.
- `Source` (string) — Originating system identifier.
- `Subject` (string) — Human-readable description of the message content.
- `SchemaId` (string?) — Reference to a schema definition for validation.
- `Version` (int) — Schema version for evolution support.

### Workflow

A `Workflow` defines a reusable integration process — a sequence of activities that transform, route, and deliver messages.

**Properties:**
- `WorkflowId` (string) — Unique workflow definition identifier.
- `Name` (string) — Human-readable workflow name.
- `Description` (string) — Purpose and behavior documentation.
- `Version` (int) — Workflow definition version for safe deployments.
- `Activities` (List<ActivityDefinition>) — Ordered list of activities to execute.
- `RetryPolicy` (RetryPolicy) — Default retry configuration for activities.
- `TimeoutSeconds` (int) — Maximum workflow execution duration.
- `IsActive` (bool) — Whether the workflow accepts new executions.

### Activity

An `Activity` is a discrete unit of work within a workflow. Activities are stateless, independently testable, and composable.

**Properties:**
- `ActivityId` (string) — Unique activity type identifier.
- `ActivityType` (enum) — Category: Validate, Transform, Route, Deliver, Enrich, Custom.
- `Name` (string) — Human-readable activity name.
- `Configuration` (JsonDocument) — Activity-specific configuration parameters.
- `RetryPolicy` (RetryPolicy?) — Override retry policy for this activity.
- `TimeoutSeconds` (int) — Maximum activity execution duration.

### Connector

A `Connector` encapsulates the protocol-specific logic for communicating with an external system.

**Properties:**
- `ConnectorId` (string) — Unique connector identifier.
- `ConnectorType` (enum) — Protocol type: HTTP, SFTP, Email, File.
- `Name` (string) — Human-readable connector name.
- `Endpoint` (string) — Target system address (URL, host:port, path).
- `Authentication` (AuthConfig) — Credentials and authentication method.
- `RetryPolicy` (RetryPolicy) — Delivery retry configuration.
- `Headers` (Dictionary<string, string>) — Default headers/metadata for outbound messages.
- `IsEnabled` (bool) — Whether the connector is active.
- `HealthStatus` (enum) — Current health: Healthy, Degraded, Unhealthy, Unknown.

### Route

A `Route` defines a mapping from message characteristics to processing workflows and target connectors.

**Properties:**
- `RouteId` (string) — Unique route identifier.
- `Name` (string) — Human-readable route name.
- `Priority` (int) — Evaluation order (lower = higher priority).
- `Conditions` (List<RouteCondition>) — Predicates that must match for this route to apply.
- `WorkflowId` (string) — Target workflow to execute.
- `ConnectorIds` (List<string>) — Target connectors for delivery (recipient list).
- `IsActive` (bool) — Whether the route is evaluating incoming messages.

### Transformation

A `Transformation` defines a data mapping between source and target schemas.

**Properties:**
- `TransformId` (string) — Unique transformation identifier.
- `Name` (string) — Human-readable transformation name.
- `SourceSchemaId` (string) — Input schema reference.
- `TargetSchemaId` (string) — Output schema reference.
- `MappingDefinition` (JsonDocument) — Field mapping rules.
- `TransformType` (enum) — Mapping, Template, Script, AI-Generated.

### Rule

A `Rule` defines a business logic predicate used in routing, filtering, and validation.

**Properties:**
- `RuleId` (string) — Unique rule identifier.
- `Name` (string) — Human-readable rule name.
- `Expression` (string) — The rule expression (JSONPath, custom DSL, or C# expression).
- `RuleType` (enum) — Routing, Validation, Filter, Enrichment.
- `Parameters` (Dictionary<string, string>) — Configurable rule parameters.

### Tenant

A `Tenant` represents an organizational unit with isolated resources and configurations.

**Properties:**
- `TenantId` (string) — Unique tenant identifier.
- `Name` (string) — Organization name.
- `IsActive` (bool) — Whether the tenant is active.
- `Quotas` (TenantQuotas) — Rate limits, storage limits, and processing limits.
- `Configuration` (TenantConfig) — Tenant-specific configuration overrides.

## Domain Relationships

```
Tenant (1) ──────────── (*) Route
  │                          │
  │                          ├── (1) Workflow
  │                          │       │
  │                          │       └── (*) Activity
  │                          │              │
  │                          │              └── (0..1) Transformation
  │                          │
  │                          └── (*) Connector
  │
  └── (*) IntegrationEnvelope
              │
              └── (1) Message
```

- A **Tenant** owns multiple **Routes**, **Connectors**, and **IntegrationEnvelopes**.
- A **Route** references one **Workflow** and one or more **Connectors**.
- A **Workflow** contains an ordered list of **Activities**.
- An **Activity** may reference a **Transformation** for data mapping.
- An **IntegrationEnvelope** wraps one **Message** and carries processing metadata.
- **Rules** are used within **Routes** (as conditions) and **Activities** (as validation/filter logic).

## Value Objects

### RetryPolicy
- `MaxAttempts` (int) — Maximum number of retry attempts.
- `InitialIntervalMs` (int) — Initial delay between retries.
- `BackoffCoefficient` (double) — Multiplier for exponential backoff.
- `MaxIntervalMs` (int) — Maximum delay between retries.
- `NonRetryableErrors` (List<string>) — Error types that should not be retried.

### RouteCondition
- `Field` (string) — The envelope or message field to evaluate.
- `Operator` (enum) — Equals, Contains, StartsWith, Regex, GreaterThan, LessThan.
- `Value` (string) — The value to compare against.

### AuthConfig
- `AuthType` (enum) — None, Basic, Bearer, OAuth2, ApiKey, Certificate.
- `Credentials` (Dictionary<string, string>) — Authentication parameters (references to secrets, not plaintext).

### TenantQuotas
- `MaxMessagesPerMinute` (int) — Rate limit for message ingestion.
- `MaxStorageBytes` (long) — Storage quota for message payloads.
- `MaxConcurrentWorkflows` (int) — Maximum parallel workflow executions.
