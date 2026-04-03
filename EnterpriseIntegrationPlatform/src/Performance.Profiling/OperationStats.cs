namespace Performance.Profiling;

/// <summary>
/// Aggregated statistics for a tracked operation.
/// </summary>
public sealed record OperationStats
{
    /// <summary>Name of the operation.</summary>
    public required string OperationName { get; init; }

    /// <summary>Total number of recorded invocations.</summary>
    public required long InvocationCount { get; init; }

    /// <summary>Average duration per invocation.</summary>
    public required TimeSpan AverageDuration { get; init; }

    /// <summary>Maximum observed duration.</summary>
    public required TimeSpan MaxDuration { get; init; }

    /// <summary>Minimum observed duration.</summary>
    public required TimeSpan MinDuration { get; init; }

    /// <summary>Average bytes allocated per invocation.</summary>
    public required long AverageAllocatedBytes { get; init; }

    /// <summary>Total bytes allocated across all invocations.</summary>
    public required long TotalAllocatedBytes { get; init; }
}
