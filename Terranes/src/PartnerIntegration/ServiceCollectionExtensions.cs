using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.PartnerIntegration;

/// <summary>
/// Registers all partner integration services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPartnerIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IBuilderService, BuilderService>();
        services.AddSingleton<ILandscaperService, LandscaperService>();
        services.AddSingleton<IFurnitureService, FurnitureService>();
        services.AddSingleton<ISmartHomeService, SmartHomeService>();
        services.AddSingleton<ISolicitorService, SolicitorService>();
        services.AddSingleton<IRealEstateAgentService, RealEstateAgentService>();
        return services;
    }
}
