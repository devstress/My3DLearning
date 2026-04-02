using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Extension methods for registering tenant onboarding services in the DI container.
/// </summary>
public static class TenantOnboardingServiceExtensions
{
    /// <summary>
    /// Registers all tenant onboarding services: <see cref="ITenantOnboardingService"/>,
    /// <see cref="ITenantQuotaManager"/>, and <see cref="IBrokerNamespaceProvisioner"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration (reserved for future options binding).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTenantOnboarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<ITenantQuotaManager, InMemoryTenantQuotaManager>();
        services.AddSingleton<IBrokerNamespaceProvisioner, InMemoryBrokerNamespaceProvisioner>();
        services.AddSingleton<ITenantOnboardingService, InMemoryTenantOnboardingService>();

        return services;
    }
}
