# Tutorial 45 — Performance Profiling

Profile CPU, memory, and GC performance to identify bottlenecks under load.

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

## Exercises

### 1. ContinuousProfiler — CaptureSnapshot WithLabel

```csharp
var profiler = new ContinuousProfiler(
    NullLogger<ContinuousProfiler>.Instance,
    Options.Create(new ProfilingOptions()));

var snapshot = profiler.CaptureSnapshot("baseline");

Assert.That(snapshot, Is.Not.Null);
Assert.That(snapshot.Label, Is.EqualTo("baseline"));
Assert.That(snapshot.SnapshotId, Is.Not.Null.And.Not.Empty);
Assert.That(snapshot.Cpu, Is.Not.Null);
Assert.That(snapshot.Memory, Is.Not.Null);
Assert.That(snapshot.Gc, Is.Not.Null);
```

### 2. ContinuousProfiler — SnapshotCount Increments

```csharp
var profiler = new ContinuousProfiler(
    NullLogger<ContinuousProfiler>.Instance,
    Options.Create(new ProfilingOptions()));

Assert.That(profiler.SnapshotCount, Is.EqualTo(0));

profiler.CaptureSnapshot();
Assert.That(profiler.SnapshotCount, Is.EqualTo(1));

profiler.CaptureSnapshot();
profiler.CaptureSnapshot();
Assert.That(profiler.SnapshotCount, Is.EqualTo(3));
```

### 3. ContinuousProfiler — GetLatestSnapshot ReturnsLastCaptured

```csharp
var profiler = new ContinuousProfiler(
    NullLogger<ContinuousProfiler>.Instance,
    Options.Create(new ProfilingOptions()));

Assert.That(profiler.GetLatestSnapshot(), Is.Null);

profiler.CaptureSnapshot("first");
profiler.CaptureSnapshot("second");
var latest = profiler.CaptureSnapshot("third");

var retrieved = profiler.GetLatestSnapshot();
Assert.That(retrieved, Is.Not.Null);
Assert.That(retrieved!.SnapshotId, Is.EqualTo(latest.SnapshotId));
Assert.That(retrieved.Label, Is.EqualTo("third"));
```

### 4. AllocationHotspotDetector — RegisterAndGetOperationStats

```csharp
var detector = new AllocationHotspotDetector(
    NullLogger<AllocationHotspotDetector>.Instance,
    Options.Create(new ProfilingOptions()));

detector.RegisterOperation("ProcessOrder", TimeSpan.FromMilliseconds(100), 1024);
detector.RegisterOperation("ProcessOrder", TimeSpan.FromMilliseconds(200), 2048);

var stats = detector.GetOperationStats("ProcessOrder");

Assert.That(stats, Is.Not.Null);
Assert.That(stats!.OperationName, Is.EqualTo("ProcessOrder"));
Assert.That(stats.InvocationCount, Is.EqualTo(2));
Assert.That(stats.AverageDuration, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
Assert.That(stats.MaxDuration, Is.EqualTo(TimeSpan.FromMilliseconds(200)));
Assert.That(stats.MinDuration, Is.EqualTo(TimeSpan.FromMilliseconds(100)));
Assert.That(stats.TotalAllocatedBytes, Is.EqualTo(3072));
```

### 5. InMemoryBenchmarkRegistry — RegisterAndGetBaseline

```csharp
var registry = new InMemoryBenchmarkRegistry(
    NullLogger<InMemoryBenchmarkRegistry>.Instance);

var baseline = new BenchmarkBaseline
{
    BenchmarkName = "SerializeOrder",
    MeanDuration = TimeSpan.FromMilliseconds(5),
    MeanAllocatedBytes = 4096,
    Iterations = 1000,
    RecordedAt = DateTimeOffset.UtcNow,
};

registry.RegisterBaseline(baseline);

var retrieved = registry.GetBaseline("SerializeOrder");
Assert.That(retrieved, Is.Not.Null);
Assert.That(retrieved!.BenchmarkName, Is.EqualTo("SerializeOrder"));
Assert.That(retrieved.MeanDuration, Is.EqualTo(TimeSpan.FromMilliseconds(5)));
Assert.That(retrieved.MeanAllocatedBytes, Is.EqualTo(4096));
Assert.That(retrieved.Iterations, Is.EqualTo(1000));
Assert.That(retrieved.RegressionThresholdPercent, Is.EqualTo(20.0));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial45/Lab.cs`](../tests/TutorialLabs/Tutorial45/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial45.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial45/Exam.cs`](../tests/TutorialLabs/Tutorial45/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial45.Exam"
```

---

**Previous: [← Tutorial 44](44-disaster-recovery.md)** | **Next: [Tutorial 46 →](46-complete-integration.md)**
