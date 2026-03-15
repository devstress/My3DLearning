# Quality Pillars — Architectural Quality Attributes

> These 11 pillars are the **highest-level** guiding principles for the Enterprise Integration Platform.
> Every chunk, every design decision, and every line of code must satisfy the relevant pillars.
> Pillars are delivered incrementally across chunks — each chunk must advance at least one pillar.

## Design Philosophy

This framework should focus on **few lines of code** — an operator writes a minimal specification and asks AI to auto-generate a complete, production-ready integration.

The platform includes a **self-hosted GraphRAG** system (RagFlow + Ollama) as part of the Aspire project. The repository's own docs, rules, and source code are indexed as the knowledge base. Ollama provides embeddings and retrieval within RagFlow. Developers on any client machine use their own preferred AI provider (Copilot, Codex, Claude Code) connecting to this self-hosted RAG system — the platform retrieves relevant context, and the developer's AI provider generates the code. All data stays on-premises; no cloud AI dependency.

### Example Prompt

```
Generate for me a new integration:
1/ Map a message (XML/JSON or flat file) to another format
2/ Obtain authentication token from web API with expiry — token must be stored and reused
3/ Submit this message to another web API with the token
```

### Notification Loopback — Ack/Nack

Every integration must implement atomic notification loopback:

- **Ack (Acknowledge):** If all steps succeed, return an Ack to the sender.
- **Nack (Negative Acknowledge):** If any step fails, return a Nack to the sender.
- **All or nothing:** Atomic semantics — partial success is treated as failure.
- **Ack/Nack queues:** The platform publishes Ack/Nack messages to dedicated queues.
- **Subscription/routing:** Downstream systems can subscribe to Ack/Nack queues to trigger rollback, retry, or send notifications back to the original sender if enabled.

### Resilience Contract

**Zero message loss** even after restart or outage of full or partial system offline. Every accepted message is either delivered to its destination or routed to a dead letter queue for human review. No silent drops.

---

## The 11 Pillars

### Pillar 1 — Reliability

**The system must continue to work correctly at all times.**

| Concern | Approach |
|---------|----------|
| Delivery guarantees | Support both "at-least-once" and "exactly-once" (via idempotency + dedup). Extend the zero-data-loss concept across all boundaries. |
| Redundancy strategy | Replicate every stateful component (Kafka RF=3, Cassandra RF=3, Temporal multi-node, NATS JetStream Raft). If the data store is not durable (e.g., Redis AOF can fail from disk disruption), use write-ahead logging or dual-write to a durable store. |
| Data integrity | Atomic operations via Outbox Pattern or Saga orchestration (Temporal). Protect the source of truth — never rely on a single non-durable cache as the only copy. |
| Unintended behaviour | Guard against infinite loops, validation bypass, and data corruption. Every retry must be bounded. Every transformation must validate output. |
| Rebalancing | Consistent hashing for partition assignment. Graceful rebalancing on node join/leave without message loss. |
| ACID properties | Atomicity (all-or-nothing via Saga), Consistency (schema validation at every boundary), Isolation (tenant-level and message-level), Durability (persist before acknowledge). |

**Chunked delivery:** Reliability foundations are in chunks 004–007 (contracts, broker, Temporal, Cassandra). Production hardening in chunks 016–017 (DLQ, retry). Advanced exactly-once in Phase 4.

---

### Pillar 2 — Security

**Protect data, access, and operations at every layer.**

| Concern | Approach |
|---------|----------|
| Authentication | OAuth 2.0, JWT, integrate with identity providers (Okta, Microsoft Entra ID). Every API endpoint requires authentication. |
| Authorization | RBAC (Role-Based Access Control) for standard use; ABAC (Attribute-Based Access Control) for multi-tenant scale. Fine-grained permissions per tenant, resource type, and action. |
| Data protection | Encrypt data in transit (TLS 1.2+ everywhere) and at rest (Cassandra TDE, Kafka disk encryption, Temporal payload encryption with AES-256-GCM). |
| Input validation & sanitization | Validate and sanitize all input at service boundaries. Prevent injection, malformed data, oversized payloads. Schema validation (JSON Schema, XSD). |
| Audit logging | Track all sensitive actions and configuration changes in an immutable, write-once audit log. Retention: 1 year default, 7 years for compliance. |
| Rate limiting & abuse protection | Per-tenant, per-endpoint rate limiting. Throttle excessive or malicious usage (429 Too Many Requests). |
| Multi-tenant isolation | Prevent cross-tenant data leaks or interference. Partition keys include tenant ID. Kafka topic ACLs per tenant. Temporal namespace separation. |
| Secrets management | Secure storage and access to API keys, credentials, and tokens. No secrets in code. Use Azure Key Vault / HashiCorp Vault / AWS Secrets Manager in production. Auto-rotation on configurable schedule. |

