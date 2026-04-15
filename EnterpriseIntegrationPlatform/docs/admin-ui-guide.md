# Admin UI Guide — Walkthrough of All 19 Pages

> Complete guide to the EIP Admin Dashboard. Covers every page, what it does, how to use it, and tips for daily operations.

---

## Overview

The Admin Dashboard is a **Vue 3 single-page application** with 19 pages organized into 4 sections:

| Section | Pages | Purpose |
|---------|-------|---------|
| **Monitoring** | Dashboard, Message Flow, Messages, In-Flight, Subscriptions, Connectors, Event Store | Real-time visibility into platform operations |
| **Operations** | DLQ, Replay, Test Messages, Control Bus | Day-to-day operational tasks |
| **Configuration** | Throttle, Rate Limiting, Config, Feature Flags, Tenants | Platform configuration management |
| **System** | Audit Log, DR Drills, Profiling | System health, compliance, and performance |

### Accessing the Dashboard

- **Local dev (Aspire):** Check the Aspire Dashboard for the Admin.Web URL (typically `http://localhost:15200`)
- **Docker/K8s:** Navigate to the configured Admin.Web endpoint

### Theme Toggle

Click the **☀️ Light / 🌙 Dark** button at the bottom of the sidebar to switch between light and dark themes. Your preference is saved in the browser.

### Sidebar Navigation

The sidebar is **collapsible** — click the ◀/▶ button to collapse or expand it. In collapsed mode, hover over icons to see tooltips.

---

## Monitoring Section

### 1. 📊 Dashboard

**What it shows:** Platform health overview with real-time metrics.

**Key elements:**
- **Total Messages** — Count of messages processed (today / all time)
- **Active Workflows** — Currently running Temporal workflows
- **Error Rate** — Percentage of failed messages in the last hour
- **Broker Status** — Health of the active message broker (NATS/Kafka/Pulsar)
- **Service Status** — Health of all platform services
- **Throughput Chart** — Messages per second over time
- **Recent Errors** — Last 10 error events with details

**When to use:** First page you check every morning. Gives instant visibility into whether the platform is healthy.

**Tips:**
- If error rate > 1%, investigate immediately via the DLQ page
- Rising active workflow count may indicate downstream systems are slow
- Use the refresh button to get the latest data

---

### 2. 🔀 Message Flow

**What it shows:** Visual timeline of message processing flows.

**Key elements:**
- **Flow Timeline** — Step-by-step visualization of a message's journey
- **Step Details** — Click any step to see timing, input/output, and status
- **Filter** — Filter by message type, date range, or status
- **Search** — Search by correlation ID or business key

**When to use:** Troubleshooting a specific message's journey. Understanding where in the pipeline a message is or where it failed.

**Tips:**
- Failed steps are highlighted in red — click to see the error details
- Long steps may indicate downstream performance issues
- Use the correlation ID from the Gateway API response to find specific flows

---

### 3. 🔍 Messages (Message Inspector)

**What it shows:** Search and inspect individual message envelopes.

**Key elements:**
- **Search Bar** — Search by Message ID, Correlation ID, or Business Key
- **Message List** — Results with message type, status, timestamp, and priority
- **Message Detail** — Full envelope inspection: headers, payload, metadata, processing history
- **Copy Button** — Copy the full envelope JSON to clipboard

**When to use:** When you need to inspect the exact content of a specific message or verify that a message was correctly received and processed.

**Tips:**
- Use the Business Key for searches that relate to your domain (e.g., order number)
- The processing history shows every activity that touched the message
- Expand the metadata section to see tenant, routing, and trace information

---

### 4. ⚡ In-Flight Messages

**What it shows:** Messages currently being processed in real-time.

**Key elements:**
- **In-Flight Count** — Total messages currently in the pipeline
- **Message Table** — Each in-flight message with type, age, current step, tenant
- **Age Warning** — Messages older than the threshold are highlighted
- **Auto-Refresh** — Updates every few seconds

