using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Marketplace;

/// <summary>
/// DI registration for Marketplace services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplace(this IServiceCollection services)
    {
        services.AddSingleton<IMarketplaceService, MarketplaceService>();
        services.AddSingleton<MarketplaceService>();
        return services;
    }
}
