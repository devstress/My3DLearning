namespace Performance.Profiling;

/// <summary>
/// Severity classification for detected performance hotspots.
/// </summary>
public enum HotspotSeverity
{
    /// <summary>Informational — within normal range but worth noting.</summary>
    Info,

    /// <summary>Warning — approaching threshold; may degrade under load.</summary>
    Warning,

    /// <summary>Critical — exceeds threshold; immediate attention recommended.</summary>
    Critical
}
