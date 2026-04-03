namespace Performance.Profiling;

/// <summary>
/// Result of a single benchmark run, to be compared against a stored baseline.
/// </summary>
public sealed record BenchmarkResult
{
    /// <summary>Name of the benchmark that was run.</summary>
    public required string BenchmarkName { get; init; }

    /// <summary>Mean duration of the benchmark run.</summary>
    public required TimeSpan MeanDuration { get; init; }

    /// <summary>Mean allocated bytes per iteration.</summary>
    public required long MeanAllocatedBytes { get; init; }

    /// <summary>Number of iterations in this run.</summary>
    public required int Iterations { get; init; }

    /// <summary>UTC timestamp of the benchmark run.</summary>
    public required DateTimeOffset RunAt { get; init; }
}