**Chunked delivery:** Security foundations in chunk 023. Multi-tenancy in chunk 024. Auth integration can be added as a dedicated chunk.

---

### Pillar 3 — Scalability

**Build fast, but don't block future growth.**

| Concern | Approach |
|---------|----------|
| Scale dimensions | Isolation (number of users/tenants), partitioning (volume of data), TPS/TPD, data size — plan for 1/3/5/10 year horizons. |
| Storage scaling | Cassandra (NoSQL) for operational data. Consider Spark SQL vs Hadoop for ETL workloads. Partitioning/splitting strategies for any RDB components. |
| Rate limiting per component | Each component/phase has independent rate limits. Backpressure propagates upstream without cascading failure. |
| Design flexibility | Loosely coupled components, easy to scale independently. Broker abstraction allows swapping NATS → Pulsar without code changes. |
| Data growth | Identify bottlenecks per component. Time-windowed partitions in Cassandra. TTL-based cleanup. Claim check pattern for large payloads. |
| Load balancing | Round Robin, Weighted Round Robin, Least Connections, Least Response Time / Latency-based routing. |
| Sharding | Hash Sharding (modulo), Consistent Hashing, Weighted Consistent Hashing. |
| Autoscaling | CPU-based, RPS/QPS-based, Queue-length-based, SLO/Latency-based autoscale. |
| Geo-routing | Multi-region Active-Active support. Geo-routing for latency-sensitive workloads. |
| Worker scaling | Worker Pool scaling, Adaptive concurrency limits per activity type. |

**Chunked delivery:** Scalability foundations in chunks 005 (broker), 007 (Cassandra). Advanced scaling in Phase 4 (load testing chunk 026).

---

### Pillar 4 — Maintainability

**Over time, many people will work on this system. They must all be able to work productively.**

| Concern | Approach |
|---------|----------|
| High quality code | All code must be production-ready. No pretend, demo, hacky, or conceptual code. See `reality-filter.md` and `coding-standards.md`. |
| Consistent style & rules | Enforce via CheckStyle equivalents (Roslyn analyzers, .editorconfig), static analysis (SonarQube, CodeQL), and CI gates. Zero warnings policy. |
| Lead with content | Documentation-first approach. ADRs for significant decisions. Docs updated with every chunk. |
| Visible architecture | Architecture is explicit in project structure, dependency rules, and documentation. Any developer can understand the system by reading `rules/` and `docs/`. |
| Modifiability & extensibility | Dependency inversion, abstraction-based design. Adding a new connector or broker provider requires implementing one interface — no core changes. |
| Backward compatibility | Contract versioning. Schema evolution with backward-compatible changes. API versioning. |
| Analyzability & debuggability | OpenTelemetry tracing across all boundaries. Structured logging with correlation IDs. OpenClaw AI-powered "where is my message?" diagnostics. |
| CI/CD | GitHub Actions pipeline. Automated build, test, static analysis on every push/PR. Zero-warning enforcement. |
| Ownership | Clear project ownership. Repository history with conventional commits. Automated commit descriptions. |
| Development lifecycle | Chunk-based incremental delivery. Each chunk is self-contained, testable, and resumable by any developer or AI agent. |

**Chunked delivery:** Maintainability is enforced from chunk 001 (scaffold) through all chunks via rules and CI.

---

### Pillar 5 — Availability

**Design for high availability: 99% → 99.9% → 99.99% → 99.999% uptime.**

| Concern | Approach |
|---------|----------|
| Uptime targets | Start at 99.9% (8.7h downtime/year). Progress toward 99.99% (52min/year) and 99.999% (5min/year) with infrastructure hardening. |
| Smooth deployments | Blue-green and canary deployments. Zero-downtime upgrades, downgrades, and major version transitions. |
| Failover | Automatic failover for all stateful components. Leader election (Temporal, NATS Raft). Cassandra multi-DC replication. |
| Auto-scaling | Kubernetes HPA with custom metrics (queue depth, CPU, latency). Scale to zero for dev, scale to N for production. |
| Multi-cluster / geo-redundancy | Active-Active or Active-Passive regional setups. Cassandra NetworkTopologyStrategy for multi-DC. |
| Kubernetes patterns | Blue-green or canary deployments. Self-healing pods for fault isolation. Liveness, readiness, startup probes. |
| Disaster recovery | Automated DR mechanisms. Tested failover procedures. Regular DR drills. |

**Chunked delivery:** Availability foundations in chunks 003 (Aspire), 009 (observability/health). Advanced HA in Phase 4.

---

### Pillar 6 — Resilience

**Build fault-tolerant systems that continue operating under failure.**

