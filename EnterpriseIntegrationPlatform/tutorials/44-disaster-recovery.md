# Tutorial 44 — Disaster Recovery

## What You'll Learn

- The `DisasterRecovery` module in `src/DisasterRecovery/`
- Failover strategies for active-passive and active-active topologies
- Replication configuration for Cassandra, Kafka, and NATS
- Defining RPO and RTO targets for integration workloads
- Automating DR drills and validating recovery procedures
- Admin.Api endpoints for DR status and failover triggers

## Architecture Overview

```
┌─────────────────────────────┐    ┌─────────────────────────────┐
│        PRIMARY REGION       │    │       SECONDARY REGION      │
│                             │    │                             │
│  Gateway.Api  Workers       │    │  Gateway.Api  Workers       │
│       │          │          │    │       │          │          │
│  ┌────▼──────────▼────┐    │    │  ┌────▼──────────▼────┐    │
│  │  Kafka (Leader)    │────┼────┼─▶│  Kafka (Follower)  │    │
│  └────────────────────┘    │    │  └────────────────────┘    │
│  ┌────────────────────┐    │    │  ┌────────────────────┐    │
│  │  Cassandra (RF=3)  │────┼────┼─▶│  Cassandra (RF=3)  │    │
│  └────────────────────┘    │    │  └────────────────────┘    │
│  ┌────────────────────┐    │    │  ┌────────────────────┐    │
│  │  NATS Cluster      │────┼────┼─▶│  NATS Cluster      │    │
│  └────────────────────┘    │    │  └────────────────────┘    │
└─────────────────────────────┘    └─────────────────────────────┘
```

## DisasterRecovery Module

The `src/DisasterRecovery/` project provides three core interfaces:

```csharp
// src/DisasterRecovery/IFailoverManager.cs
public interface IFailoverManager
{
    Task RegisterRegionAsync(RegionInfo region, CancellationToken cancellationToken = default);
    Task<RegionInfo?> GetPrimaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegionInfo>> GetAllRegionsAsync(CancellationToken cancellationToken = default);
    Task<FailoverResult> FailoverAsync(string targetRegionId, CancellationToken cancellationToken = default);
    Task<FailoverResult> FailbackAsync(string originalPrimaryRegionId, CancellationToken cancellationToken = default);
    Task UpdateHealthCheckAsync(string regionId, CancellationToken cancellationToken = default);
}
```

## Failover Strategies

| Strategy        | RPO      | RTO       | Use Case                    |
|----------------|----------|----------|-----------------------------|
| Active-Passive | < 1 min  | 5–15 min | Cost-sensitive workloads    |
| Active-Active  | ~ 0      | < 1 min  | Mission-critical pipelines  |
| Pilot Light    | < 5 min  | 15–30 min| Low-traffic DR standby      |

## Replication Configuration

### Cassandra Replication Factor

```cql
CREATE KEYSPACE eip WITH replication = {
  'class': 'NetworkTopologyStrategy',
  'dc-primary': 3,
  'dc-secondary': 3
};
```

### Kafka Replication

```properties
default.replication.factor=3
min.insync.replicas=2
unclean.leader.election.enable=false
```

### NATS Clustering

```
nats-server --cluster nats://0.0.0.0:6222 \
            --routes nats://nats-1:6222,nats://nats-2:6222
```

NATS JetStream replicates streams across the cluster for durability.

## RPO / RTO Targets

```
 RPO ◄──────────────────── time ──────────────────────► RTO
 │                          │                            │
 │  Max data loss           │  Disaster                  │  Service restored
 │  (last ack'd message)    │  occurs                    │  (consumers active)
```

- **RPO (Recovery Point Objective)**: Maximum acceptable data loss, measured in
  time. With synchronous Kafka replication (`min.insync.replicas=2`), RPO ≈ 0.
- **RTO (Recovery Time Objective)**: Maximum acceptable downtime. Active-active
  achieves RTO < 1 minute; active-passive targets 5–15 minutes.

## Admin.Api DR Endpoints

```http
GET  /api/admin/dr/status          # Current DR posture and region health
POST /api/admin/dr/failover        # Trigger manual failover
POST /api/admin/dr/drill           # Initiate automated DR drill
GET  /api/admin/dr/drill/history   # Past drill results and metrics
```

## DR Drill Automation

Automated drills validate recovery without production impact:

```csharp
// src/DisasterRecovery/IDrDrillRunner.cs
public interface IDrDrillRunner
{
    Task<DrDrillResult> RunDrillAsync(
        DrDrillScenario scenario, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DrDrillResult>> GetDrillHistoryAsync(
        int limit = 50, CancellationToken cancellationToken = default);
    Task<DrDrillResult?> GetLastDrillResultAsync(CancellationToken cancellationToken = default);
}
```

## Scalability Dimension

DR replication distributes load across regions. During normal operation, read
traffic can be served from the secondary region, effectively doubling read
capacity. Active-active deployments balance writes across regions.

## Atomicity Dimension

Synchronous replication (`min.insync.replicas=2`) ensures that every acknowledged
message exists on at least two brokers before the producer receives confirmation.
This guarantees no acknowledged message is lost during failover.

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial44/Lab.cs`](../tests/TutorialLabs/Tutorial44/Lab.cs)

**Objective:** Calculate RPO/RTO for different replication configurations, design a DR drill for Cassandra failover, and analyze broker replication trade-offs for **atomic** message durability.

### Step 1: Calculate RPO Under Different Configurations

| Configuration | RPO | Risk |
|--------------|-----|------|
| Kafka: `min.insync.replicas=1`, async replication | ? | Messages not yet replicated are lost |
| Kafka: `min.insync.replicas=2`, sync replication | 0 (zero data loss) | Higher write latency |
| NATS JetStream: `num_replicas=3` | ? | Depends on quorum writes |

Calculate: With `min.insync.replicas=1` and async replication, if the primary broker fails with 500ms of unreplicated data at 10,000 msg/s, how many messages could be lost?

### Step 2: Design a DR Drill for Cassandra

Design a complete DR drill that tests Cassandra failover:

```
1. Record current data state: SELECT count(*) FROM message_state WHERE status='delivered'
2. Trigger failover: Promote secondary Cassandra to primary
3. Verify data consistency:
   - Re-run count query on new primary
   - Compare with pre-failover count
   - Check for data divergence in recent writes
4. Test write path: Insert a test record and verify replication
5. Verify application reconnection: Check application logs for successful reconnect
```

What **atomicity** guarantees does Cassandra's eventual consistency model provide during failover? What data could be lost?

### Step 3: Analyze Broker Replication Trade-Offs

NATS JetStream's `num_replicas` setting affects both durability and write latency:

| num_replicas | Durability | Write Latency | Network Cost |
|-------------|-----------|---------------|-------------|
| 1 | Low (single point of failure) | ~1ms | Minimal |
| 3 | High (survives 1 node failure) | ~5ms | 3x network |
| 5 | Very high (survives 2 failures) | ~10ms | 5x network |

For notification delivery (Tutorial 48), which trade-off would you choose? Why?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial44/Exam.cs`](../tests/TutorialLabs/Tutorial44/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 43](43-kubernetes-deployment.md)** | **Next: [Tutorial 45 →](45-performance-profiling.md)**
