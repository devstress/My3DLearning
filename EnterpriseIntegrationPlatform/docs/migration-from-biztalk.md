# Migration Guide: BizTalk Server to Enterprise Integration Platform

## Overview

This guide provides a structured approach for migrating integration solutions from Microsoft BizTalk Server to the Enterprise Integration Platform. It maps BizTalk concepts to their platform equivalents, outlines migration steps, and describes a coexistence strategy for incremental migration.

## Concept Mapping

| BizTalk Concept        | Platform Equivalent           | Notes                                                     |
|------------------------|-------------------------------|-----------------------------------------------------------|
| Orchestrations         | Temporal Workflows            | State machines → durable workflow definitions              |
| Receive Ports          | Kafka Consumers + Ingress API | Protocol adapters publish to Kafka topics                  |
| Receive Locations      | Ingress Adapter Configurations| Protocol-specific endpoint configuration                   |
| Send Ports             | Connectors                    | Outbound delivery with protocol-specific logic             |
| Send Port Groups       | Recipient Lists (Routes)      | Route to multiple connectors based on subscriptions        |
| Maps (XSLT/BizTalk Mapper) | Processing.Translator    | Transformation activities with configurable mappings       |
| Pipelines              | Activity Chains               | Ordered sequence of processing activities                  |
| Pipeline Components    | Individual Activities         | Discrete units: validate, decode, transform, encode        |
| MessageBox             | Configurable Message Broker    | Kafka for streaming, NATS/Pulsar for task delivery — pub/sub with topic/subject-based routing |
| Subscriptions          | Route Conditions              | Content-based routing rules replacing BizTalk subscriptions|
| Promoted Properties    | IntegrationEnvelope Headers   | Metadata extracted to envelope headers for routing         |
| Correlation Sets       | CorrelationId + Temporal State| Correlation managed by envelope IDs and workflow state     |
| BizTalk Admin Console  | Admin API + Dashboard         | Web-based administration and monitoring                    |
| BAM (Business Activity Monitoring) | OpenTelemetry + Dashboards | Distributed tracing and metrics                   |
| SSO (Enterprise SSO)   | Vault + Secret Management     | Centralized secret storage with rotation                   |
| BRE (Business Rules Engine) | Rule Evaluation Activities | Rules defined as configuration, evaluated in activities   |
| Host Instances         | Service Replicas + Workers    | Horizontally scaled, stateless service instances           |
| Adapters               | Connectors                    | Protocol-specific communication modules                    |

## Detailed Migration Mapping

### Orchestrations → Temporal Workflows

**BizTalk Orchestration:**
- Visual designer with shapes (Receive, Send, Transform, Decide, Parallel, Loop)
- Compiled to C# and deployed as assemblies
- State persisted in MessageBox (SQL Server)
- Dehydration/rehydration for long-running processes

**Platform Equivalent:**
- Temporal workflow definitions in C#
- Activities as async methods with retry policies
- State automatically persisted by Temporal
- Native support for long-running processes (days, weeks, months)

**Migration Steps:**
1. Document each orchestration's logic flow, shapes, and decisions.
2. Map each shape to a Temporal workflow construct (activity, saga, timer, signal).
3. Implement the workflow as a C# class inheriting from the platform's base workflow.
4. Port business logic from orchestration expressions to activity implementations.
5. Define retry policies and compensation logic for each activity.
6. Write integration tests that validate the same input/output behavior.

### Receive Ports → Kafka Consumers + Ingress Adapters

**BizTalk Receive Port:**
- Receive Location with adapter configuration (FILE, FTP, HTTP, SOAP, SQL)
- Receive Pipeline for decoding, disassembling, and validating
- Published to MessageBox with promoted properties

**Platform Equivalent:**
- Ingress adapter (HTTP, SFTP, Email, File) receives external messages
- Adapter normalizes into IntegrationEnvelope
- Published to the configured message broker

**Migration Steps:**
1. Inventory all receive locations with their adapters, URIs, and schedules.
2. Create corresponding ingress adapter configurations.
3. Map receive pipeline components to ingress validation activities.
4. Map promoted properties to envelope headers.
5. Configure message broker topics/subjects to match the routing topology.

### Send Ports → Connectors

**BizTalk Send Port:**
- Adapter configuration (FILE, FTP, HTTP, SOAP, SQL, SMTP)
- Send Pipeline for assembling and encoding
- Filters (subscriptions) determine which messages are sent
- Retry count and interval configuration

**Platform Equivalent:**
- Connector with protocol-specific configuration
- Delivery activity within a Temporal workflow
- Route conditions replace send port subscriptions
- Retry policies configured per connector and activity

### Maps → Processing.Transform