| Concern | Approach |
|---------|----------|
| State persistence | All state is persisted before acknowledgement. No in-memory-only state that can be lost on crash. |
| Retry logic | Exponential backoff with jitter. Bounded retries. Non-retryable error classification. Polly for .NET resilience. |
| Redundancy levels | Hardware, software, data, network, geography. N+1, 2N, 2N+1 redundancy depending on criticality. |
| Disaster recovery | RTO (Recovery Time Objective): < 15 minutes. RPO (Recovery Point Objective): 0 messages lost. |
| Regional strategy | Active-Active (both regions serve traffic) or Active-Passive (standby region for failover). Favor duplication over data loss. |
| Circuit breakers | Prevent cascading failures. Per-connector circuit breakers with configurable thresholds. |
| Bulkhead isolation | Process, thread pool, consumer group, task queue, and tenant-level isolation. |
| Backpressure | Kafka consumer pause, Temporal rate limiting, ingress rate limiting, bounded connection pools. |

**Chunked delivery:** Resilience foundations in chunks 005–006 (broker, Temporal). Pattern implementations in chunks 016–017 (DLQ, retry). Advanced resilience in Phase 4 (chunk 025 saga compensation).

---

### Pillar 7 — Supportability

**Systems must be diagnosable and controllable through standardized tooling.**

| Concern | Approach |
|---------|----------|
| Observability tooling | Logging (Loki), metrics (Prometheus), traces (OpenTelemetry). Standardized across all services. |
| Monitoring | Health checks, alerts (PagerDuty/OpsGenie integration), visualization (Grafana dashboards). |
| Internal tools | OpenClaw web UI for message tracing and AI-powered diagnostics. Admin API for configuration management. |
| Incident response | Clear processes, runbooks (`docs/operations-runbook.md`), escalation procedures, and shared knowledge. |
| DLQ observability | Kafka/NATS/Pulsar DLQ tools with standardized resubmission mechanisms. Reduce operational complexity and learning curves. |
| Automation rule | **If it takes more than an hour and you'll need to do it more than once, automate it.** |
| Training & documentation | Wikis, ADRs, runbooks, and onboarding docs. Every operator can diagnose and resolve issues without tribal knowledge. |

**Chunked delivery:** Supportability foundations in chunks 009 (observability), 010 (Admin API), 027 (operational tooling).

---

### Pillar 8 — Observability & Monitoring

**Know what is happening inside the system at all times.**

| Concern | Approach |
|---------|----------|
| Three pillars of observability | **Logging** (structured JSON, correlated with trace context), **Metrics** (server health, backlog, throughput, tail latency via Prometheus), **Traces** (distributed traces via OpenTelemetry). |
| Observability purpose | Detect **unknown** issues → investigation. Find problems you didn't know existed. |
| Three pillars of monitoring | **Health Checks** (liveness, readiness, startup), **Alerts** (threshold-based, anomaly detection), **Visualization** (Grafana dashboards, real-time). |
| Monitoring purpose | Track **known** issues → visualization. Watch for expected failure modes. |
| Tooling | Prometheus (metrics + data storage) + Grafana (real-time monitoring + dashboards). OpenTelemetry (instrumentation). Loki (log aggregation). |
| Platform-specific | OpenClaw AI-powered diagnostics. "Where is my message?" natural language queries. Temporal workflow visibility. |

**Chunked delivery:** Observability in chunk 009 (done). Enhanced monitoring in chunks 027 (operational tooling).

---

### Pillar 9 — Operational Excellence

**Continuous improvement, right people right task, cost optimal.**

| Concern | Approach |
|---------|----------|
| Continuous improvement | Regular retrospectives on integration failures. Automated post-incident reviews. Trend analysis on DLQ patterns. |
| Right people, right task | Clear ownership per service/component. Automated routing of alerts to responsible teams. |
| Cost optimization | Right-size infrastructure. Use spot/preemptible instances for workers. TTL-based data cleanup. Avoid over-provisioning. |
| Lower risk | Canary deployments. Feature flags. Gradual rollouts. Automated rollback on failure. |
| Social impact | Reduce operational burden on teams. Automate toil. Improve developer experience. |
| Happy customer | SLA-driven design. Proactive notification on integration failures. Transparent status pages. |

**Chunked delivery:** Operational excellence is a cross-cutting concern applied across all phases. Dedicated chunk 027 (operational tooling).

---

### Pillar 10 — Testability

**Prove the system works correctly at every level.**

