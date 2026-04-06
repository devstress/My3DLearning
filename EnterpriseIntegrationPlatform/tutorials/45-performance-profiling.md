# Tutorial 45 — Performance Profiling

## What You'll Learn

- The `Performance.Profiling` module in `src/Performance.Profiling/`
- CPU and memory profiling with snapshot capture
- GC tuning: Server GC mode and LOH compaction strategies
- Running benchmarks via the `LoadTests/` project
- Admin.Api profiling endpoints for production diagnostics
- Integrating `dotnet-counters` and `dotnet-trace` into your workflow

## Profiling Architecture

```
┌────────────────────────────────────────────────────────┐
│                   EIP Runtime                          │
│                                                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Gateway.Api  │  │ Workers      │  │ Temporal     │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │
│         │                 │                 │          │
│         ▼                 ▼                 ▼          │
│  ┌─────────────────────────────────────────────────┐   │
│  │          Performance.Profiling Module            │   │
│  │  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │   │
│  │  │Continuous│ │GcMonitor │ │ GC Diagnostics  │ │   │
│  │  │ Profiler │ │          │ │                 │ │   │
│  │  └──────────┘ └──────────┘ └─────────────────┘ │   │
│  └─────────────────────────────────────────────────┘   │
│         │                                              │
│         ▼                                              │
│  ┌─────────────────┐                                   │
│  │ Snapshot Store   │  .nettrace / .gcdump files       │
│  └─────────────────┘                                   │
└────────────────────────────────────────────────────────┘
```

## CPU Profiling Snapshots

Capture CPU and runtime profiling snapshots to identify hot paths:

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

## Memory Profiling

Track GC activity and detect memory issues:

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

## GC Tuning

### Server GC

Enable Server GC for multi-core throughput in `runtimeconfig.json`:

```json
{
  "runtimeOptions": {
    "configProperties": {
      "System.GC.Server": true,
      "System.GC.Concurrent": true
    }
  }
}
```

### LOH Compaction

Large Object Heap fragmentation can cause memory pressure:

```csharp
// Trigger LOH compaction during maintenance windows
GCSettings.LargeObjectHeapCompactionMode =
    GCLargeObjectHeapCompactionMode.CompactOnce;
GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
```

```
┌──────────────────────────────────────┐
│            Managed Heap              │
│  ┌─────────┐ ┌─────┐ ┌─────┐       │
│  │  Gen 0   │ │Gen 1│ │Gen 2│       │
│  │ (short)  │ │     │ │     │       │
│  └─────────┘ └─────┘ └─────┘       │
│  ┌──────────────────────────┐       │
│  │   Large Object Heap      │       │
│  │   (> 85KB allocations)   │       │
│  │   ████░░░████░░░████     │ ← fragmented
│  └──────────────────────────┘       │
└──────────────────────────────────────┘
```

## Benchmarks via LoadTests

The `LoadTests/` project measures throughput and latency:

```bash
cd tests/LoadTests
dotnet run -c Release -- --warmup 10 --duration 60 --concurrent 50
```

Key metrics captured:
- **Throughput**: messages/second through the pipeline
- **P50/P95/P99 latency**: end-to-end processing time
- **Error rate**: percentage of failed message deliveries
- **GC pause time**: impact of garbage collection on latency

## Admin.Api Profiling Endpoints

```http
GET  /api/admin/profiling/status        # Current profiling state
POST /api/admin/profiling/cpu/start     # Begin CPU profiling
POST /api/admin/profiling/cpu/stop      # Stop and download snapshot
POST /api/admin/profiling/memory/snap   # Capture heap snapshot
GET  /api/admin/profiling/gc/stats      # GC generation statistics
```

## dotnet-counters and dotnet-trace

### Live Monitoring with dotnet-counters

```bash
dotnet-counters monitor --process-id $(pidof Gateway.Api) \
    --counters System.Runtime,Microsoft.AspNetCore.Hosting
```

### Trace Capture with dotnet-trace

```bash
dotnet-trace collect --process-id $(pidof Gateway.Api) \
    --providers Microsoft-DotNETCore-SampleProfiler \
    --duration 00:00:30
```

Analyze the resulting `.nettrace` file in Visual Studio, PerfView, or
Speedscope.

## Scalability Dimension

Profiling identifies bottlenecks that limit horizontal scaling. If a worker's
CPU profile shows contention on a shared lock, that lock becomes the scaling
ceiling regardless of how many pod replicas the HPA provisions.

## Atomicity Dimension

GC pauses can cause broker acknowledgment timeouts, leading to duplicate message
delivery. Server GC with concurrent mode reduces pause duration, keeping the
Ack/Nack cycle within configured timeout windows.

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial45/Lab.cs`](../tests/TutorialLabs/Tutorial45/Lab.cs)

**Objective:** Use profiling tools to identify performance bottlenecks, analyze GC behavior under load, and design optimization strategies for **scalable** high-throughput message processing.

### Step 1: Monitor GC Behavior Under Load

Use `dotnet-counters` to monitor Gen 0/1/2 collection rates:

```bash
dotnet-counters monitor --process-id <pid> --counters System.Runtime
```

Observe the Gen 0 to Gen 2 ratio during a load test:

| Metric | Healthy | Unhealthy |
|--------|---------|-----------|
| Gen 0 collections | 100/min | 100/min |
| Gen 1 collections | 10/min | 50/min |
| Gen 2 collections | 1/min | 20/min |
| Gen 0:Gen 2 ratio | 100:1 | 5:1 |

A low Gen 0:Gen 2 ratio indicates objects surviving to older generations — a sign of memory pressure. Open `src/Performance.Profiling/GcMonitor.cs` and trace: How does the platform track these metrics?

### Step 2: Identify CPU Hotspots

Capture a CPU profile of the pipeline worker under load:

```bash
dotnet-trace collect --process-id <pid> --duration 00:00:30
```

Open the trace in a flame graph viewer. Identify likely hotspots:

| Method | CPU % (expected) | Optimization |
|--------|-----------------|-------------|
| JSON serialization | 30-40% | Pool JsonSerializerOptions |
| Regex evaluation (routing) | 15-20% | Pre-compile patterns (already done) |
| HTTP connector I/O wait | 20-30% | Connection pooling |
| GC pauses | 5-10% | Reduce allocations |

Open `src/Performance.Profiling/AllocationHotspotDetector.cs` and trace how the platform detects allocation-heavy code paths.

### Step 3: Analyze LOH Compaction Impact on Latency

Large Object Heap (LOH) compaction trades latency for memory efficiency:

| Metric | Without Compaction | With Compaction |
|--------|-------------------|----------------|
| P50 latency | 5ms | 5ms |
| P99 latency | 15ms | 50ms (GC pause spikes) |
| Memory usage | Growing (fragmentation) | Stable |

Design a profiling experiment to measure this trade-off. When is LOH compaction worth the latency cost?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial45/Exam.cs`](../tests/TutorialLabs/Tutorial45/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 44](44-disaster-recovery.md)** | **Next: [Tutorial 46 →](46-complete-integration.md)**
