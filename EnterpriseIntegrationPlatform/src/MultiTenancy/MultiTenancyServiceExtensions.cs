using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.MultiTenancy;

/// <summary>
/// Extension methods for registering multi-tenancy services.
/// </summary>
public static class MultiTenancyServiceExtensions
{
    /// <summary>
    /// Registers all multi-tenancy services: <see cref="ITenantResolver"/>,
    /// <see cref="ITenantIsolationGuard"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITenantResolver, TenantResolver>();
        services.AddSingleton<ITenantIsolationGuard, TenantIsolationGuard>();
        return services;
    }
}
