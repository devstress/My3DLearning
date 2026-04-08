# Tutorial 45 — Performance Profiling

Profile CPU, memory, and GC performance to identify bottlenecks under load.

## Learning Objectives

After completing this tutorial you will be able to:

1. Capture performance snapshots and publish metrics to a broker endpoint
2. Track snapshot counts and retrieve the latest snapshot by label
3. Query snapshots by time range with filtered results
4. Attach labels and metadata to profiling snapshots
5. Enforce max-retention eviction of oldest snapshots

## Key Types

```csharp
public sealed class ContinuousProfiler : IContinuousProfiler
{
    public ContinuousProfiler(ILogger<ContinuousProfiler> logger, IOptions<ProfilingOptions> options) { /* ... */ }

    public ProfileSnapshot CaptureSnapshot(string? label = null)
    {
        // Captures a point-in-time snapshot with nested Cpu, Memory, and Gc sub-objects
        return new ProfileSnapshot
        {
            SnapshotId = Guid.NewGuid().ToString("N"),
            CapturedAt = DateTimeOffset.UtcNow,
            Cpu = new CpuSnapshot { /* CpuUsagePercent, ThreadCount, ... */ },
            Memory = new MemorySnapshot { /* WorkingSetBytes, ManagedHeapSizeBytes, ... */ },
            Gc = new GcSnapshot { /* Gen0Collections, Gen1Collections, Gen2Collections, ... */ },
            Label = label
        };
    }

    public IReadOnlyList<ProfileSnapshot> GetSnapshots(DateTimeOffset from, DateTimeOffset to) { /* ... */ }
    public ProfileSnapshot? GetLatestSnapshot() { /* ... */ }
}
```

```csharp
public sealed class GcMonitor : IGcMonitor
{
    public GcSnapshot CaptureSnapshot()
    {
        return new GcSnapshot
        {
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            TotalCommittedBytes = GC.GetTotalMemory(forceFullCollection: false),
            IsServerGc = GCSettings.IsServerGC,
            // ... additional properties: heap sizes, fragmentation, pause times
        };
    }

    public IReadOnlyList<GcTuningRecommendation> GetRecommendations() { /* ... */ }
    public IReadOnlyList<GcSnapshot> GetHistory() { /* ... */ }
    public void ClearHistory() { /* ... */ }
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `CaptureSnapshot_PublishMetricsToNatsBrokerEndpoint` | Capture and publish performance snapshot |
| 2 | `SnapshotCount_Increments_PublishCount` | Snapshot count increments |
| 3 | `GetLatestSnapshot_PublishLabel` | Get latest snapshot by label |
| 4 | `GetSnapshotsByTimeRange_PublishFiltered` | Query snapshots by time range |
| 5 | `LabelledSnapshots_PublishWithMetadata` | Labelled snapshots with metadata |
| 6 | `MaxRetention_EvictsOldest_PublishCurrent` | Max retention evicts oldest snapshot |

> 💻 [`tests/TutorialLabs/Tutorial45/Lab.cs`](../tests/TutorialLabs/Tutorial45/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial45.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_MultipleSnapshots_TimeRangeQuery_PublishAnalysis` | 🟢 Starter | Multiple snapshots with time-range query and analysis |
| 2 | `Challenge2_SnapshotDeltaMetrics_CpuUsageTracking` | 🟡 Intermediate | Snapshot delta metrics — CPU usage tracking |
| 3 | `Challenge3_ProfilingSessionLifecycle_PublishAllSnapshots` | 🔴 Advanced | Profiling session lifecycle — publish all snapshots |

> 💻 [`tests/TutorialLabs/Tutorial45/Exam.cs`](../tests/TutorialLabs/Tutorial45/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial45.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial45.ExamAnswers"
```

---

**Previous: [← Tutorial 44](44-disaster-recovery.md)** | **Next: [Tutorial 46 →](46-complete-integration.md)**
