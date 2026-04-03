using System.Diagnostics;
using System.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Performance.Profiling;

/// <summary>
/// Monitors garbage collection behavior and provides tuning recommendations
/// based on observed GC patterns. Tracks GC snapshot history for trend analysis.
/// Thread-safe for concurrent snapshot capture.
/// </summary>
public sealed class GcMonitor : IGcMonitor
{
    private readonly ILogger<GcMonitor> _logger;
    private readonly ProfilingOptions _options;
    private readonly List<GcSnapshot> _history = [];
    private readonly object _historyLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="GcMonitor"/>.
    /// </summary>
    public GcMonitor(ILogger<GcMonitor> logger, IOptions<ProfilingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public GcSnapshot CaptureSnapshot()
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

        var totalPause = GC.GetTotalPauseDuration();
        var uptime = DateTimeOffset.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        var pausePercent = uptime.TotalMilliseconds > 0
            ? (totalPause.TotalMilliseconds / uptime.TotalMilliseconds) * 100.0
            : 0.0;

        var snapshot = new GcSnapshot
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

        lock (_historyLock)
        {
            _history.Add(snapshot);

            // Bound history size
            while (_history.Count > _options.MaxRetainedSnapshots)
            {
                _history.RemoveAt(0);
            }
        }

        _logger.LogDebug(
            "GC snapshot captured: Gen0={Gen0}, Gen1={Gen1}, Gen2={Gen2}, Fragmentation={Frag:P1}, PauseTime={Pause:P2}",
            snapshot.Gen0Collections,
            snapshot.Gen1Collections,
            snapshot.Gen2Collections,
            snapshot.FragmentationRatio,
            snapshot.PauseTimePercentage / 100.0);

        return snapshot;
    }

    /// <inheritdoc />
    public IReadOnlyList<GcTuningRecommendation> GetRecommendations()
    {
        GcSnapshot? latest;
        GcSnapshot? earliest;
        int historyCount;

        lock (_historyLock)
        {
            historyCount = _history.Count;
            latest = historyCount > 0 ? _history[^1] : null;
            earliest = historyCount > 1 ? _history[0] : null;
        }

        if (latest is null)
        {
            _logger.LogDebug("No GC snapshots available for recommendations");
            return [];
        }

        var recommendations = new List<GcTuningRecommendation>();

        // 1. Server GC recommendation for high-throughput workloads
        if (!latest.IsServerGc)
        {
            recommendations.Add(new GcTuningRecommendation
            {
                Category = "ServerGC",
                Severity = HotspotSeverity.Info,
                Description = "Workstation GC is active. For server workloads with multiple cores, Server GC provides better throughput.",
                CurrentValue = "Workstation GC",
                RecommendedAction = "Set <ServerGarbageCollection>true</ServerGarbageCollection> in the project file for server workloads"
            });
        }

        // 2. High fragmentation check
        if (latest.FragmentationRatio > 0.30)
        {
            var severity = latest.FragmentationRatio > 0.50
                ? HotspotSeverity.Critical
                : HotspotSeverity.Warning;

            recommendations.Add(new GcTuningRecommendation
            {
                Category = "Fragmentation",
                Severity = severity,
                Description = $"Heap fragmentation is {latest.FragmentationRatio:P1}. High fragmentation increases memory usage and GC pressure.",
                CurrentValue = $"{latest.FragmentationRatio:P1}",
                RecommendedAction = "Reduce pinned objects and large temporary allocations. Consider object pooling for frequently allocated objects."
            });
        }

        // 3. Gen2 pressure — too many Gen2 collections
        if (earliest is not null)
        {
            var gen2Delta = latest.Gen2Collections - earliest.Gen2Collections;
            var gen0Delta = latest.Gen0Collections - earliest.Gen0Collections;

            if (gen0Delta > 0)
            {
                var gen2Ratio = (double)gen2Delta / gen0Delta;

                if (gen2Ratio > 0.10)
                {
                    var severity = gen2Ratio > 0.25
                        ? HotspotSeverity.Critical
                        : HotspotSeverity.Warning;

                    recommendations.Add(new GcTuningRecommendation
                    {
                        Category = "Gen2Pressure",
                        Severity = severity,
                        Description = $"Gen2/Gen0 collection ratio is {gen2Ratio:P1}. High Gen2 pressure indicates too many long-lived objects or LOH allocations.",
                        CurrentValue = $"Gen2/Gen0 ratio: {gen2Ratio:P1} ({gen2Delta} Gen2 / {gen0Delta} Gen0)",
                        RecommendedAction = "Reduce large object allocations (>85KB). Use ArrayPool<T> or MemoryPool<T> for buffer reuse."
                    });
                }
            }
        }

        // 4. Pause time percentage
        if (latest.PauseTimePercentage > 5.0)
        {
            var severity = latest.PauseTimePercentage > 10.0
                ? HotspotSeverity.Critical
                : HotspotSeverity.Warning;

            recommendations.Add(new GcTuningRecommendation
            {
                Category = "PauseTime",
                Severity = severity,
                Description = $"GC pause time is {latest.PauseTimePercentage:F2}% of total runtime. High pause times impact latency-sensitive operations.",
                CurrentValue = $"{latest.PauseTimePercentage:F2}% ({latest.TotalPauseDuration.TotalMilliseconds:F0}ms total)",
                RecommendedAction = "Consider SustainedLowLatency or GCLatencyMode.LowLatency for latency-sensitive paths. Reduce allocation rate."
            });
        }

        // 5. LOH size check
        if (latest.LargeObjectHeapSizeBytes > 100 * 1024 * 1024) // > 100MB
        {
            var severity = latest.LargeObjectHeapSizeBytes > 500 * 1024 * 1024
                ? HotspotSeverity.Critical
                : HotspotSeverity.Warning;

            recommendations.Add(new GcTuningRecommendation
            {
                Category = "LargeObjectHeap",
                Severity = severity,
                Description = $"Large Object Heap is {latest.LargeObjectHeapSizeBytes / (1024.0 * 1024.0):F1}MB. LOH allocations cause Gen2 collections.",
                CurrentValue = $"{latest.LargeObjectHeapSizeBytes / (1024.0 * 1024.0):F1}MB",
                RecommendedAction = "Use ArrayPool<T>.Shared for large buffers. Avoid allocating arrays >85KB repeatedly."
            });
        }

        if (recommendations.Count > 0)
        {
            _logger.LogInformation(
                "Generated {Count} GC tuning recommendations ({Critical} critical, {Warning} warnings)",
                recommendations.Count,
                recommendations.Count(r => r.Severity == HotspotSeverity.Critical),
                recommendations.Count(r => r.Severity == HotspotSeverity.Warning));
        }

        return recommendations;
    }

    /// <inheritdoc />
    public IReadOnlyList<GcSnapshot> GetHistory()
    {
        lock (_historyLock)
        {
            return [.. _history];
        }
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        lock (_historyLock)
        {
            _history.Clear();
        }

        _logger.LogInformation("GC monitor history cleared");
    }
}
