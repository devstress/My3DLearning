using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class VirtualVillageEndpoints
{
    public static void MapVirtualVillageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/villages").WithTags("Virtual Villages");

        group.MapPost("/", async (VirtualVillage village, IVirtualVillageService service) =>
        {
            var created = await service.CreateAsync(village);
            return Results.Created($"/api/villages/{created.Id}", created);
        }).WithName("CreateVillage");

        group.MapGet("/{id:guid}", async (Guid id, IVirtualVillageService service) =>
        {
            var village = await service.GetByIdAsync(id);
            return village is not null ? Results.Ok(village) : Results.NotFound();
        }).WithName("GetVillage");

        group.MapGet("/", async (string? name, VillageLayoutType? layout, IVirtualVillageService service) =>
        {
            var results = await service.SearchAsync(name, layout);
            return Results.Ok(results);
        }).WithName("SearchVillages");

        group.MapPost("/{villageId:guid}/lots", async (Guid villageId, VillageLot lot, IVirtualVillageService service) =>
        {
            var lotWithVillage = lot with { VillageId = villageId };
            var created = await service.AddLotAsync(lotWithVillage);
            return Results.Created($"/api/villages/{villageId}/lots/{created.Id}", created);
        }).WithName("AddVillageLot");

        group.MapGet("/{villageId:guid}/lots", async (Guid villageId, IVirtualVillageService service) =>
        {
            var lots = await service.GetLotsAsync(villageId);
            return Results.Ok(lots);
        }).WithName("GetVillageLots");

        group.MapPost("/{villageId:guid}/lots/{lotId:guid}/assign", async (Guid villageId, Guid lotId, Guid sitePlacementId, IVirtualVillageService service) =>
        {
            var updated = await service.AssignPlacementAsync(lotId, sitePlacementId);
            return Results.Ok(updated);
        }).WithName("AssignLotPlacement");

        group.MapGet("/{villageId:guid}/stats", async (Guid villageId, IVirtualVillageService service) =>
        {
            var (total, occupied, vacant) = await service.GetStatsAsync(villageId);
            return Results.Ok(new { TotalLots = total, OccupiedLots = occupied, VacantLots = vacant });
        }).WithName("GetVillageStats");
    }
}
