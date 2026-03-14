# ADR-001: Temporal.io Over Azure Durable Functions for Workflow Orchestration

## Status

**Accepted** — January 2025

## Context

The Enterprise Integration Platform requires a durable workflow orchestration engine to coordinate multi-step integration processes. Workflows must survive process restarts, support long-running operations (minutes to days), provide retry and compensation capabilities, and offer visibility into execution state.

Two candidates were evaluated:

1. **Azure Durable Functions** — Microsoft's serverless workflow framework built on Azure Functions and Azure Storage.
2. **Temporal.io** — An open-source, platform-agnostic durable execution framework with a dedicated server component.

### Evaluation Criteria

| Criterion                    | Weight | Description                                                |
|------------------------------|--------|------------------------------------------------------------|
| Cloud portability            | High   | Must run on any cloud or on-premises                       |
| Workflow complexity support  | High   | Must handle branching, saga, fan-out/fan-in, signals       |
| Operational visibility       | High   | Must provide workflow search, history, and debugging tools |
| .NET SDK maturity            | Medium | Must have a production-ready .NET SDK                      |
| Scalability model            | High   | Must scale horizontally for high throughput                |
| Vendor lock-in               | High   | Must avoid dependency on a single cloud provider           |
| Community and ecosystem      | Medium | Must have active community and integration ecosystem       |
| Testing support              | Medium | Must support unit and integration testing of workflows     |
| Long-running workflow support| High   | Must handle workflows running for days or weeks            |

## Decision

**We chose Temporal.io** as the workflow orchestration engine for the Enterprise Integration Platform.

## Rationale

### Cloud Portability

**Temporal:** Runs anywhere — cloud VMs, Kubernetes, bare metal, or as Temporal Cloud (managed SaaS). The same workflow code runs in all environments without modification.

**Durable Functions:** Tightly coupled to Azure infrastructure. Requires Azure Storage accounts (or the Netherite/MSSQL backend) and Azure Functions runtime. While the Azure Functions runtime can run in containers, the storage dependency remains Azure-specific.

**Winner: Temporal** — Critical requirement for our multi-cloud and on-premises deployment targets.

### Workflow Complexity

**Temporal:** Native support for complex patterns including saga compensation, signal-based human interaction, child workflows, continue-as-new for long histories, and side effects. Workflows are written as regular code (async/await) with full language expressiveness.

**Durable Functions:** Supports orchestration patterns through the Durable Task framework. Saga, fan-out/fan-in, and sub-orchestrations are supported. However, the programming model is more constrained — orchestrator functions must be deterministic and cannot call arbitrary code.

**Winner: Temporal** — More natural programming model with fewer constraints on workflow logic.

### Operational Visibility

**Temporal:** Provides a built-in Web UI for searching, viewing, and debugging workflow executions. Custom search attributes enable rich querying. Workflow history is fully inspectable, and individual workflow executions can be terminated, reset, or signaled from the UI or CLI.

**Durable Functions:** Azure Monitor and Application Insights provide some visibility. The Durable Functions Monitor extension offers a UI, but it's less mature. Querying across large numbers of orchestrations requires custom implementation.

**Winner: Temporal** — Significantly better operational tooling out of the box.

### .NET SDK

**Temporal:** The Temporalio .NET SDK is production-ready, supports .NET 6+, and provides idiomatic C# APIs with source generators. It follows Temporal's multi-language SDK consistency guarantees.

**Durable Functions:** The Durable Task SDK is mature and well-integrated with the Azure Functions ecosystem. It has years of production use in .NET environments.

**Winner: Durable Functions** — More mature .NET integration, though Temporal's SDK is catching up rapidly.

### Scalability

**Temporal:** Horizontally scalable at every layer. History, matching, and frontend services scale independently. Task queues can be sharded across many workers. Proven at massive scale (Uber, Netflix, Snap).

**Durable Functions:** Scales through Azure Functions' consumption or premium plans. Partition-based scaling with control queues. Performance depends heavily on the chosen storage backend.

**Winner: Temporal** — More granular scaling controls and proven at higher scale.

### Vendor Lock-In

**Temporal:** Open-source (MIT license). Self-hosted or Temporal Cloud (managed). No cloud provider dependency. Can migrate between deployment models freely.

**Durable Functions:** Dependent on Azure ecosystem. While the Durable Task framework is open-source, the full Azure Functions experience requires Azure infrastructure. Migrating away requires significant rearchitecture.

**Winner: Temporal** — Zero vendor lock-in; critical for our platform strategy.

### Testing Support

**Temporal:** Provides a test environment that runs workflows in-process without a Temporal server. Supports time skipping for testing timer-based logic. Activity mocking is straightforward.

**Durable Functions:** Testing requires the Durable Task test utilities. Integration testing needs the Azure Functions host or emulator. Time-based testing is less convenient.

**Winner: Temporal** — Better testing ergonomics and faster test execution.

## Consequences

### Positive

- **Cloud portability achieved** — Same platform code runs on AWS, Azure, GCP, and on-premises.
- **Rich operational visibility** — Temporal UI provides immediate insight into workflow state and history.
- **Natural programming model** — Workflows are written as regular async C# code with minimal constraints.
- **Independent scaling** — Workflow workers and activity workers scale independently based on workload.
- **No cloud vendor dependency** — Platform can be deployed anywhere without rearchitecture.

### Negative

- **Additional infrastructure** — Temporal server requires its own deployment and management (4+ services, persistence database, optional Elasticsearch for visibility).
- **Smaller .NET ecosystem** — Fewer .NET-specific examples and community content compared to Durable Functions.
- **Learning curve** — Team must learn Temporal concepts (task queues, workflows vs. activities, determinism rules).
- **Operational overhead** — Self-hosted Temporal requires monitoring, upgrades, and capacity planning. This can be mitigated by using Temporal Cloud.

### Risks and Mitigations

| Risk                              | Mitigation                                                   |
|-----------------------------------|--------------------------------------------------------------|
| Temporal .NET SDK immaturity      | Monitor SDK releases; contribute issues upstream; fallback to gRPC API if needed |
| Temporal server operational burden| Evaluate Temporal Cloud for managed hosting; invest in automation |
| Team learning curve               | Dedicate time for team training; start with simple workflows  |
| Temporal project discontinuation  | MIT license allows forking; large community (Uber, Netflix) reduces risk |

## Alternatives Considered

### Azure Durable Functions

Rejected due to cloud vendor lock-in and less portable deployment model. Would be reconsidered if the platform were Azure-only.

### MassTransit Sagas

MassTransit provides saga support with state machines. Rejected because it lacks the full workflow orchestration capabilities (timers, signals, long-running workflows, operational UI) that Temporal provides.

### Custom Workflow Engine

Building a custom workflow engine was rejected due to the enormous effort required to implement durability, replay, versioning, and operational tooling — all of which Temporal provides out of the box.

## References

- [Temporal.io Documentation](https://docs.temporal.io/)
- [Temporalio .NET SDK](https://github.com/temporalio/sdk-dotnet)
- [Azure Durable Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/durable/)
- [Temporal vs. Durable Functions Comparison](https://docs.temporal.io/evaluate/durable-functions-vs-temporal)
