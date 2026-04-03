using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using EnterpriseIntegrationPlatform.Ingestion.Nats;

namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Extension methods for registering the demo pipeline services
/// with the dependency injection container.
/// </summary>
public static class PipelineServiceExtensions
{
    /// <summary>
    /// Registers all services required by the end-to-end demo integration pipeline:
    /// NATS broker (for inbound subscription), Temporal dispatcher, orchestrator,
    /// and the background worker. All orchestration logic now runs atomically
    /// inside the Temporal <c>IntegrationPipelineWorkflow</c>.
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

        // ── Message broker (NATS JetStream) for inbound subscription ──────────
        services.AddNatsJetStreamBroker(pipelineOpts.NatsUrl);

        // ── Temporal workflow dispatcher ───────────────────────────────────────
        services.AddSingleton<ITemporalWorkflowDispatcher, TemporalWorkflowDispatcher>();

        // ── Pipeline orchestrator (thin Temporal dispatcher) ───────────────────
        services.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();

        // ── Background worker ──────────────────────────────────────────────────
        services.AddHostedService<IntegrationPipelineWorker>();

        return services;
    }
}
