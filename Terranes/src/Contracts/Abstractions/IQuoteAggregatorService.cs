using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Aggregates cost estimates from multiple partner categories into a unified quote.
/// </summary>
public interface IQuoteAggregatorService
{
    /// <summary>Generates an aggregated quote for a buyer journey.</summary>
    Task<AggregatedQuote> AggregateAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves an aggregated quote by ID.</summary>
    Task<AggregatedQuote?> GetAggregatedQuoteAsync(Guid aggregatedQuoteId, CancellationToken cancellationToken = default);

    /// <summary>Gets all aggregated quotes for a journey.</summary>
    Task<IReadOnlyList<AggregatedQuote>> GetQuotesForJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default);
}
