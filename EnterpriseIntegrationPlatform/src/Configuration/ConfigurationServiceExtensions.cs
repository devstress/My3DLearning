using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Extension methods for registering configuration management services.
/// </summary>
public static class ConfigurationServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IConfigurationStore"/>, <see cref="IFeatureFlagService"/>,
    /// <see cref="ConfigurationChangeNotifier"/>, and <see cref="EnvironmentOverrideProvider"/>
    /// with the dependency injection container.
    /// </summary>
    public static IServiceCollection AddConfigurationManagement(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ConfigurationChangeNotifier>();
        services.AddSingleton<IConfigurationStore, InMemoryConfigurationStore>();
        services.AddSingleton<IFeatureFlagService, InMemoryFeatureFlagService>();
        services.AddSingleton<EnvironmentOverrideProvider>();

        return services;
    }
}