**When to use:** Monitoring during high-throughput periods or investigating processing delays.

**Tips:**
- Healthy platforms show in-flight messages appearing and disappearing quickly
- Stuck messages (high age) may indicate workflow issues — check Temporal
- Sort by age to find the oldest messages first

---

### 5. 📡 Subscriptions

**What it shows:** Active message routing subscriptions (similar to BizTalk subscription viewer).

**Key elements:**
- **Subscription List** — All active subscriptions with topic, filter, and consumer group
- **Subscription Detail** — Full subscription configuration and active consumer count
- **Status Indicators** — Active, paused, or unhealthy subscriptions

**When to use:** Verifying that routing subscriptions are correctly configured. Troubleshooting messages not reaching expected consumers.

**Tips:**
- Compare subscriptions to your routing rules to verify coverage
- Inactive consumers may indicate a crashed service — check the Aspire Dashboard
- Use this page alongside the Message Flow page for end-to-end routing verification

---

### 6. 🔌 Connectors

**What it shows:** Health status of all outbound connectors (HTTP, SFTP, Email, File).

**Key elements:**
- **Connector Cards** — Each connector with type, target, health status, and last delivery time
- **Health Status** — Healthy (green), Degraded (yellow), Unhealthy (red)
- **Circuit Breaker** — Shows whether the circuit breaker is open/closed/half-open
- **Test Connection** — Button to test connectivity to the target system

**When to use:** Verifying that target systems are reachable. Investigating delivery failures.

**Tips:**
- Open circuit breakers mean the connector has stopped attempting delivery after repeated failures
- Use "Test Connection" to verify connectivity before investigating further
- Monitor the "last delivery time" — large gaps may indicate a problem

---

### 7. 📚 Event Store

**What it shows:** Browse events stored by the Event Sourcing system.

**Key elements:**
- **Stream Browser** — Browse event streams by aggregate ID
- **Event List** — Chronological list of events with type, timestamp, and sequence number
- **Event Detail** — Full event payload and metadata
- **Snapshot View** — View aggregate snapshots at specific versions

**When to use:** Auditing event history for a specific aggregate. Debugging event sourcing behavior.

**Tips:**
- Events are immutable — you're seeing the exact record of what happened
- Use the aggregate ID to trace all events for a specific business entity
- Snapshots show the computed state at a point in time

---

## Operations Section

### 8. 📬 DLQ (Dead Letter Queue)

**What it shows:** Messages that failed processing and landed in the dead letter queue.

**Key elements:**
- **DLQ Count** — Total messages in the DLQ
- **Message List** — Failed messages with error type, message type, timestamp, and retry count
- **Error Details** — Full exception information (type, message, stack trace)
- **Resubmit Button** — Resubmit a message back into the pipeline
- **Bulk Actions** — Select and resubmit multiple messages

**When to use:** Daily DLQ review. Investigating and resolving failed messages.

**Tips:**
- Group by error type to identify systemic issues (e.g., all failures are "ConnectionRefused")
- Fix the root cause before bulk resubmitting — otherwise they'll just fail again
- Resubmitted messages get a `ReplayId` header for audit trail

---

### 9. ⏪ Replay

**What it shows:** Message replay management — resubmit previously processed messages.

**Key elements:**
- **Replay Form** — Specify correlation ID or message type + date range
- **Replay History** — Log of all replay operations with status and count
- **Dry Run** — Preview which messages would be replayed without actually replaying

**When to use:** Re-processing messages after a bug fix. Resubmitting messages that were delivered to the wrong target.

**Tips:**
- Always use Dry Run first to verify the scope of the replay
- Replay creates new messages with `ReplayId` — the originals are preserved
- Monitor the DLQ after a replay to catch any new failures

---

### 10. 🧪 Test Messages

**What it shows:** Generate and submit test messages for pipeline verification.

