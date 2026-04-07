using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Journey;

/// <summary>
/// Registers all Buyer Journey services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJourney(this IServiceCollection services)
    {
        services.AddSingleton<IBuyerJourneyService, BuyerJourneyService>();
        services.AddSingleton<IQuoteAggregatorService, QuoteAggregatorService>();
        services.AddSingleton<IReferralService, ReferralService>();
        return services;
    }
}
