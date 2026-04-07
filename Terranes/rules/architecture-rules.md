# Architecture Rules

> All architecture decisions must satisfy the 11 Quality Pillars in `rules/quality-pillars.md`.

## General Principles

1. **Separation of Concerns** – Each project has a single responsibility
2. **Dependency Inversion** – Depend on abstractions, not implementations
3. **3D-First Design** – Every property, land block, and home design is represented as an immersive 3D model that users can explore, modify, and experience before committing
4. **Broker + Facilitator Model** – The platform connects clients with builders, landscapers, interior designers, furniture suppliers, and smart home providers. All legal contracts remain between client and each party directly
5. **Real-Time Quoting** – End-to-end indicative quotes from compliance, landscaping, construction, and furnishing are available before client commitment
6. **Site-Aware Placement** – 3D home models are test-fitted onto real land blocks using government land data for accurate block analysis and compliance checks
7. **Zero Data Loss** – Every submitted design, quote request, and transaction record is persisted. No silent drops
8. **Modular Integration Layer** – Land providers, real estate agents, solicitors, compliance engines, builders, and suppliers connect via standardised APIs

## Project Dependency Rules

- `Contracts` has ZERO project dependencies (pure DTOs and interfaces)
- `Models3D` depends only on `Contracts` (3D model loading, placement, and manipulation)
- `Land` depends only on `Contracts` (land data, block analysis, zoning, compliance)
- `Quoting` depends on `Contracts` (quote aggregation from builders, landscapers, furniture, smart home)
- `Marketplace` depends on `Contracts` (home listings, agent/owner self-service)
- `Platform.Api` depends on `Contracts` and service projects for orchestration
- Test projects may reference any src project

## Communication Patterns

- **Synchronous**: REST via Platform.Api for user-facing operations and partner integrations
- **Asynchronous**: Event-driven messaging for quote requests, partner notifications, and compliance checks
- **Real-Time**: WebSocket/SignalR for live 3D model collaboration and real-time quote updates
- **Storage**: Persistent storage for all designs, quotes, transactions, and user data

## Data Rules

- All entities use canonical data models defined in `Contracts`
- All records are immutable once finalised (audit trail)
- Every transaction has a correlation ID
- Every record has a timestamp (UTC)
- Data schemas are versioned

## Resilience Rules

- All external calls must have retry policies
- Circuit breakers on all partner API calls
- Dead letter handling for unprocessable requests
- Idempotent processing required
- Compensation logic for failed transactions

## Observability Rules

- All services emit structured logs
- Health checks on every service
- Metrics for throughput, latency, error rates
- End-to-end tracing for quote and design workflows

## Security Rules

- No secrets in source code
- Validate all input at service boundaries
- Authorise all API endpoints
- Tenant and user isolation for all data
- GDPR-compliant data handling
