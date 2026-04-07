# Quality Pillars — Architectural Quality Attributes

> These 11 pillars are the **highest-level** guiding principles for the Terranes platform.
> Every chunk, every design decision, and every line of code must satisfy the relevant pillars.
> Pillars are delivered incrementally across chunks — each chunk must advance at least one pillar.

## Design Philosophy

Terranes is a **3D immersive property platform** where buyers explore a virtual village of fully designed homes, walk through each home, test-fit designs onto their own land, and receive end-to-end indicative quotes — all before committing.

The platform acts as a broker and facilitator, connecting clients with builders, landscapers, interior/furnishing providers, and smart home suppliers. All legal contracts remain directly between client and each party.

By the time a client is referred, they are:
- Visually committed
- Budget-aware
- Scope-aligned
- Site-considered

---

## The 11 Pillars

### Pillar 1 — Reliability

**The system must continue to work correctly at all times.**

| Concern | Approach |
|---------|----------|
| Data integrity | Every design, quote, and transaction is persisted atomically. No partial saves |
| Delivery guarantees | All partner quote requests and notifications are delivered or retried |
| Idempotency | Duplicate submissions produce the same result without side effects |

---

### Pillar 2 — Security

**Protect data, access, and operations at every layer.**

| Concern | Approach |
|---------|----------|
| Authentication | OAuth 2.0, JWT for all users, agents, and partner APIs |
| Authorization | RBAC for standard use; fine-grained permissions per user role (buyer, agent, builder, admin) |
| Data protection | Encrypt data in transit (TLS) and at rest. GDPR-compliant data handling |
| Input validation | Validate and sanitise all input at service boundaries |

---

### Pillar 3 — Scalability

**Build fast, but don't block future growth.**

| Concern | Approach |
|---------|----------|
| Horizontal scaling | Stateless services behind load balancers |
| Storage scaling | Partitioned data for designs, quotes, and user data |
| Concurrent users | Support thousands of simultaneous 3D viewers and editors |

---

### Pillar 4 — Maintainability

**Over time, many people will work on this system. They must all be able to work productively.**

| Concern | Approach |
|---------|----------|
| High quality code | All code must be production-ready. See `reality-filter.md` and `coding-standards.md` |
| Consistent style | Enforce via .editorconfig, analyzers, and CI gates |
| Documentation-first | ADRs for significant decisions. Docs updated with every chunk |
| Modifiability | Adding a new partner type requires implementing one interface — no core changes |

---

### Pillar 5 — Availability

**Design for high availability.**

| Concern | Approach |
|---------|----------|
| Uptime targets | 99.9% uptime for user-facing services |
| Failover | Automatic failover for stateful components |
| Zero-downtime deployments | Blue-green or canary deployments |

---

### Pillar 6 — Resilience

**Build fault-tolerant systems that continue operating under failure.**

| Concern | Approach |
|---------|----------|
| Retry logic | Exponential backoff with jitter for all partner API calls |
| Circuit breakers | Per-partner circuit breakers to prevent cascading failures |
| Graceful degradation | If a partner quote service is down, show cached/estimated pricing |

---

### Pillar 7 — Supportability

**Systems must be diagnosable and controllable through standardised tooling.**

| Concern | Approach |
|---------|----------|
| Observability | Structured logging, metrics, health checks |
| Incident response | Runbooks, escalation procedures |
| Automation | If it takes more than an hour and is repeated, automate it |

---

### Pillar 8 — Observability & Monitoring

**Know what is happening inside the system at all times.**

| Concern | Approach |
|---------|----------|
| Logging | Structured JSON logs with correlation IDs |
| Metrics | Throughput, latency, error rates per service |
| Health checks | Liveness, readiness probes on every service |

---

### Pillar 9 — Operational Excellence

**Continuous improvement, right people right task, cost optimal.**

| Concern | Approach |
|---------|----------|
| Continuous improvement | Regular retrospectives on partner integration failures |
| Cost optimisation | Right-size infrastructure, TTL-based data cleanup |
| Happy customer | Proactive notification on quote updates and status changes |

---

### Pillar 10 — Testability

**Prove the system works correctly at every level.**

| Concern | Approach |
|---------|----------|
| Unit tests | NUnit, NSubstitute. One logical assertion per test |
| Integration tests | Real service tests with test containers |
| End-to-end tests | Full workflow from design to quote |

---

### Pillar 11 — Performance

**Optimise for time, space, and throughput.**

| Concern | Approach |
|---------|----------|
| 3D rendering | Optimise model loading, LOD (level of detail), and streaming for real-time interaction |
| Quote latency | Sub-second cached quotes, seconds for fresh aggregated quotes |
| API response times | < 200ms for standard API calls |

---

## Pillar Coverage by Chunk

| Chunk | Primary Pillars |
|-------|----------------|
| 001 Repository scaffold | 4 (Maintainability) |
| 002 3D Model Service | 1 (Reliability), 11 (Performance) |
| 003 Land Data Service | 1 (Reliability), 3 (Scalability) |
| 004 Site Placement Engine | 11 (Performance), 1 (Reliability) |
| 005 Quoting Engine | 1 (Reliability), 6 (Resilience) |
| 006 Marketplace Service | 3 (Scalability), 2 (Security) |
| 007 Compliance Engine | 1 (Reliability), 2 (Security) |
| 008–013 Partner integrations | 6 (Resilience), 3 (Scalability) |
| 014–018 Immersive 3D | 11 (Performance), 5 (Availability) |
| 019–022 Platform infrastructure | 2 (Security), 8 (Observability) |
