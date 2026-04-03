namespace Performance.Profiling;

/// <summary>
/// Describes a detected performance hotspot in a registered operation.
/// </summary>
public sealed record HotspotInfo
{
    /// <summary>Name of the operation exhibiting the hotspot.</summary>
    public required string OperationName { get; init; }

    /// <summary>Category of the hotspot (e.g., "Duration", "Allocation").</summary>
    public required string Category { get; init; }

    /// <summary>Severity of the hotspot.</summary>
    public required HotspotSeverity Severity { get; init; }

    /// <summary>Human-readable description of the hotspot.</summary>
    public required string Description { get; init; }

    /// <summary>The measured value that triggered the hotspot detection.</summary>
    public required double MeasuredValue { get; init; }

    /// <summary>The threshold that was exceeded.</summary>
    public required double ThresholdValue { get; init; }
}
