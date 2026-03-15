# Operations Runbook

## Overview

This runbook provides operational procedures for the Enterprise Integration Platform, covering startup, health checks, monitoring, alerting, troubleshooting, and disaster recovery.

## Startup Procedures

### Local Development (Aspire)

```bash
# Start all services with .NET Aspire
cd src/AppHost
dotnet run

# Aspire Dashboard available at: https://localhost:18888
# Ingress API: https://localhost:5001
# Admin API: https://localhost:5002
```

Aspire automatically starts and configures:
- Kafka (single broker, port 9092)
- NATS JetStream (single node, host port 15222)
- Temporal (dev server, host port 15233)
- Temporal UI (host port 15280)
- Cassandra (single node, port 9042)
- Ollama (host port 15434)
- RagFlow (UI host port 15080, API host port 15380)
- Loki (host port 15100)
- All platform services

### Production Startup Order

Infrastructure must be started in dependency order:

```
1. Cassandra cluster (verify all nodes join the ring)
2. Kafka cluster (verify all brokers register with controller)
2a. NATS/Pulsar cluster (verify all nodes are healthy)
3. Temporal server (verify frontend, history, matching, worker services)
4. Platform services:
   a. Worker services (Kafka consumers + Temporal workers)
   b. Ingress API (accepts inbound messages)
   c. Admin API (management endpoints)
```

### Pre-Flight Checks

Before accepting traffic, verify:

- [ ] Cassandra: All nodes UN (Up/Normal) in `nodetool status`
- [ ] Kafka: All brokers in-sync, no under-replicated partitions
- [ ] NATS/Pulsar: All nodes healthy, streams/topics available
- [ ] Temporal: Namespace exists and is active
- [ ] Worker services: Connected to Kafka and Temporal, health check passing
- [ ] Ingress API: Health check at `/health/ready` returns 200
- [ ] Admin API: Health check at `/health/ready` returns 200

## Health Checks

### Endpoint Summary

| Service       | Liveness             | Readiness              | Startup               |
|---------------|----------------------|------------------------|-----------------------|
| Ingress API   | `/health/live`       | `/health/ready`        | `/health/startup`     |
| Admin API     | `/health/live`       | `/health/ready`        | `/health/startup`     |
| Worker Svc    | `/health/live`       | `/health/ready`        | `/health/startup`     |

### Health Check Interpretation

| Status   | Meaning                                    | Action                     |
|----------|--------------------------------------------|-----------------------------|
| Healthy  | All dependencies connected and responsive  | None                        |
| Degraded | Non-critical dependency unavailable        | Monitor; investigate if sustained |
| Unhealthy| Critical dependency unavailable            | Investigate immediately     |

### Manual Health Verification

```bash
# Check Ingress API health
curl -s https://ingress-api/health/ready | jq .

# Check Kafka broker health
kafka-broker-api-versions.sh --bootstrap-server kafka:9092

# Check NATS JetStream health
nats server check jetstream --server nats://nats:4222

# Check Temporal namespace
temporal operator namespace describe --namespace eip-production

# Check Cassandra cluster status
nodetool status eip_platform

# Check consumer group lag
kafka-consumer-groups.sh --bootstrap-server kafka:9092 \
  --group eip-workflow-starters --describe
```

## Monitoring

### Dashboard Access

| Dashboard            | URL                              | Purpose                      |
|----------------------|----------------------------------|------------------------------|
| Aspire Dashboard     | `https://localhost:18888`        | Local dev service overview   |
| Grafana              | `https://monitoring/grafana`     | Production metrics & alerts  |
| Temporal UI          | `https://temporal-ui/`           | Workflow visibility          |
| Jaeger/Tempo         | `https://monitoring/traces`      | Distributed trace viewer     |

### Key Metrics to Watch

**Real-Time (check every 5 minutes during incidents):**
- Message throughput (received vs. processed vs. failed)
- Kafka consumer lag per consumer group
- Active Temporal workflow count
- Error rate by activity type

**Daily Review:**
- End-to-end latency trends (p50, p95, p99)
- DLQ message count and age
- Connector health status and circuit breaker events
- Storage utilization (Kafka disk, Cassandra disk)

## Alerting

### Alert Routing

| Severity | Channel          | Response Time | Examples                          |
|----------|------------------|---------------|-----------------------------------|
| Critical | PagerDuty + Slack| 15 minutes    | Service down, data loss risk      |
| Warning  | Slack channel    | 1 hour        | High lag, elevated errors         |
| Info     | Email digest     | Next business day | Capacity thresholds approaching |

### Alert Runbooks

#### Alert: High Consumer Lag

**Symptoms:** `eip.kafka.consumer_lag` > 10,000 for 5 minutes.

**Investigation:**
1. Check consumer group status: `kafka-consumer-groups.sh --describe --group {group}`
2. Verify worker service health: `curl /health/ready`
3. Check worker logs for errors: search for `ERROR` level entries
4. Check Temporal for stuck workflows: query by `ProcessingStatus = "Running"` older than 1 hour