**Key elements:**
- **Message Template** — Pre-built templates for common message types
- **Custom Payload** — JSON editor for custom message payloads
- **Submit Button** — Send the test message through the Gateway
- **Response** — Message ID and Correlation ID from the Gateway response
- **Quick Track** — Link to track the submitted message in Message Flow

**When to use:** Verifying that a new routing rule or transformation works correctly. Testing after configuration changes.

**Tips:**
- Use meaningful business keys (e.g., "test-2024-01-15-routing") for easy tracking
- Test with different message types to verify content-based routing
- The response's correlation ID links directly to the Message Flow page

---

### 11. 🎛️ Control Bus

**What it shows:** Platform-wide control commands (EIP Control Bus pattern).

**Key elements:**
- **Command Console** — Send control commands to platform services
- **Service List** — All services with their current status (running/paused/stopped)
- **Command History** — Log of all control commands issued
- **Pause/Resume** — Pause or resume message processing on specific services

**When to use:** Pausing processing during maintenance. Sending diagnostic commands to services.

**Tips:**
- Pause consumers before deploying configuration changes to prevent partial processing
- Resume in order: workers first, then consumers, then gateway
- All control commands are audit logged — use the Audit Log page to review

---

## Configuration Section

### 12. 🔧 Throttle

**What it shows:** Manage throttle policies that control message processing rates.

**Key elements:**
- **Policy List** — All throttle policies with current utilization
- **Create/Edit Policy** — Form to create or modify throttle policies
- **Policy Fields** — Policy ID, name, tenant, queue, max messages/sec, burst capacity
- **Delete Policy** — Remove a throttle policy

**When to use:** Protecting downstream systems from overload. Controlling processing rates per tenant.

**Tips:**
- Start with conservative limits and increase based on monitoring
- Set `burstCapacity` to 2–3× the `maxMessagesPerSecond` for handling spikes
- Enable `rejectOnBackpressure` for latency-sensitive endpoints

---

### 13. 🚦 Rate Limiting

**What it shows:** Current rate limiting status at the Gateway level.

**Key elements:**
- **Rate Limit Status** — Current request rate vs. configured limit per endpoint
- **Per-Tenant Limits** — Rate limits broken down by tenant
- **Rejected Requests** — Count and details of requests rejected due to rate limiting

**When to use:** Monitoring Gateway utilization. Investigating "429 Too Many Requests" errors.

**Tips:**
- Rate limiting protects the platform from being overwhelmed
- If legitimate traffic is being rejected, increase the rate limit for that tenant/endpoint
- Use alongside the Throttle page — rate limiting is at the Gateway, throttling is at the processing layer

---

### 14. ⚙️ Config

**What it shows:** Dynamic configuration store — runtime configuration without restarts.

**Key elements:**
- **Configuration Keys** — Hierarchical list of all configuration values
- **Edit Value** — Modify a configuration value at runtime
- **History** — Change history for each configuration key
- **Effective Config** — View the merged configuration from all sources

**When to use:** Changing routing rules, connector settings, or processing parameters without restarting services.

**Tips:**
- Configuration changes take effect within seconds
- All changes are audit logged with who/when/what
- Use the history view to understand when a setting was last changed

---

### 15. 🚩 Feature Flags

**What it shows:** Manage feature flags for gradual rollout and A/B testing.

**Key elements:**
- **Flag List** — All feature flags with current state (on/off/percentage)
- **Toggle** — Enable or disable a feature flag
- **Targeting Rules** — Configure which tenants or message types a flag applies to
- **Audit Trail** — Who toggled which flag and when

**When to use:** Rolling out new processing logic gradually. Enabling/disabling features per tenant.

**Tips:**
- Use feature flags to safely roll out new transformations or routing rules
- Start with a small percentage and increase based on monitoring
- Feature flags are evaluated at runtime — no deployment needed

---

### 16. 🏢 Tenants

**What it shows:** Manage multi-tenant configuration.

**Key elements:**
- **Tenant List** — All tenants with status, message limits, and enabled connectors
- **Create Tenant** — Onboard a new tenant with configuration
- **Edit Tenant** — Modify tenant settings (limits, connectors, routing overrides)
- **Tenant Health** — Per-tenant message throughput and error rates