**BizTalk Maps:**
- Visual mapper with source/target schemas
- XSLT generated from visual mappings
- Functoids for complex transformations
- Tested with map unit tests

**Platform Equivalent:**
- Transformation activities with JSON-based mapping definitions
- Support for JSON↔XML, CSV, and custom formats
- Programmatic transformations for complex logic
- RAG-assisted mapping generation from schema pairs (use your own AI provider with platform context)

**Migration Steps:**
1. Export BizTalk map source and target schemas.
2. Convert XSD schemas to JSON Schema (where applicable).
3. Recreate mappings using the platform's transformation definitions.
4. Use the RAG knowledge API with your preferred AI provider to accelerate complex mapping creation.
5. Validate with test cases comparing BizTalk output to platform output.

## Migration Steps

### Phase 1: Assessment (2–4 weeks)

1. **Inventory BizTalk artifacts** — Document all applications, orchestrations, ports, maps, and pipelines.
2. **Classify integrations** — Categorize by complexity (simple pass-through, transformation, orchestration).
3. **Identify dependencies** — Map external system dependencies, schemas, and credentials.
4. **Prioritize** — Order integrations for migration based on business criticality and complexity.
5. **Estimate effort** — Use the complexity classification to estimate migration effort per integration.

### Phase 2: Foundation (4–6 weeks)

1. **Deploy platform infrastructure** — Set up Kafka, Temporal, Cassandra, and platform services.
2. **Configure tenants** — Create tenant configurations matching BizTalk application boundaries.
3. **Set up monitoring** — Deploy OpenTelemetry, dashboards, and alerting.
4. **Establish CI/CD** — Create build and deployment pipelines for the platform.
5. **Create shared schemas** — Port common schemas and canonical data models.

### Phase 3: Incremental Migration (ongoing)

For each integration, starting with the lowest complexity:

1. **Implement ingress** — Create adapter configuration for the receive location.
2. **Implement transformation** — Port the BizTalk map to a platform transformation.
3. **Implement workflow** — Create the Temporal workflow with validation, transformation, and routing activities.
4. **Implement connector** — Configure the outbound connector for the send port.
5. **Test** — Run parallel processing (BizTalk and platform) and compare outputs.
6. **Cut over** — Switch the source system to send to the platform instead of BizTalk.
7. **Decommission** — Disable the BizTalk integration after successful verification period.

### Phase 4: Decommission (2–4 weeks)

1. **Verify all integrations migrated** — Confirm no active message flow through BizTalk.
2. **Archive BizTalk artifacts** — Export and archive all BizTalk applications, bindings, and schemas.
3. **Decommission BizTalk** — Shut down BizTalk host instances and SQL databases.
4. **Update documentation** — Remove BizTalk references from operational runbooks.

## Coexistence Strategy

During migration, BizTalk and the platform run in parallel:

```
                    ┌──────────────────┐
                    │ Source Systems    │
                    └────────┬─────────┘
                             │
                    ┌────────┴─────────┐
                    │  Traffic Router  │
                    │  (by integration)│
                    └───┬──────────┬───┘
                        │          │
              ┌─────────▼──┐  ┌───▼──────────┐
              │  BizTalk   │  │  Enterprise  │
              │  Server    │  │  Integration │
              │ (legacy)   │  │  Platform    │
              └─────────┬──┘  └───┬──────────┘
                        │          │
                    ┌───▼──────────▼───┐
                    │  Target Systems  │
                    └──────────────────┘
```

### Coexistence Principles

- **Integration-level routing** — Route individual integrations to either BizTalk or the platform, never both simultaneously for the same integration.
- **Shared schemas** — Use identical schemas in both systems during parallel operation.
- **Parallel monitoring** — Monitor both systems with unified dashboards.
- **Rollback capability** — Maintain the ability to route an integration back to BizTalk if issues arise.
- **No big bang** — Never migrate all integrations at once; incremental migration reduces risk.

## Common Migration Challenges

| Challenge                          | Mitigation                                                 |
|------------------------------------|------------------------------------------------------------|
| Complex XSLT maps                  | Use RAG context with your AI provider; break into smaller transformations|
| Custom pipeline components         | Reimplement as activities; most logic maps directly        |
| BRE rules                          | Convert to platform rule definitions; test thoroughly      |
| BAM tracking profiles              | Map to OpenTelemetry spans and custom metrics              |
| Orchestration dehydration points   | Temporal handles this natively; no migration needed        |
| Send port groups with filters      | Convert to route conditions with recipient lists           |
| Convoy patterns                    | Implement as Temporal workflows with signal-based correlation|
| Direct-bound ports                 | Replace with explicit message broker topic/subject routing            |