| Concern | Approach |
|---------|----------|
| TDD | Write tests before implementation where applicable. Tests are first-class citizens. |
| BDD | Behavior-driven scenarios for integration workflows. Given/When/Then specifications. |
| DDD | Domain-driven design for bounded contexts. Ubiquitous language in contracts and code. |
| Performance testing | Load testing (chunk 026). Throughput and latency benchmarks. Stress testing under 2× and 5× peak. |
| Unit tests | xUnit, FluentAssertions, NSubstitute. One logical assertion per test. `MethodName_Scenario_ExpectedResult` naming. |
| Integration tests | Testcontainers for real infrastructure (Loki, Kafka, Cassandra). WebApplicationFactory for API testing. |
| End-to-end tests | Full message lifecycle tests from ingress to delivery. Playwright for web UI testing. |
| A/B testing | Canary deployments with traffic splitting. Compare new vs old behavior with metrics. |
| Backward compatibility testing | Contract tests ensure schema changes don't break consumers. Especially critical in Kubernetes rolling updates. |

**Chunked delivery:** Test infrastructure in chunks 001–002 (scaffold, CI). Tests accompany every chunk. Load testing in chunk 026.

---

### Pillar 11 — Performance

**Optimize for time, space, and throughput.**

| Concern | Approach |
|---------|----------|
| Time vs space | Choose efficient algorithms and data structures. Profile before optimizing. Avoid premature optimization. |
| Efficient technology | Use the right tool for each job — Kafka for streaming, NATS/Pulsar for queuing, Cassandra for writes, Temporal for orchestration. |
| Caching | Cache authentication tokens (with expiry tracking). Cache frequently-accessed schemas. Cache connector configurations. Redis or in-memory with invalidation. |
| Concurrency & parallelism | Async/await everywhere. Parallel activity execution in Temporal. Consumer group parallelism in Kafka. Per-subject independence in NATS/Pulsar. |
| Optimized code | Minimize allocations. Use `Span<T>` and `Memory<T>` for buffer operations. Use `System.Text.Json` source generators for serialization. |
| Latency | Measure response time at every boundary. Use CDN/edge caching (Cloudflare) for public APIs. DNS routing (Route53) for geo-optimization. |
| Throughput | Measure messages per second at each stage. Identify and eliminate bottlenecks. Horizontal scaling for throughput-bound components. |
| Resource utilization | Monitor CPU, memory, disk, network. Right-size containers. Auto-scale based on utilization metrics. |
| Operations fail rate | Track and minimize failed operations. Distinguish transient vs permanent failures. Alert on failure rate spikes. |

**Chunked delivery:** Performance foundations in every chunk (efficient code). Dedicated performance testing in chunk 026 (load testing).

---

## Pillar Coverage by Chunk

| Chunk | Primary Pillars |
|-------|----------------|
| 001 Repository scaffold | 4 (Maintainability) |
| 002 CI pipeline | 4 (Maintainability), 10 (Testability) |
| 003 Aspire AppHost | 5 (Availability), 7 (Supportability) |
| 004 Contracts | 1 (Reliability), 4 (Maintainability) |
| 005 Message broker | 1 (Reliability), 3 (Scalability), 6 (Resilience) |
| 006 Temporal workflow | 1 (Reliability), 6 (Resilience) |
| 007 Cassandra storage | 1 (Reliability), 3 (Scalability), 11 (Performance) |
| 008 Ollama AI | 7 (Supportability), 9 (Operational Excellence) |
| 009 Observability | 8 (Observability), 7 (Supportability) |
| 010 Admin API | 2 (Security), 7 (Supportability) |
| 011 End-to-end pipeline | 1 (Reliability), 10 (Testability) |
| 012–018 EIP patterns | 1 (Reliability), 6 (Resilience), 11 (Performance) |
| 019–022 Connectors | 6 (Resilience), 11 (Performance) |
| 023 Security | 2 (Security) |
| 024 Multi-tenancy | 2 (Security), 3 (Scalability) |
| 025 Saga compensation | 1 (Reliability), 6 (Resilience) |
| 026 Load testing | 10 (Testability), 11 (Performance) |
| 027 Operational tooling | 7 (Supportability), 9 (Operational Excellence) |
| 028 AI code generation | 4 (Maintainability), 9 (Operational Excellence) |

---

## MVP Strategy

Design deliverable, chunked building blocks that **effectively capture user needs and validate core business value**.

The MVP must demonstrate:

1. **Message ingestion** — Accept a message from an external system (done: chunk 005)
2. **Transformation** — Map the message to a target format (chunk 013)
3. **Authentication** — Obtain and cache auth tokens for target APIs (chunk 019)
4. **Delivery** — Submit the transformed message to a target API (chunk 019)
5. **Ack/Nack** — Return success/failure notification to the sender (chunk 011)
6. **Zero message loss** — Survive restarts and partial outages (chunks 005–007)
7. **Observability** — Trace every message from ingress to delivery (done: chunk 009)

Each chunk after MVP adds depth to one or more pillars, building toward enterprise-grade production readiness across all 11 dimensions.
