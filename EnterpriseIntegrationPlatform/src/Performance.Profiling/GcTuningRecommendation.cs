namespace Performance.Profiling;

/// <summary>
/// A GC tuning recommendation based on observed garbage collection behavior.
/// </summary>
public sealed record GcTuningRecommendation
{
    /// <summary>Category of the recommendation (e.g., "ServerGC", "Fragmentation", "Gen2Pressure").</summary>
    public required string Category { get; init; }

    /// <summary>Severity of the recommendation.</summary>
    public required HotspotSeverity Severity { get; init; }

    /// <summary>Human-readable description of the recommendation.</summary>
    public required string Description { get; init; }

    /// <summary>Current observed value related to the recommendation.</summary>
    public required string CurrentValue { get; init; }

    /// <summary>Recommended target value or action.</summary>
    public required string RecommendedAction { get; init; }
}
