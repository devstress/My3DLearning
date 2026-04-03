using System.Runtime;

namespace Performance.Profiling;

/// <summary>
/// Point-in-time garbage collection metrics for the current process.
/// </summary>
public sealed record GcSnapshot
{
    /// <summary>Number of Gen 0 collections since process start.</summary>
    public required int Gen0Collections { get; init; }

    /// <summary>Number of Gen 1 collections since process start.</summary>
    public required int Gen1Collections { get; init; }

    /// <summary>Number of Gen 2 collections since process start.</summary>
    public required int Gen2Collections { get; init; }

    /// <summary>Heap size of Gen 0 in bytes.</summary>
    public required long Gen0HeapSizeBytes { get; init; }

    /// <summary>Heap size of Gen 1 in bytes.</summary>
    public required long Gen1HeapSizeBytes { get; init; }

    /// <summary>Heap size of Gen 2 in bytes.</summary>
    public required long Gen2HeapSizeBytes { get; init; }

    /// <summary>Large object heap size in bytes.</summary>
    public required long LargeObjectHeapSizeBytes { get; init; }

    /// <summary>Pinned object heap size in bytes.</summary>
    public required long PinnedObjectHeapSizeBytes { get; init; }

    /// <summary>Total committed memory in bytes (all heaps).</summary>
    public required long TotalCommittedBytes { get; init; }

    /// <summary>Heap fragmentation ratio (0.0–1.0). Higher values indicate more fragmentation.</summary>
    public required double FragmentationRatio { get; init; }

    /// <summary>Total pause duration from GC since process start.</summary>
    public required TimeSpan TotalPauseDuration { get; init; }

    /// <summary>Percentage of time spent in GC pauses (0.0–100.0).</summary>
    public required double PauseTimePercentage { get; init; }

    /// <summary>Whether the GC is running in server mode.</summary>
    public required bool IsServerGc { get; init; }

    /// <summary>Current GC latency mode.</summary>
    public required GCLatencyMode LatencyMode { get; init; }
}
