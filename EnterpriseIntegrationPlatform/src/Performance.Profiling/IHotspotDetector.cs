namespace Performance.Profiling;

/// <summary>
/// Detects CPU and memory hotspots from registered operation metrics.
/// Thread-safe for concurrent operation registration.
/// </summary>
public interface IHotspotDetector
{
    /// <summary>
    /// Registers a completed operation's performance metrics.
    /// </summary>
    /// <param name="operationName">Unique name identifying the operation.</param>
    /// <param name="duration">Time taken by the operation.</param>
    /// <param name="allocatedBytes">Bytes allocated during the operation.</param>
    void RegisterOperation(string operationName, TimeSpan duration, long allocatedBytes);

    /// <summary>
    /// Analyzes all registered operations against the provided thresholds
    /// and returns detected hotspots.
    /// </summary>
    IReadOnlyList<HotspotInfo> DetectHotspots(HotspotThresholds thresholds);

    /// <summary>
    /// Returns aggregated statistics for a specific operation.
    /// Returns null if the operation has not been registered.
    /// </summary>
    OperationStats? GetOperationStats(string operationName);

    /// <summary>
    /// Returns aggregated statistics for all tracked operations.
    /// </summary>
    IReadOnlyList<OperationStats> GetAllOperationStats();

    /// <summary>
    /// Clears all tracked operation data.
    /// </summary>
    void Reset();
}
