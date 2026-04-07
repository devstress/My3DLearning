using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Infrastructure;

/// <summary>
/// Registers all Infrastructure services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IObservabilityService, ObservabilityService>();
        services.AddSingleton<ITenantService, TenantService>();
        return services;
    }
}
