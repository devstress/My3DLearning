using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Performance.Profiling;

/// <summary>
/// Production continuous profiler that captures periodic CPU, memory, and GC snapshots
/// using System.Diagnostics and GC APIs. Thread-safe with bounded snapshot retention.
/// </summary>
public sealed class ContinuousProfiler : IContinuousProfiler
{
    private readonly ILogger<ContinuousProfiler> _logger;
    private readonly ProfilingOptions _options;
    private readonly ConcurrentQueue<ProfileSnapshot> _snapshots = new();
    private readonly object _captureLock = new();

    private ProfileSnapshot? _previousSnapshot;
    private int _snapshotCount;

    /// <inheritdoc />
    public int SnapshotCount => _snapshotCount;

    /// <summary>
    /// Initializes a new instance of <see cref="ContinuousProfiler"/>.
    /// </summary>
    public ContinuousProfiler(ILogger<ContinuousProfiler> logger, IOptions<ProfilingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public ProfileSnapshot CaptureSnapshot(string? label = null)
    {
        lock (_captureLock)
        {
            var process = Process.GetCurrentProcess();
            var now = DateTimeOffset.UtcNow;
            var snapshotId = Guid.NewGuid().ToString("N");

            var cpuSnapshot = CaptureCpuSnapshot(process, _previousSnapshot, now);
            var memorySnapshot = CaptureMemorySnapshot(process, _previousSnapshot, now);
            var gcSnapshot = CaptureGcSnapshot();

            var snapshot = new ProfileSnapshot
            {
                SnapshotId = snapshotId,
                CapturedAt = now,
                Cpu = cpuSnapshot,
                Memory = memorySnapshot,
                Gc = gcSnapshot,
                Label = label
            };

            _snapshots.Enqueue(snapshot);
            Interlocked.Increment(ref _snapshotCount);

            // Evict oldest snapshots beyond retention limit
            while (_snapshotCount > _options.MaxRetainedSnapshots && _snapshots.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _snapshotCount);
            }

            _previousSnapshot = snapshot;

            _logger.LogDebug(
                "Profile snapshot {SnapshotId} captured: CPU={CpuPercent:F1}%, WorkingSet={WorkingSetMB:F1}MB, Gen2={Gen2}",
                snapshotId,
                cpuSnapshot.CpuUsagePercent ?? 0,
                memorySnapshot.WorkingSetBytes / (1024.0 * 1024.0),
                gcSnapshot.Gen2Collections);

            return snapshot;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ProfileSnapshot> GetSnapshots(DateTimeOffset from, DateTimeOffset to)
    {
        return _snapshots
            .Where(s => s.CapturedAt >= from && s.CapturedAt <= to)
            .OrderBy(s => s.CapturedAt)
            .ToList();
    }

    /// <inheritdoc />
    public ProfileSnapshot? GetLatestSnapshot()
    {
        lock (_captureLock)
        {
            return _previousSnapshot;
        }
    }

    private static CpuSnapshot CaptureCpuSnapshot(
        Process process, ProfileSnapshot? previous, DateTimeOffset now)
    {
        var totalCpu = process.TotalProcessorTime;
        var userCpu = process.UserProcessorTime;
        var privilegedCpu = process.PrivilegedProcessorTime;
        var threadCount = process.Threads.Count;

        double? cpuPercent = null;

        if (previous is not null)
        {
            var elapsed = now - previous.CapturedAt;
            if (elapsed.TotalMilliseconds > 0)
            {
                var cpuDelta = totalCpu - previous.Cpu.TotalProcessorTime;
                cpuPercent = (cpuDelta.TotalMilliseconds / elapsed.TotalMilliseconds / Environment.ProcessorCount) * 100.0;
                cpuPercent = Math.Max(0, Math.Min(100.0 * Environment.ProcessorCount, cpuPercent.Value));
            }
        }

        return new CpuSnapshot
        {
            TotalProcessorTime = totalCpu,
            UserProcessorTime = userCpu,
            PrivilegedProcessorTime = privilegedCpu,
            ThreadCount = threadCount,
            CpuUsagePercent = cpuPercent
        };
    }

    private static MemorySnapshot CaptureMemorySnapshot(
        Process process, ProfileSnapshot? previous, DateTimeOffset now)
    {
        var workingSet = process.WorkingSet64;
        var privateMemory = process.PrivateMemorySize64;
        var managedHeap = GC.GetTotalMemory(forceFullCollection: false);
        var totalAllocated = GC.GetTotalAllocatedBytes(precise: false);

        double? allocationRate = null;

        if (previous is not null)
        {
            var elapsed = now - previous.CapturedAt;
            if (elapsed.TotalSeconds > 0)
            {
                var allocDelta = totalAllocated - previous.Memory.TotalAllocatedBytes;
                allocationRate = allocDelta / elapsed.TotalSeconds;
            }
        }

        return new MemorySnapshot
        {
            WorkingSetBytes = workingSet,
            PrivateMemoryBytes = privateMemory,
            ManagedHeapSizeBytes = managedHeap,
            TotalAllocatedBytes = totalAllocated,
            AllocationRateBytesPerSecond = allocationRate
        };
    }

    private static GcSnapshot CaptureGcSnapshot()
    {
        var gcInfo = GC.GetGCMemoryInfo();

        var gen0Size = gcInfo.GenerationInfo.Length > 0 ? gcInfo.GenerationInfo[0].SizeAfterBytes : 0;
        var gen1Size = gcInfo.GenerationInfo.Length > 1 ? gcInfo.GenerationInfo[1].SizeAfterBytes : 0;
        var gen2Size = gcInfo.GenerationInfo.Length > 2 ? gcInfo.GenerationInfo[2].SizeAfterBytes : 0;
        var lohSize = gcInfo.GenerationInfo.Length > 3 ? gcInfo.GenerationInfo[3].SizeAfterBytes : 0;
        var pohSize = gcInfo.GenerationInfo.Length > 4 ? gcInfo.GenerationInfo[4].SizeAfterBytes : 0;

        var totalHeap = gcInfo.HeapSizeBytes;
        var fragmentation = totalHeap > 0
            ? (double)gcInfo.FragmentedBytes / totalHeap
            : 0.0;

        var pauseDuration = gcInfo.PauseDurations.Length > 0
            ? gcInfo.PauseDurations.ToArray().Aggregate(TimeSpan.Zero, (sum, d) => sum + d)
            : TimeSpan.Zero;

        var totalPause = GC.GetTotalPauseDuration();
        var uptime = DateTimeOffset.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        var pausePercent = uptime.TotalMilliseconds > 0
            ? (totalPause.TotalMilliseconds / uptime.TotalMilliseconds) * 100.0
            : 0.0;

        return new GcSnapshot
        {
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            Gen0HeapSizeBytes = gen0Size,
            Gen1HeapSizeBytes = gen1Size,
            Gen2HeapSizeBytes = gen2Size,
            LargeObjectHeapSizeBytes = lohSize,
            PinnedObjectHeapSizeBytes = pohSize,
            TotalCommittedBytes = gcInfo.TotalCommittedBytes,
            FragmentationRatio = fragmentation,
            TotalPauseDuration = totalPause,
            PauseTimePercentage = pausePercent,
            IsServerGc = GCSettings.IsServerGC,
            LatencyMode = GCSettings.LatencyMode
        };
    }
}
