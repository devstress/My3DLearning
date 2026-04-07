using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;

namespace Terranes.Platform.Api.Endpoints;

public static class BuyerJourneyEndpoints
{
    public static void MapBuyerJourneyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/journeys").WithTags("BuyerJourney");

        group.MapPost("/", async (Guid buyerId, Guid? villageId, IBuyerJourneyService service) =>
        {
            var journey = await service.StartJourneyAsync(buyerId, villageId);
            return Results.Created($"/api/journeys/{journey.Id}", journey);
        }).WithName("StartJourney");

        group.MapGet("/{journeyId:guid}", async (Guid journeyId, IBuyerJourneyService service) =>
        {
            var journey = await service.GetJourneyAsync(journeyId);
            return journey is not null ? Results.Ok(journey) : Results.NotFound();
        }).WithName("GetJourney");

        group.MapGet("/buyer/{buyerId:guid}", async (Guid buyerId, IBuyerJourneyService service) =>
        {
            var journeys = await service.GetBuyerJourneysAsync(buyerId);
            return Results.Ok(journeys);
        }).WithName("GetBuyerJourneys");

        group.MapPut("/{journeyId:guid}/advance", async (Guid journeyId, JourneyStage stage, Guid? entityId, IBuyerJourneyService service) =>
        {
            var journey = await service.AdvanceStageAsync(journeyId, stage, entityId);
            return Results.Ok(journey);
        }).WithName("AdvanceJourneyStage");

        group.MapPost("/{journeyId:guid}/abandon", async (Guid journeyId, IBuyerJourneyService service) =>
        {
            var journey = await service.AbandonJourneyAsync(journeyId);
            return Results.Ok(journey);
        }).WithName("AbandonJourney");

        group.MapGet("/active", async (IBuyerJourneyService service) =>
        {
            var journeys = await service.GetActiveJourneysAsync();
            return Results.Ok(journeys);
        }).WithName("GetActiveJourneys");
    }
}
