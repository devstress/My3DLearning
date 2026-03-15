using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// OpenTelemetry diagnostics for the Cassandra storage module.
/// Provides a dedicated <see cref="System.Diagnostics.ActivitySource"/>
/// and <see cref="Meter"/> for Cassandra-specific traces and metrics.
/// </summary>
public static class CassandraDiagnostics
{
    /// <summary>The source name used for Cassandra storage traces.</summary>
    public const string SourceName = "EnterpriseIntegrationPlatform.Storage.Cassandra";

    /// <summary>The version emitted with every Cassandra telemetry signal.</summary>
    public const string SourceVersion = "1.0.0";

    /// <summary>
    /// Activity source for creating Cassandra operation spans
    /// (reads, writes, schema operations).
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);

    /// <summary>
    /// Meter for recording Cassandra-specific metrics
    /// (operation counts, latencies).
    /// </summary>
    public static readonly Meter Meter = new(SourceName, SourceVersion);
}
