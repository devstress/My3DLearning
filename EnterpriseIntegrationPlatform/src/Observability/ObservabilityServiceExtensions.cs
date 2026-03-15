using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Extension methods for registering the platform observability services
/// with the dependency injection container.
/// </summary>
public static class ObservabilityServiceExtensions
{
    /// <summary>
    /// Registers the platform's custom <see cref="DiagnosticsConfig.ActivitySource"/>
    /// and <see cref="DiagnosticsConfig.Meter"/> with the OpenTelemetry pipeline,
    /// plus the separated production store, observability event log, lifecycle
    /// recorder, and AI-powered inspector.
    /// <para>
    /// <b>Production storage</b> (<see cref="IMessageStateStore"/>) is used by the
    /// message processing pipeline only.<br/>
    /// <b>Observability storage</b> (<see cref="IObservabilityEventLog"/> + Prometheus
    /// metrics at <c>/metrics</c>) is queried by operators via OpenClaw.
    /// </para>
    /// <para>
    /// Uses <see cref="LokiObservabilityEventLog"/> backed by Grafana Loki for
    /// durable, queryable event storage.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lokiBaseUrl">
    /// Base URL of the Loki HTTP API (e.g. <c>http://localhost:15100</c>).
    /// </param>
    public static IServiceCollection AddPlatformObservability(
        this IServiceCollection services,
        string lokiBaseUrl)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource(DiagnosticsConfig.ServiceName);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(DiagnosticsConfig.ServiceName);
            });

        // ── Production storage (message processing pipeline only) ─────────────
        services.AddSingleton<IMessageStateStore, InMemoryMessageStateStore>();

        // ── Isolated observability storage (Loki-backed) ──────────────────────
        services.AddHttpClient<IObservabilityEventLog, LokiObservabilityEventLog>(client =>
        {
            client.BaseAddress = new Uri(lokiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── Lifecycle recorder (writes to BOTH production + observability) ─────
        services.AddSingleton<MessageLifecycleRecorder>();

        // ── AI-powered analysis (queries observability store, not production) ──
        services.AddSingleton<ITraceAnalyzer, TraceAnalyzer>();
        services.AddSingleton<MessageStateInspector>();

        return services;
    }
}
