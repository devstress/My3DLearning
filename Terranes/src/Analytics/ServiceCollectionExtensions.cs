using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Analytics;

/// <summary>
/// Registers all Search, Analytics, and Reporting services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalytics(this IServiceCollection services)
    {
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<IReportingService, ReportingService>();
        return services;
    }
}
