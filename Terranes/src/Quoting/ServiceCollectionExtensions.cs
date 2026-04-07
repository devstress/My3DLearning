using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Quoting;

/// <summary>
/// DI registration for Quoting services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuoting(this IServiceCollection services)
    {
        services.AddSingleton<IQuotingService, QuotingService>();
        services.AddSingleton<QuotingService>();
        return services;
    }
}
