# Onboarding Checklist

> Structured checklist for new team members joining the Enterprise Integration Platform. Complete each section in order.

---

## Week 1 — Environment Setup & First Message

### Day 1: Install & Build

- [ ] Install .NET 10 SDK — verify with `dotnet --version`
- [ ] Install Docker Desktop — verify it's running
- [ ] Install Node.js 20+ — verify with `node --version`
- [ ] Install .NET Aspire templates: `dotnet new install Aspire.ProjectTemplates`
- [ ] Clone the repository
- [ ] Run `dotnet restore` in the `EnterpriseIntegrationPlatform` directory
- [ ] Run `dotnet build` — verify 0 warnings, 0 errors
- [ ] Run `npm install` in `src/Admin.Web/clientapp`

### Day 1: Run & Explore

- [ ] Start the platform: `cd src/AppHost && dotnet run`
- [ ] Open the Aspire Dashboard — verify all services are running
- [ ] Open the Admin Dashboard — browse through all 19 pages
- [ ] Follow the [Quick Start](quickstart.md) — submit your first message via curl
- [ ] Track your message in the Message Flow page
- [ ] Track your message in OpenClaw ("Where Is My Message?")
- [ ] Toggle dark/light theme in the Admin Dashboard
- [ ] Collapse and expand the sidebar

### Day 2: Run Tests

- [ ] Run unit tests: `dotnet test tests/UnitTests` (expect ~1919 passing)
- [ ] Run contract tests: `dotnet test tests/ContractTests` (expect 57 passing)
- [ ] Run tutorial labs: `dotnet test tests/TutorialLabs` (expect 526 passing)
- [ ] Run Vue frontend tests: `cd src/Admin.Web/clientapp && npx vitest run` (expect 100 passing)
- [ ] Run broker-agnostic tests: `dotnet test tests/BrokerAgnosticTests` (expect 38 passing)

---

## Week 1 — Core Concepts (Tutorials 01–08)

### Day 2–3: Getting Started Tutorials

- [ ] Complete [Tutorial 01 — Introduction](../tutorials/01-introduction.md) (Lab + Exam)
- [ ] Complete [Tutorial 02 — Environment Setup](../tutorials/02-environment-setup.md)
- [ ] Complete [Tutorial 03 — Your First Message](../tutorials/03-first-message.md)

### Day 3–5: Core Concepts

- [ ] Complete [Tutorial 04 — The Integration Envelope](../tutorials/04-integration-envelope.md)
- [ ] Complete [Tutorial 05 — Message Brokers](../tutorials/05-message-brokers.md)
- [ ] Complete [Tutorial 06 — Messaging Channels](../tutorials/06-messaging-channels.md)
- [ ] Complete [Tutorial 07 — Temporal Workflows](../tutorials/07-temporal-workflows.md)
- [ ] Complete [Tutorial 08 — Activities and the Pipeline](../tutorials/08-activities-pipeline.md)

**Checkpoint:** You should understand IntegrationEnvelope, message brokers, Temporal workflows, and activity pipelines.

---

## Week 2 — Routing, Transformation & Error Handling (Tutorials 09–28)

### Day 6–7: Message Routing

- [ ] Complete [Tutorial 09 — Content-Based Router](../tutorials/09-content-based-router.md)
- [ ] Complete [Tutorial 10 — Message Filter](../tutorials/10-message-filter.md)
- [ ] Complete [Tutorial 11 — Dynamic Router](../tutorials/11-dynamic-router.md)
- [ ] Complete [Tutorial 12 — Recipient List](../tutorials/12-recipient-list.md)
- [ ] Complete [Tutorial 13 — Routing Slip](../tutorials/13-routing-slip.md)
- [ ] Complete [Tutorial 14 — Process Manager](../tutorials/14-process-manager.md)

### Day 8–9: Message Transformation

- [ ] Complete [Tutorial 15 — Message Translator](../tutorials/15-message-translator.md)
- [ ] Complete [Tutorial 16 — Transform Pipeline](../tutorials/16-transform-pipeline.md)
- [ ] Complete [Tutorial 17 — Normalizer](../tutorials/17-normalizer.md)
- [ ] Complete [Tutorial 18 — Content Enricher](../tutorials/18-content-enricher.md)
- [ ] Complete [Tutorial 19 — Content Filter](../tutorials/19-content-filter.md)

### Day 9–10: Message Construction & Decomposition

- [ ] Complete [Tutorial 20 — Splitter](../tutorials/20-splitter.md)
- [ ] Complete [Tutorial 21 — Aggregator](../tutorials/21-aggregator.md)
- [ ] Complete [Tutorial 22 — Scatter-Gather](../tutorials/22-scatter-gather.md)
- [ ] Complete [Tutorial 23 — Request-Reply](../tutorials/23-request-reply.md)

### Day 10: Reliability & Error Handling

- [ ] Complete [Tutorial 24 — Retry Framework](../tutorials/24-retry-framework.md)
- [ ] Complete [Tutorial 25 — Dead Letter Queue](../tutorials/25-dead-letter-queue.md)
- [ ] Complete [Tutorial 26 — Message Replay](../tutorials/26-message-replay.md)
- [ ] Complete [Tutorial 27 — Resequencer](../tutorials/27-resequencer.md)
- [ ] Complete [Tutorial 28 — Competing Consumers](../tutorials/28-competing-consumers.md)

**Checkpoint:** You should understand all EIP routing patterns, transformation pipeline, and error handling strategies.

---

## Week 3 — Advanced Patterns & Operations (Tutorials 29–46)

### Day 11–12: Advanced Patterns

