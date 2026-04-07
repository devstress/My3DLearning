using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Immersive3D;

/// <summary>
/// Registers all Immersive 3D Experience services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImmersive3D(this IServiceCollection services)
    {
        services.AddSingleton<IVirtualVillageService, VirtualVillageService>();
        services.AddSingleton<IWalkthroughService, WalkthroughService>();
        services.AddSingleton<IDesignEditorService, DesignEditorService>();
        services.AddSingleton<IVideoToModelService, VideoToModelService>();
        services.AddSingleton<IContentService, ContentService>();
        return services;
    }
}
