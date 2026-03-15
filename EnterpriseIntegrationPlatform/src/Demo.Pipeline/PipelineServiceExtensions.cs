using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using EnterpriseIntegrationPlatform.Ingestion.Nats;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using EnterpriseIntegrationPlatform.Observability;

namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Extension methods for registering the demo pipeline services
/// with the dependency injection container.
/// </summary>
public static class PipelineServiceExtensions
{
    /// <summary>
    /// Registers all services required by the end-to-end demo integration pipeline:
    /// NATS broker, Cassandra storage, observability, Temporal dispatcher, orchestrator,
    /// and the background worker. Configuration is read from <c>Pipeline</c> and
    /// <c>Cassandra</c> sections.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDemoPipeline(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Pipeline options ──────────────────────────────────────────────────
        services.Configure<PipelineOptions>(
            configuration.GetSection(PipelineOptions.SectionName));

        var pipelineOpts = new PipelineOptions();
        configuration.GetSection(PipelineOptions.SectionName).Bind(pipelineOpts);

        // ── Message broker (NATS JetStream) ───────────────────────────────────
        services.AddNatsJetStreamBroker(pipelineOpts.NatsUrl);

        // ── Cassandra storage ─────────────────────────────────────────────────
        services.AddCassandraStorage(configuration);

        // ── Observability (Loki-backed event log + lifecycle recorder) ─────────
        var lokiBaseUrl = configuration["Loki:BaseAddress"] ?? "http://localhost:15100";
        services.AddPlatformObservability(lokiBaseUrl);

        // ── Temporal workflow dispatcher ───────────────────────────────────────
        services.AddSingleton<ITemporalWorkflowDispatcher, TemporalWorkflowDispatcher>();

        // ── Pipeline orchestrator ──────────────────────────────────────────────
        services.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();

        // ── Background worker ──────────────────────────────────────────────────
        services.AddHostedService<IntegrationPipelineWorker>();

        return services;
    }
}