**Resolution:**
- If workers are healthy: scale up worker replicas
- If workers are unhealthy: check dependency health (Temporal, Cassandra)
- If Temporal is slow: check Temporal server metrics and persistence database

#### Alert: DLQ Messages Accumulating

**Symptoms:** DLQ topic depth > 100 messages for 15 minutes.

**Investigation:**
1. Review DLQ messages via Admin API: `GET /api/dlq/messages?limit=10`
2. Identify common error patterns (same error type, same connector)
3. Check if a connector is down (circuit breaker open)

**Resolution:**
- If connector issue: fix connector, then replay DLQ messages
- If data quality issue: fix data, then replay specific messages
- If permanent errors: review and discard invalid messages

#### Alert: Circuit Breaker Open

**Symptoms:** Any connector circuit breaker in OPEN state.

**Investigation:**
1. Check connector health: `GET /api/connectors/{id}/health`
2. Test target system connectivity manually
3. Review recent connector errors in logs

**Resolution:**
- If target system down: wait for recovery; circuit breaker will probe automatically
- If authentication issue: rotate credentials, update connector config
- If configuration issue: update connector configuration and reset circuit breaker

## Troubleshooting

### Message Not Processing

```
Symptom: Message submitted but not appearing in target system.

Diagnostic steps:
1. Get envelope ID from ingress API response (202 Accepted body)
2. Search Kafka ingestion topic for the envelope ID
3. Search Temporal UI for workflow by EnvelopeId search attribute
4. If workflow found: check workflow history for failed activities
5. If workflow not found: check consumer lag on ingestion topic
6. Check DLQ topics for the envelope ID
```

### Workflow Stuck

```
Symptom: Temporal workflow running longer than expected.

Diagnostic steps:
1. Find workflow in Temporal UI by workflow ID
2. Check workflow history: what was the last completed event?
3. If waiting on activity: check activity worker health
4. If activity retrying: check error details and retry count
5. If waiting on signal: check if human approval is needed

Resolution:
- Signal the workflow if waiting for approval
- Terminate and reprocess if workflow is in an unrecoverable state
- Reset workflow to a specific event if a transient issue has been fixed
```

### Performance Degradation

```
Symptom: End-to-end latency increasing.

Diagnostic steps:
1. Check which layer has increased latency (trace analysis)
2. Kafka: Check consumer lag and broker latency
3. Temporal: Check schedule-to-start latency (worker saturation)
4. Cassandra: Check query latency (compaction, disk I/O)
5. Connectors: Check target system response times

Resolution:
- Scale the bottleneck layer (add workers, consumers, Cassandra nodes)
- If Cassandra compaction: verify compaction strategy is appropriate
- If connector latency: check target system health
```

## Disaster Recovery

### Backup Strategy

| Component   | Backup Method                    | Frequency | Retention |
|-------------|----------------------------------|-----------|-----------|
| Cassandra   | Snapshot + incremental backup    | Daily     | 30 days   |
| Kafka       | MirrorMaker 2 to DR cluster     | Continuous| Real-time |
| Temporal    | Persistence DB backup            | Daily     | 30 days   |
| Configuration| Git repository                  | On change | Unlimited |

### Recovery Procedures

#### Full Site Failover

1. Verify DR site infrastructure is healthy.
2. Update DNS to point to DR site load balancers.
3. Start platform services on DR site.
4. Verify consumers resume from replicated Kafka offsets.
5. Monitor for duplicate processing (idempotent activities handle this).
6. Confirm end-to-end message flow.

#### Cassandra Node Recovery

1. Replace failed node hardware or VM.
2. Bootstrap new Cassandra node into the cluster.
3. Wait for streaming to complete (data replication from surviving nodes).
4. Run `nodetool repair` to ensure consistency.
5. Verify node is UN in `nodetool status`.

#### Kafka Broker Recovery

1. Replace failed broker hardware or VM.
2. Start Kafka broker with same broker ID and configuration.
3. Broker automatically rejoins cluster and begins replicating.
4. Monitor under-replicated partitions until they reach zero.

## Maintenance Windows

### Scheduled Maintenance

- **Cassandra repair:** Weekly, rolling (one node at a time), off-peak hours.
- **Kafka rebalance:** As needed after adding/removing brokers.
- **Platform service upgrades:** Rolling deployments with zero downtime.
- **Temporal server upgrades:** Follow Temporal's rolling upgrade procedure.

### Maintenance Checklist

- [ ] Notify stakeholders of maintenance window.
- [ ] Verify backups are current.
- [ ] Perform maintenance on one component at a time.
- [ ] Verify health checks pass after each change.
- [ ] Monitor for 30 minutes after maintenance completes.
- [ ] Close maintenance window and notify stakeholders.
