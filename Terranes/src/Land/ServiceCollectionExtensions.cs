using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Land;

/// <summary>
/// DI registration for Land services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLand(this IServiceCollection services)
    {
        services.AddSingleton<ILandBlockService, LandBlockService>();
        return services;
    }
}
