namespace Performance.Profiling;

/// <summary>
/// Configuration options for the continuous profiling system.
/// </summary>
public sealed class ProfilingOptions
{
    /// <summary>
    /// Interval between automatic profile snapshots.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan SnapshotInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of snapshots to retain in the in-memory store.
    /// Oldest snapshots are evicted when this limit is reached.
    /// Default: 1000.
    /// </summary>
    public int MaxRetainedSnapshots { get; set; } = 1000;

    /// <summary>
    /// Maximum number of operations tracked by the hotspot detector.
    /// Prevents unbounded memory growth from unique operation names.
    /// Default: 10000.
    /// </summary>
    public int MaxTrackedOperations { get; set; } = 10000;

    /// <summary>
    /// Whether continuous profiling is enabled on startup.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Hotspot detection thresholds.
    /// </summary>
    public HotspotThresholds HotspotThresholds { get; set; } = new();
}
