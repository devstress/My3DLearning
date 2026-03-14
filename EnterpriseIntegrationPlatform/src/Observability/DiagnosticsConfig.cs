using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Central diagnostics configuration for OpenTelemetry.
/// Provides the shared <see cref="ActivitySource"/> and <see cref="Meter"/>
/// used by all platform components for distributed tracing and metrics.
/// </summary>
public static class DiagnosticsConfig
{
    /// <summary>The service name used for telemetry.</summary>
    public const string ServiceName = "EnterpriseIntegrationPlatform";

    /// <summary>The version emitted with every telemetry signal.</summary>
    public const string ServiceVersion = "1.0.0";

    /// <summary>
    /// The platform-wide <see cref="ActivitySource"/> for creating trace spans.
    /// All services should use this source (or <see cref="PlatformActivitySource"/>)
    /// when starting custom activities.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    /// <summary>
    /// The platform-wide <see cref="Meter"/> for recording metrics.
    /// All services should use <see cref="PlatformMeters"/> to record counters and histograms.
    /// </summary>
    public static readonly Meter Meter = new(ServiceName, ServiceVersion);
}
