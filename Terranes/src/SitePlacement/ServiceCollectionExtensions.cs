using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.SitePlacementEngine;

/// <summary>
/// DI registration for SitePlacement services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSitePlacement(this IServiceCollection services)
    {
        services.AddSingleton<ISitePlacementService, SitePlacementService>();
        return services;
    }
}
