namespace Performance.Profiling;

/// <summary>
/// Point-in-time CPU utilization metrics captured from the current process.
/// </summary>
public sealed record CpuSnapshot
{
    /// <summary>Total processor time consumed by the process at capture time.</summary>
    public required TimeSpan TotalProcessorTime { get; init; }

    /// <summary>User-mode processor time consumed by the process.</summary>
    public required TimeSpan UserProcessorTime { get; init; }

    /// <summary>Privileged (kernel-mode) processor time consumed by the process.</summary>
    public required TimeSpan PrivilegedProcessorTime { get; init; }

    /// <summary>Number of threads active in the process.</summary>
    public required int ThreadCount { get; init; }

    /// <summary>
    /// Estimated CPU usage percentage since the previous snapshot.
    /// Null when this is the first snapshot (no baseline for delta calculation).
    /// </summary>
    public double? CpuUsagePercent { get; init; }
}
