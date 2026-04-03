namespace Performance.Profiling;

/// <summary>
/// Aggregated performance snapshot containing CPU, memory, and GC metrics
/// captured at a specific point in time.
/// </summary>
public sealed record ProfileSnapshot
{
    /// <summary>Unique identifier for this snapshot.</summary>
    public required string SnapshotId { get; init; }

    /// <summary>UTC timestamp when the snapshot was captured.</summary>
    public required DateTimeOffset CapturedAt { get; init; }

    /// <summary>CPU utilization metrics.</summary>
    public required CpuSnapshot Cpu { get; init; }

    /// <summary>Memory utilization metrics.</summary>
    public required MemorySnapshot Memory { get; init; }

    /// <summary>Garbage collection metrics.</summary>
    public required GcSnapshot Gc { get; init; }

    /// <summary>Optional label for the snapshot (e.g., "baseline", "after-optimization").</summary>
    public string? Label { get; init; }
}
