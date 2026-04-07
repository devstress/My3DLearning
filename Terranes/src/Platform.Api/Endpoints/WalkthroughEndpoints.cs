using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class WalkthroughEndpoints
{
    public static void MapWalkthroughEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/walkthroughs").WithTags("Walkthroughs");

        group.MapPost("/generate", async (Guid homeModelId, Guid? sitePlacementId, Guid userId, IWalkthroughService service) =>
        {
            var walkthrough = await service.GenerateAsync(homeModelId, sitePlacementId, userId);
            return Results.Created($"/api/walkthroughs/{walkthrough.Id}", walkthrough);
        }).WithName("GenerateWalkthrough");

        group.MapGet("/{id:guid}", async (Guid id, IWalkthroughService service) =>
        {
            var walkthrough = await service.GetByIdAsync(id);
            return walkthrough is not null ? Results.Ok(walkthrough) : Results.NotFound();
        }).WithName("GetWalkthrough");

        group.MapGet("/by-model/{homeModelId:guid}", async (Guid homeModelId, IWalkthroughService service) =>
        {
            var results = await service.GetByHomeModelAsync(homeModelId);
            return Results.Ok(results);
        }).WithName("GetWalkthroughsByModel");

        group.MapPost("/{walkthroughId:guid}/pois", async (Guid walkthroughId, WalkthroughPoi poi, IWalkthroughService service) =>
        {
            var poiWithWalkthrough = poi with { WalkthroughId = walkthroughId };
            var created = await service.AddPoiAsync(poiWithWalkthrough);
            return Results.Created($"/api/walkthroughs/{walkthroughId}/pois/{created.Id}", created);
        }).WithName("AddWalkthroughPoi");

        group.MapGet("/{walkthroughId:guid}/pois", async (Guid walkthroughId, string? room, IWalkthroughService service) =>
        {
            var pois = string.IsNullOrWhiteSpace(room)
                ? await service.GetPoisAsync(walkthroughId)
                : await service.GetPoisByRoomAsync(walkthroughId, room);
            return Results.Ok(pois);
        }).WithName("GetWalkthroughPois");
    }
}
