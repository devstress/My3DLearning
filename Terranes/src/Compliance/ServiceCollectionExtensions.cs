using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Compliance;

/// <summary>
/// DI registration for Compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCompliance(this IServiceCollection services)
    {
        services.AddSingleton<IComplianceService, ComplianceService>();
        return services;
    }
}
