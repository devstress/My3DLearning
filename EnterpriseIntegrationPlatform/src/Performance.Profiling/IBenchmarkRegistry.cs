namespace Performance.Profiling;

/// <summary>
/// Stores and retrieves benchmark baselines for regression detection.
/// Thread-safe for concurrent access.
/// </summary>
public interface IBenchmarkRegistry
{
    /// <summary>
    /// Registers or replaces a baseline for the named benchmark.
    /// </summary>
    void RegisterBaseline(BenchmarkBaseline baseline);

    /// <summary>
    /// Retrieves the stored baseline for the named benchmark.
    /// Returns null if no baseline exists.
    /// </summary>
    BenchmarkBaseline? GetBaseline(string benchmarkName);

    /// <summary>
    /// Compares a benchmark result against the stored baseline and returns
    /// regression analysis. Returns null if no baseline exists.
    /// </summary>
    BenchmarkRegression? Compare(BenchmarkResult result);

    /// <summary>
    /// Returns all stored baselines.
    /// </summary>
    IReadOnlyList<BenchmarkBaseline> GetAllBaselines();

    /// <summary>
    /// Removes the baseline for the named benchmark.
    /// Returns true if a baseline was removed, false if not found.
    /// </summary>
    bool RemoveBaseline(string benchmarkName);
}