**When to use:** Onboarding new tenants. Adjusting tenant-specific settings.

**Tips:**
- Each tenant gets isolated broker topics/subjects for data isolation
- Set appropriate rate limits per tenant to prevent noisy neighbor issues
- Use the tenant health view to identify tenants with high error rates

---

## System Section

### 17. 📋 Audit Log

**What it shows:** Comprehensive audit trail of all administrative actions.

**Key elements:**
- **Log Entries** — All admin actions with timestamp, user, action, and details
- **Filter** — Filter by date range, user, action type, or resource
- **Export** — Export audit log entries for compliance reporting

**When to use:** Security auditing. Compliance reporting. Investigating configuration changes.

**Tips:**
- Review the audit log after any incident to understand what changed
- Export regularly for compliance archives
- All configuration changes, DLQ resubmissions, and control commands are logged

---

### 18. 🛡️ DR Drills

**What it shows:** Disaster recovery drill management.

**Key elements:**
- **Execute Drill** — Start a DR drill with a specific scenario
- **Drill History** — Past drill results with pass/fail status and duration
- **Scenario Library** — Pre-built DR scenarios (Cassandra failover, broker switchover, etc.)
- **Drill Report** — Detailed report of each drill step and its outcome

**When to use:** Scheduled DR testing. Validating recovery procedures before they're needed.

**Tips:**
- Run DR drills monthly at minimum
- Review drill reports to identify areas for improvement
- Use drill results to update the operations runbook

---

### 19. 📈 Profiling

**What it shows:** Performance profiling and diagnostics.

**Key elements:**
- **Memory Snapshots** — Current heap usage, GC generation statistics
- **CPU Profiling** — CPU utilization across services
- **GC Diagnostics** — Garbage collection frequency, pause times, heap sizes
- **Benchmarks** — Run built-in performance benchmarks

**When to use:** Investigating performance issues. Capacity planning. Memory leak detection.

**Tips:**
- Take memory snapshots before and after load tests to detect leaks
- High GC pause times indicate memory pressure — consider scaling up
- Use alongside the Grafana dashboards for historical performance data

---

## Daily Operations Workflow

A recommended daily routine using the Admin Dashboard:

### Morning Check (5 minutes)

1. **Dashboard** → Verify error rate is < 1% and all services are healthy
2. **DLQ** → Check for overnight failures; investigate and resubmit as needed
3. **Connectors** → Verify all connectors are healthy (no open circuit breakers)
4. **In-Flight** → Confirm no stuck messages (no entries older than expected)

### Incident Investigation

1. **Dashboard** → Identify the anomaly (error spike, throughput drop)
2. **DLQ** → Check error types for the affected time period
3. **Message Flow** → Trace a specific failing message to find the broken step
4. **Connectors** → Check if a downstream system is unhealthy
5. **Audit Log** → Check if any configuration change coincided with the incident
6. **Control Bus** → Pause affected consumers while investigating (if needed)
7. **Replay** → Resubmit affected messages after fixing the root cause

### Configuration Change

1. **Config / Feature Flags** → Make the change
2. **Audit Log** → Verify the change was recorded
3. **Test Messages** → Submit a test message to verify behavior
4. **Message Flow** → Track the test message through the pipeline
5. **Dashboard** → Monitor for 15 minutes after the change

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Click sidebar icon | Navigate to page |
| Click collapse button (◀/▶) | Toggle sidebar |
| Click theme button | Toggle dark/light mode |

---

## Next Steps

| Guide | Description |
|-------|-------------|
| [Quick Start](quickstart.md) | Submit your first message in 15 minutes |
| [Platform Usage Guide](platform-usage-guide.md) | Detailed configuration and operations |
| [Operations Runbook](operations-runbook.md) | Production monitoring and incident response |
| [Tutorial Course](../tutorials/README.md) | 50 hands-on tutorials |
