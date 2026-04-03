namespace Performance.Profiling;

/// <summary>
/// Point-in-time memory utilization metrics for the current process.
/// </summary>
public sealed record MemorySnapshot
{
    /// <summary>Working set of the process in bytes.</summary>
    public required long WorkingSetBytes { get; init; }

    /// <summary>Private memory size of the process in bytes.</summary>
    public required long PrivateMemoryBytes { get; init; }

    /// <summary>Total managed heap size reported by the GC in bytes.</summary>
    public required long ManagedHeapSizeBytes { get; init; }

    /// <summary>Total allocated bytes since process start.</summary>
    public required long TotalAllocatedBytes { get; init; }

    /// <summary>Allocation rate in bytes per second since the previous snapshot. Null for the first snapshot.</summary>
    public double? AllocationRateBytesPerSecond { get; init; }
}
