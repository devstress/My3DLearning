using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class FurnitureEndpoints
{
    public static void MapFurnitureEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/furniture").WithTags("Furniture & Interior");

        group.MapPost("/items", async (FurnitureItem item, IFurnitureService service) =>
        {
            var created = await service.AddItemAsync(item);
            return Results.Created($"/api/furniture/items/{created.Id}", created);
        }).WithName("AddFurnitureItem");

        group.MapGet("/items/{itemId:guid}", async (Guid itemId, IFurnitureService service) =>
        {
            var item = await service.GetItemAsync(itemId);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        }).WithName("GetFurnitureItem");

        group.MapGet("/items", async (FurnitureCategory? category, decimal? maxPrice, Guid? supplierId, IFurnitureService service) =>
        {
            var results = await service.SearchCatalogAsync(category, maxPrice, supplierId);
            return Results.Ok(results);
        }).WithName("SearchFurnitureCatalog");

        group.MapPost("/fittings", async (RoomFitting fitting, IFurnitureService service) =>
        {
            var created = await service.FitItemAsync(fitting);
            return Results.Created($"/api/furniture/fittings/{created.Id}", created);
        }).WithName("FitFurnitureItem");

        group.MapGet("/fittings/model/{homeModelId:guid}", async (Guid homeModelId, IFurnitureService service) =>
        {
            var fittings = await service.GetFittingsForModelAsync(homeModelId);
            return Results.Ok(fittings);
        }).WithName("GetFittingsForModel");

        group.MapGet("/total/{homeModelId:guid}", async (Guid homeModelId, IFurnitureService service) =>
        {
            var total = await service.CalculateTotalAsync(homeModelId);
            return Results.Ok(new { HomeModelId = homeModelId, TotalAud = total });
        }).WithName("CalculateFurnitureTotal");
    }
}
