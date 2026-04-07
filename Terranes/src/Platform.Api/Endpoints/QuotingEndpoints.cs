using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class QuotingEndpoints
{
    public static void MapQuotingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/quotes").WithTags("Quoting");

        group.MapPost("/", async (QuoteRequest request, IQuotingService service) =>
        {
            var created = await service.RequestQuoteAsync(request);
            return Results.Created($"/api/quotes/{created.Id}", created);
        }).WithName("CreateQuoteRequest");

        group.MapGet("/{id:guid}", async (Guid id, IQuotingService service) =>
        {
            var request = await service.GetQuoteRequestAsync(id);
            return request is not null ? Results.Ok(request) : Results.NotFound();
        }).WithName("GetQuoteRequest");

        group.MapGet("/{id:guid}/line-items", async (Guid id, IQuotingService service) =>
        {
            var items = await service.GetLineItemsAsync(id);
            return Results.Ok(items);
        }).WithName("GetQuoteLineItems");

        group.MapPost("/line-items", async (QuoteLineItem lineItem, IQuotingService service) =>
        {
            var created = await service.AddLineItemAsync(lineItem);
            return Results.Created($"/api/quotes/{created.QuoteRequestId}/line-items", created);
        }).WithName("AddQuoteLineItem");

        group.MapPost("/{id:guid}/complete", async (Guid id, IQuotingService service) =>
        {
            var completed = await service.CompleteQuoteAsync(id);
            return Results.Ok(completed);
        }).WithName("CompleteQuoteRequest");
    }
}
