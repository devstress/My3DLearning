using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Extension methods for registering Disaster Recovery services into the DI container.
/// </summary>
public static class DisasterRecoveryServiceExtensions
{
    /// <summary>
    /// Registers the disaster recovery infrastructure: <see cref="IFailoverManager"/>,
    /// <see cref="IReplicationManager"/>, <see cref="IRecoveryPointValidator"/>,
    /// and <see cref="IDrDrillRunner"/>.
    /// Options are bound from the <c>DisasterRecovery</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddDisasterRecovery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DisasterRecoveryOptions>(
            configuration.GetSection(DisasterRecoveryOptions.SectionName));

        services.AddSingleton<IFailoverManager, InMemoryFailoverManager>();
        services.AddSingleton<IReplicationManager, InMemoryReplicationManager>();
        services.AddSingleton<IRecoveryPointValidator, RecoveryPointValidator>();
        services.AddSingleton<IDrDrillRunner, DrDrillRunner>();

        return services;
    }
}
