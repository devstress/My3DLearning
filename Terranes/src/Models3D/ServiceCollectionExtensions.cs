using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Models3D;

/// <summary>
/// DI registration for Models3D services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModels3D(this IServiceCollection services)
    {
        services.AddSingleton<IHomeModelService, HomeModelService>();
        return services;
    }
}
