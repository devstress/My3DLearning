using Terranes.Contracts.Abstractions;

namespace Terranes.Platform.Api.Endpoints;

public static class SitePlacementEndpoints
{
    public static void MapSitePlacementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/site-placements").WithTags("Site Placements");

        group.MapPost("/", async (Terranes.Contracts.Models.SitePlacement placement, ISitePlacementService service) =>
        {
            var created = await service.PlaceAsync(placement);
            return Results.Created($"/api/site-placements/{created.Id}", created);
        }).WithName("CreateSitePlacement");

        group.MapGet("/{id:guid}", async (Guid id, ISitePlacementService service) =>
        {
            var placement = await service.GetByIdAsync(id);
            return placement is not null ? Results.Ok(placement) : Results.NotFound();
        }).WithName("GetSitePlacement");

        group.MapPost("/validate", async (Terranes.Contracts.Models.SitePlacement placement, ISitePlacementService service) =>
        {
            var fits = await service.ValidateFitAsync(placement);
            return Results.Ok(new { Fits = fits });
        }).WithName("ValidateSitePlacement");
    }
}
