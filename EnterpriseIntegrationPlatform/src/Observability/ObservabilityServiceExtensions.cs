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
    /// plus the message state store, lifecycle recorder, and AI-powered inspector.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddPlatformObservability(this IServiceCollection services)
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

        // State store (in-memory by default; swap for Cassandra-backed in production)
        services.AddSingleton<IMessageStateStore, InMemoryMessageStateStore>();

        // Lifecycle recorder (writes to store + emits OTel)
        services.AddSingleton<MessageLifecycleRecorder>();

        // AI-powered analysis
        services.AddSingleton<ITraceAnalyzer, TraceAnalyzer>();
        services.AddSingleton<MessageStateInspector>();

        return services;
    }
}
