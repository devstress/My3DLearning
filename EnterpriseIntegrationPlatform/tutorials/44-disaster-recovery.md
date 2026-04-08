# Tutorial 44 — Disaster Recovery

Handle disaster recovery with failover regions, backup/restore, and replication.

## Learning Objectives

After completing this tutorial you will be able to:

1. Register multi-region topology and publish it to a broker endpoint
2. Perform failover to promote a target region to primary
3. Handle failover errors for unknown or same-region targets
4. Fail back to restore the original primary region
5. Update health-check timestamps and query all region states

## Key Types

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

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `RegisterRegions_PublishTopologyToNatsBrokerEndpoint` | Register regions and publish topology |
| 2 | `Failover_PromotesTarget_PublishResult` | Failover promotes target region |
| 3 | `FailoverToUnknownRegion_PublishError` | Failover to unknown region publishes error |
| 4 | `FailoverToSameRegion_PublishError` | Failover to same region publishes error |
| 5 | `FailbackRestoresOriginalPrimary_PublishResult` | Failback restores original primary |
| 6 | `UpdateHealthCheck_PublishTimestampChange` | Health-check timestamp update |
| 7 | `GetAllRegions_PublishRegionStates` | Get all region states |

> 💻 [`tests/TutorialLabs/Tutorial44/Lab.cs`](../tests/TutorialLabs/Tutorial44/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial44.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullFailoverFailbackLifecycle_WithNatsBrokerEndpoint` | 🟢 Starter | Full failover → failback lifecycle with broker |
| 2 | `Challenge2_MultiRegionTopology_FailoverChain` | 🟡 Intermediate | Multi-region topology failover chain |
| 3 | `Challenge3_FailoverResultDetails_PublishAuditTrail` | 🔴 Advanced | Failover result details — publish audit trail |

> 💻 [`tests/TutorialLabs/Tutorial44/Exam.cs`](../tests/TutorialLabs/Tutorial44/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial44.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial44.ExamAnswers"
```

---

**Previous: [← Tutorial 43](43-kubernetes-deployment.md)** | **Next: [Tutorial 45 →](45-performance-profiling.md)**
