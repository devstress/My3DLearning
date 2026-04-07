using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Journey;

/// <summary>
/// In-memory implementation of <see cref="IQuoteAggregatorService"/>.
/// Aggregates cost estimates from partner categories into a unified quote.
/// </summary>
public sealed class QuoteAggregatorService : IQuoteAggregatorService
{
    private readonly ConcurrentDictionary<Guid, AggregatedQuote> _quotes = new();
    private readonly IQuotingService _quotingService;
    private readonly ILogger<QuoteAggregatorService> _logger;

    public QuoteAggregatorService(IQuotingService quotingService, ILogger<QuoteAggregatorService> logger)
    {
        _quotingService = quotingService;
        _logger = logger;
    }

    public async Task<AggregatedQuote> AggregateAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        if (journeyId == Guid.Empty)
            throw new ArgumentException("Journey ID is required.", nameof(journeyId));

        // Check if there are any existing quote requests we can aggregate from
        // In a real system, this would pull from multiple partner quote responses
        // For the in-memory demo, we generate reasonable estimates based on the journey
        var builder = 280_000m + Random.Shared.Next(0, 120_001);
        var landscaping = 15_000m + Random.Shared.Next(0, 25_001);
        var furniture = 20_000m + Random.Shared.Next(0, 30_001);
        var smartHome = 5_000m + Random.Shared.Next(0, 15_001);
        var solicitor = 2_500m + Random.Shared.Next(0, 2_501);
        var total = builder + landscaping + furniture + smartHome + solicitor;

        var quote = new AggregatedQuote(
            Id: Guid.NewGuid(),
            JourneyId: journeyId,
            BuilderEstimateAud: builder,
            LandscapingEstimateAud: landscaping,
            FurnitureEstimateAud: furniture,
            SmartHomeEstimateAud: smartHome,
            SolicitorEstimateAud: solicitor,
            TotalEstimateAud: total,
            CreatedAtUtc: DateTimeOffset.UtcNow);

        if (!_quotes.TryAdd(quote.Id, quote))
            throw new InvalidOperationException("Quote ID conflict.");

        _logger.LogInformation("Generated aggregated quote {QuoteId} for journey {JourneyId}: ${Total:N2}", quote.Id, journeyId, total);
        return quote;
    }

    public Task<AggregatedQuote?> GetAggregatedQuoteAsync(Guid aggregatedQuoteId, CancellationToken cancellationToken = default)
    {
        _quotes.TryGetValue(aggregatedQuoteId, out var quote);
        return Task.FromResult(quote);
    }

    public Task<IReadOnlyList<AggregatedQuote>> GetQuotesForJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AggregatedQuote> result = _quotes.Values
            .Where(q => q.JourneyId == journeyId)
            .OrderByDescending(q => q.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }
}
