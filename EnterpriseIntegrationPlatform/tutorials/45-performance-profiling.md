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
│  │  │ CPU Prof │ │ Mem Prof │ │ GC Diagnostics  │ │   │
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

Capture CPU profiles to identify hot paths:

```csharp
public class CpuProfiler
{
    public async Task<ProfileSnapshot> CaptureAsync(
        TimeSpan duration, CancellationToken ct)
    {
        var session = new DiagnosticsSession(processId);
        session.EnableProvider("Microsoft-DotNETCore-SampleProfiler");
        await Task.Delay(duration, ct);
        session.Stop();
        return new ProfileSnapshot
        {
            FilePath = session.OutputPath,
            Duration = duration,
            TopMethods = AnalyzeHotPaths(session.OutputPath)
        };
    }
}
```

## Memory Profiling

Track allocations and detect leaks:

```csharp
public class MemoryProfiler
{
    public HeapSnapshot CaptureHeapSnapshot()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        return new HeapSnapshot
        {
            TotalBytes = GC.GetTotalMemory(forceFullCollection: false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            LargeObjectHeapSize = GC.GetGCMemoryInfo().HeapSizeBytes
        };
    }
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

## Exercises

1. Use `dotnet-counters` to monitor Gen 0/1/2 collection rates during a load
   test. What ratio of Gen 0 to Gen 2 collections indicates healthy GC behavior?

2. Capture a CPU profile of the pipeline worker under load. Identify the top 3
   methods by inclusive CPU time. Are any of them candidates for optimization?

3. What is the impact of enabling LOH compaction on P99 latency? Design a test
   that measures latency before and after compaction.

**Previous: [← Tutorial 44](44-disaster-recovery.md)** | **Next: [Tutorial 46 →](46-complete-integration.md)**
