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

## Exercises

1. Calculate the RPO if Kafka is configured with `min.insync.replicas=1` and
   asynchronous replication. What messages could be lost?

2. Design a DR drill that tests Cassandra failover. What queries would you run
   to verify data consistency after the secondary becomes primary?

3. How does NATS JetStream's `num_replicas` setting affect both durability and
   write latency? What trade-off would you choose for notification delivery?

**Previous: [← Tutorial 43](43-kubernetes-deployment.md)** | **Next: [Tutorial 45 →](45-performance-profiling.md)**
