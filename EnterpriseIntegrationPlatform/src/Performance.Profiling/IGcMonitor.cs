namespace Performance.Profiling;

/// <summary>
/// Monitors garbage collection behavior and provides tuning recommendations
/// based on observed GC patterns.
/// </summary>
public interface IGcMonitor
{
    /// <summary>
    /// Captures a snapshot of the current GC state including generation sizes,
    /// collection counts, fragmentation, and pause metrics.
    /// </summary>
    GcSnapshot CaptureSnapshot();

    /// <summary>
    /// Analyzes collected GC snapshots and returns tuning recommendations.
    /// Requires at least two snapshots for meaningful analysis.
    /// </summary>
    IReadOnlyList<GcTuningRecommendation> GetRecommendations();

    /// <summary>
    /// Returns all captured GC snapshots in chronological order.
    /// </summary>
    IReadOnlyList<GcSnapshot> GetHistory();

    /// <summary>
    /// Clears the GC snapshot history.
    /// </summary>
    void ClearHistory();
}
