namespace Performance.Profiling;

/// <summary>
/// Stored baseline for a named benchmark, used for regression detection.
/// </summary>
public sealed record BenchmarkBaseline
{
    /// <summary>Name of the benchmark.</summary>
    public required string BenchmarkName { get; init; }

    /// <summary>Mean duration of the baseline run.</summary>
    public required TimeSpan MeanDuration { get; init; }

    /// <summary>Mean allocated bytes per iteration of the baseline run.</summary>
    public required long MeanAllocatedBytes { get; init; }

    /// <summary>Number of iterations used to establish the baseline.</summary>
    public required int Iterations { get; init; }

    /// <summary>UTC timestamp when the baseline was recorded.</summary>
    public required DateTimeOffset RecordedAt { get; init; }

    /// <summary>
    /// Maximum allowable regression percentage before flagging.
    /// Default: 20% — i.e., a result 20% slower than baseline triggers regression.
    /// </summary>
    public double RegressionThresholdPercent { get; init; } = 20.0;
}
