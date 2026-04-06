# Tutorial 44 — Disaster Recovery

Handle disaster recovery with failover regions, backup/restore, and replication.

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

## Exercises

### 1. FailoverResult — RecordShape

```csharp
var now = DateTimeOffset.UtcNow;
var result = new FailoverResult
{
    Success = true,
    PromotedRegionId = "us-west-2",
    DemotedRegionId = "us-east-1",
    Duration = TimeSpan.FromMilliseconds(150),
    CompletedAt = now,
};

Assert.That(result.Success, Is.True);
Assert.That(result.PromotedRegionId, Is.EqualTo("us-west-2"));
Assert.That(result.DemotedRegionId, Is.EqualTo("us-east-1"));
Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
Assert.That(result.CompletedAt, Is.EqualTo(now));
Assert.That(result.ErrorMessage, Is.Null);
```

### 2. ReplicationStatus — RecordShape

```csharp
var now = DateTimeOffset.UtcNow;
var status = new ReplicationStatus
{
    SourceRegionId = "us-east-1",
    TargetRegionId = "eu-west-1",
    Lag = TimeSpan.FromSeconds(5),
    PendingItems = 42,
    IsHealthy = true,
    CapturedAt = now,
    LastReplicatedSequence = 1000,
};

Assert.That(status.SourceRegionId, Is.EqualTo("us-east-1"));
Assert.That(status.TargetRegionId, Is.EqualTo("eu-west-1"));
Assert.That(status.Lag, Is.EqualTo(TimeSpan.FromSeconds(5)));
Assert.That(status.PendingItems, Is.EqualTo(42));
Assert.That(status.IsHealthy, Is.True);
Assert.That(status.LastReplicatedSequence, Is.EqualTo(1000));
```

### 3. DrDrillType — EnumValues

```csharp
var values = Enum.GetValues<DrDrillType>();

Assert.That(values, Does.Contain(DrDrillType.RegionFailure));
Assert.That(values, Does.Contain(DrDrillType.NetworkPartition));
Assert.That(values, Does.Contain(DrDrillType.StorageFailure));
Assert.That(values, Does.Contain(DrDrillType.BrokerFailure));
Assert.That(values, Does.Contain(DrDrillType.PlannedFailover));
Assert.That(values, Has.Length.EqualTo(5));
```

### 4. InMemoryFailoverManager — RegisterAndGetRegions

```csharp
var manager = new InMemoryFailoverManager(
    NullLogger<InMemoryFailoverManager>.Instance,
    Options.Create(new DisasterRecoveryOptions()));

await manager.RegisterRegionAsync(new RegionInfo
{
    RegionId = "us-east-1",
    DisplayName = "US East",
    State = FailoverState.Primary,
});

await manager.RegisterRegionAsync(new RegionInfo
{
    RegionId = "eu-west-1",
    DisplayName = "EU West",
    State = FailoverState.Standby,
});

var regions = await manager.GetAllRegionsAsync();
Assert.That(regions, Has.Count.EqualTo(2));

var primary = await manager.GetPrimaryAsync();
Assert.That(primary, Is.Not.Null);
Assert.That(primary!.RegionId, Is.EqualTo("us-east-1"));
Assert.That(primary.IsPrimary, Is.True);
```

### 5. InMemoryFailoverManager — Failover PromotesTargetRegion

```csharp
var manager = new InMemoryFailoverManager(
    NullLogger<InMemoryFailoverManager>.Instance,
    Options.Create(new DisasterRecoveryOptions()));

await manager.RegisterRegionAsync(new RegionInfo
{
    RegionId = "us-east-1",
    DisplayName = "US East",
    State = FailoverState.Primary,
});

await manager.RegisterRegionAsync(new RegionInfo
{
    RegionId = "us-west-2",
    DisplayName = "US West",
    State = FailoverState.Standby,
});

var result = await manager.FailoverAsync("us-west-2");

Assert.That(result.Success, Is.True);
Assert.That(result.PromotedRegionId, Is.EqualTo("us-west-2"));
Assert.That(result.DemotedRegionId, Is.EqualTo("us-east-1"));

var newPrimary = await manager.GetPrimaryAsync();
Assert.That(newPrimary!.RegionId, Is.EqualTo("us-west-2"));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial44/Lab.cs`](../tests/TutorialLabs/Tutorial44/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial44.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial44/Exam.cs`](../tests/TutorialLabs/Tutorial44/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial44.Exam"
```

---

**Previous: [← Tutorial 43](43-kubernetes-deployment.md)** | **Next: [Tutorial 45 →](45-performance-profiling.md)**
