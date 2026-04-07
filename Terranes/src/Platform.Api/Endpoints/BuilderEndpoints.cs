using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class BuilderEndpoints
{
    public static void MapBuilderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/partners/builders").WithTags("Builders");

        group.MapPost("/register", async (Partner partner, BuilderProfile profile, IBuilderService service) =>
        {
            var created = await service.RegisterAsync(partner, profile);
            return Results.Created($"/api/partners/builders/{created.PartnerId}", created);
        }).WithName("RegisterBuilder");

        group.MapGet("/{partnerId:guid}", async (Guid partnerId, IBuilderService service) =>
        {
            var profile = await service.GetProfileAsync(partnerId);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        }).WithName("GetBuilderProfile");

        group.MapGet("/search", async (int bedrooms, double floorArea, IBuilderService service) =>
        {
            var results = await service.FindBuildersAsync(bedrooms, floorArea);
            return Results.Ok(results);
        }).WithName("SearchBuilders");

        group.MapPost("/{partnerId:guid}/quotes/{quoteRequestId:guid}", async (Guid partnerId, Guid quoteRequestId, HomeModel model, LandBlock block, IBuilderService service) =>
        {
            var response = await service.RequestQuoteAsync(partnerId, quoteRequestId, model, block);
            return Results.Ok(response);
        }).WithName("RequestBuilderQuote");

        group.MapPost("/{partnerId:guid}/quotes/{quoteRequestId:guid}/respond", async (Guid partnerId, Guid quoteRequestId, decimal amount, int estimatedDays, string description, IBuilderService service) =>
        {
            var response = await service.SubmitQuoteResponseAsync(partnerId, quoteRequestId, amount, estimatedDays, description);
            return Results.Ok(response);
        }).WithName("SubmitBuilderQuoteResponse");
    }
}
