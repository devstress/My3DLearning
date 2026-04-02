using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Extension methods for registering competing consumer services.
/// </summary>
public static class CompetingConsumerServiceExtensions
{
    /// <summary>
    /// Registers services for the competing consumers pattern including
    /// lag monitoring, consumer scaling, backpressure signaling, and the
    /// orchestrator background service. Configuration is bound from the
    /// <c>CompetingConsumers</c> section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompetingConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<CompetingConsumerOptions>(
            configuration.GetSection(CompetingConsumerOptions.SectionName));

        services.AddSingleton<IConsumerLagMonitor, InMemoryConsumerLagMonitor>();
        services.AddSingleton<IBackpressureSignal, BackpressureSignal>();
        services.AddSingleton<IConsumerScaler>(sp =>
            new InMemoryConsumerScaler(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemoryConsumerScaler>>()));

        services.AddHostedService<CompetingConsumerOrchestrator>();

        return services;
    }
}
