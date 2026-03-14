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
    /// Uses <see cref="InMemoryObservabilityEventLog"/> as the default fallback.
    /// For Loki-backed storage, call <see cref="AddPlatformObservability(IServiceCollection,string)"/>
    /// with the Loki base URL instead.
    /// </para>
    /// </summary>
    public static IServiceCollection AddPlatformObservability(this IServiceCollection services)
    {
        AddSharedObservability(services);

        // ── Isolated observability storage (in-memory fallback) ───────────────
        services.AddSingleton<IObservabilityEventLog, InMemoryObservabilityEventLog>();

        return services;
    }

    /// <summary>
    /// Registers the platform observability services with a Grafana Loki–backed
    /// <see cref="IObservabilityEventLog"/> for durable, queryable event storage.
    /// <para>
    /// Loki stores all message lifecycle events as structured log entries and
    /// supports LogQL queries by business key, correlation ID, message type, etc.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lokiBaseUrl">
    /// Base URL of the Loki HTTP API (e.g. <c>http://localhost:3100</c>).
    /// </param>
    public static IServiceCollection AddPlatformObservability(
        this IServiceCollection services,
        string lokiBaseUrl)
    {
        AddSharedObservability(services);

        // ── Isolated observability storage (Loki-backed) ──────────────────────
        services.AddHttpClient<IObservabilityEventLog, LokiObservabilityEventLog>(client =>
        {
            client.BaseAddress = new Uri(lokiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    private static void AddSharedObservability(IServiceCollection services)
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

        // ── Lifecycle recorder (writes to BOTH production + observability) ─────
        services.AddSingleton<MessageLifecycleRecorder>();

        // ── AI-powered analysis (queries observability store, not production) ──
        services.AddSingleton<ITraceAnalyzer, TraceAnalyzer>();
        services.AddSingleton<MessageStateInspector>();
    }
}