- [ ] Complete [Tutorial 29 — Throttle and Rate Limiting](../tutorials/29-throttle-rate-limiting.md)
- [ ] Complete [Tutorial 30 — Business Rule Engine](../tutorials/30-rule-engine.md)
- [ ] Complete [Tutorial 31 — Event Sourcing](../tutorials/31-event-sourcing.md)
- [ ] Complete [Tutorial 32 — Multi-Tenancy](../tutorials/32-multi-tenancy.md)
- [ ] Complete [Tutorial 33 — Security](../tutorials/33-security.md)

### Day 12–13: Connectors

- [ ] Complete [Tutorial 34 — HTTP Connector](../tutorials/34-connector-http.md)
- [ ] Complete [Tutorial 35 — SFTP Connector](../tutorials/35-connector-sftp.md)
- [ ] Complete [Tutorial 36 — Email Connector](../tutorials/36-connector-email.md)
- [ ] Complete [Tutorial 37 — File Connector](../tutorials/37-connector-file.md)

### Day 13–14: Observability & AI

- [ ] Complete [Tutorial 38 — OpenTelemetry Observability](../tutorials/38-opentelemetry.md)
- [ ] Complete [Tutorial 39 — Message Lifecycle Tracking](../tutorials/39-message-lifecycle.md)
- [ ] Complete [Tutorial 40 — Self-Hosted RAG with Ollama](../tutorials/40-rag-ollama.md)
- [ ] Complete [Tutorial 41 — OpenClaw Web UI](../tutorials/41-openclaw-web.md)

### Day 14–15: Production Deployment

- [ ] Complete [Tutorial 42 — Dynamic Configuration](../tutorials/42-configuration.md)
- [ ] Complete [Tutorial 43 — Kubernetes Deployment](../tutorials/43-kubernetes-deployment.md)
- [ ] Complete [Tutorial 44 — Disaster Recovery](../tutorials/44-disaster-recovery.md)
- [ ] Complete [Tutorial 45 — Performance Profiling](../tutorials/45-performance-profiling.md)
- [ ] Complete [Tutorial 46 — Building a Complete Integration](../tutorials/46-complete-integration.md)

**Checkpoint:** You can build complete integrations, deploy to production, and operate the platform.

---

## Week 4 — Real-World Scenarios & Admin Operations (Tutorials 47–50)

### Day 16–17: Real-World Scenarios

- [ ] Complete [Tutorial 47 — Saga Compensation Pattern](../tutorials/47-saga-compensation.md)
- [ ] Complete [Tutorial 48 — Notification Use Cases](../tutorials/48-notification-use-cases.md)
- [ ] Complete [Tutorial 49 — Testing Your Integrations](../tutorials/49-testing-integrations.md)
- [ ] Complete [Tutorial 50 — Best Practices and Patterns](../tutorials/50-best-practices.md)

### Day 17–18: Admin Dashboard Deep Dive

- [ ] Read the [Admin UI Guide](admin-ui-guide.md) in full
- [ ] Practice the Daily Operations Workflow (Dashboard → DLQ → Connectors → In-Flight)
- [ ] Create a throttle policy via the Throttle page
- [ ] Use the Test Message Generator to submit messages with different types
- [ ] Track test messages through Message Flow
- [ ] Practice DLQ investigation: deliberately send a bad message and trace the failure
- [ ] Use the Replay page to resubmit a message
- [ ] Toggle feature flags and observe the effect on processing

### Day 18–19: Operations Readiness

- [ ] Read the [Operations Runbook](operations-runbook.md)
- [ ] Read the [Architecture Overview](architecture-overview.md)
- [ ] Read the [Security documentation](security.md)
- [ ] Read the [BizTalk Migration Guide](migration-from-biztalk.md) (if migrating from BizTalk)
- [ ] Execute a DR drill from the DR Drills page
- [ ] Review the Audit Log for your recent actions
- [ ] Explore the Profiling page — take a memory snapshot

### Day 19–20: Independent Practice

- [ ] Design and implement a simple integration end-to-end:
  - Define a message type for your domain
  - Submit via Gateway API
  - Route using content-based routing
  - Transform the message
  - Deliver via an HTTP connector
  - Track the message in OpenClaw
  - Monitor in the Admin Dashboard
- [ ] Run the full test suite and verify everything passes

---

## Documentation Reference

| Document | Purpose |
|----------|---------|
| [Quick Start](quickstart.md) | 15-minute first message |
| [Installation Guide](installation-guide.md) | All deployment modes |
| [Admin UI Guide](admin-ui-guide.md) | All 19 dashboard pages |
| [Developer Setup](developer-setup.md) | IDE and tooling setup |
| [Architecture Overview](architecture-overview.md) | System design and data flow |
| [Platform Usage Guide](platform-usage-guide.md) | Configuration, connectors, routing |
| [BizTalk Migration](migration-from-biztalk.md) | BizTalk concept mapping |
| [Operations Runbook](operations-runbook.md) | Monitoring, alerting, DR |
| [Security](security.md) | Authentication, secrets, encryption |
| [API Reference](api-reference.md) | REST API documentation |
| [Tutorial Course](../tutorials/README.md) | 50 hands-on tutorials |

---

## Completion Criteria

You're ready for production work when you can:

- ✅ Start the platform locally and submit messages
- ✅ Navigate all 19 Admin Dashboard pages confidently
- ✅ Explain the IntegrationEnvelope and message flow
- ✅ Configure content-based routing and transformations
- ✅ Set up connectors (HTTP, SFTP, Email, File)
- ✅ Investigate and resolve DLQ messages
- ✅ Use OpenClaw to track messages end-to-end
- ✅ Manage throttle policies and rate limits
- ✅ Understand multi-tenancy and tenant isolation
- ✅ Run and interpret platform tests
- ✅ Explain the difference between NATS, Kafka, Pulsar, and PostgreSQL brokers
