namespace Performance.Profiling;

/// <summary>
/// Continuous profiler that captures periodic CPU, memory, and GC snapshots
/// for the running process.
/// </summary>
public interface IContinuousProfiler
{
    /// <summary>
    /// Captures a single performance snapshot of the current process.
    /// Computes delta metrics (CPU%, allocation rate) from the previous snapshot.
    /// </summary>
    ProfileSnapshot CaptureSnapshot(string? label = null);

    /// <summary>
    /// Retrieves all stored snapshots within the specified time range.
    /// </summary>
    IReadOnlyList<ProfileSnapshot> GetSnapshots(DateTimeOffset from, DateTimeOffset to);

    /// <summary>
    /// Retrieves the most recently captured snapshot, or null if no snapshots exist.
    /// </summary>
    ProfileSnapshot? GetLatestSnapshot();

    /// <summary>
    /// Returns the total number of stored snapshots.
    /// </summary>
    int SnapshotCount { get; }
}
