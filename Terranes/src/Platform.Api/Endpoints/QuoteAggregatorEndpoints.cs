using Terranes.Contracts.Abstractions;

namespace Terranes.Platform.Api.Endpoints;

public static class QuoteAggregatorEndpoints
{
    public static void MapQuoteAggregatorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/aggregated-quotes").WithTags("QuoteAggregator");

        group.MapPost("/", async (Guid journeyId, IQuoteAggregatorService service) =>
        {
            var quote = await service.AggregateAsync(journeyId);
            return Results.Created($"/api/aggregated-quotes/{quote.Id}", quote);
        }).WithName("AggregateQuote");

        group.MapGet("/{quoteId:guid}", async (Guid quoteId, IQuoteAggregatorService service) =>
        {
            var quote = await service.GetAggregatedQuoteAsync(quoteId);
            return quote is not null ? Results.Ok(quote) : Results.NotFound();
        }).WithName("GetAggregatedQuote");

        group.MapGet("/journey/{journeyId:guid}", async (Guid journeyId, IQuoteAggregatorService service) =>
        {
            var quotes = await service.GetQuotesForJourneyAsync(journeyId);
            return Results.Ok(quotes);
        }).WithName("GetJourneyQuotes");
    }
}
