namespace Performance.Profiling;

/// <summary>
/// Result of comparing a benchmark run against its stored baseline.
/// </summary>
public sealed record BenchmarkRegression
{
    /// <summary>Name of the benchmark.</summary>
    public required string BenchmarkName { get; init; }

    /// <summary>Whether a duration regression was detected.</summary>
    public required bool DurationRegressed { get; init; }

    /// <summary>Whether a memory allocation regression was detected.</summary>
    public required bool AllocationRegressed { get; init; }

    /// <summary>Duration change as a percentage (positive = slower).</summary>
    public required double DurationChangePercent { get; init; }

    /// <summary>Allocation change as a percentage (positive = more allocations).</summary>
    public required double AllocationChangePercent { get; init; }

    /// <summary>The baseline that was compared against.</summary>
    public required BenchmarkBaseline Baseline { get; init; }

    /// <summary>The current run result.</summary>
    public required BenchmarkResult Current { get; init; }

    /// <summary>Whether any regression was detected (duration or allocation).</summary>
    public bool HasRegression => DurationRegressed || AllocationRegressed;
}
