namespace Performance.Profiling;

/// <summary>
/// Configurable thresholds for hotspot detection.
/// </summary>
public sealed class HotspotThresholds
{
    /// <summary>
    /// Warning threshold for average operation duration in milliseconds.
    /// Operations exceeding this value are flagged as duration hotspots.
    /// Default: 500ms.
    /// </summary>
    public double DurationWarningMs { get; set; } = 500;

    /// <summary>
    /// Critical threshold for average operation duration in milliseconds.
    /// Default: 2000ms.
    /// </summary>
    public double DurationCriticalMs { get; set; } = 2000;

    /// <summary>
    /// Warning threshold for average allocation per invocation in bytes.
    /// Default: 1 MB.
    /// </summary>
    public long AllocationWarningBytes { get; set; } = 1_048_576;

    /// <summary>
    /// Critical threshold for average allocation per invocation in bytes.
    /// Default: 10 MB.
    /// </summary>
    public long AllocationCriticalBytes { get; set; } = 10_485_760;

    /// <summary>
    /// Minimum number of invocations before an operation can be flagged.
    /// Prevents false positives from single anomalous calls.
    /// Default: 5.
    /// </summary>
    public int MinimumInvocations { get; set; } = 5;
}
