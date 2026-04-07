using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class LandBlockEndpoints
{
    public static void MapLandBlockEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/land-blocks").WithTags("Land Blocks");

        group.MapPost("/", async (LandBlock block, ILandBlockService service) =>
        {
            var created = await service.CreateAsync(block);
            return Results.Created($"/api/land-blocks/{created.Id}", created);
        }).WithName("CreateLandBlock");

        group.MapGet("/{id:guid}", async (Guid id, ILandBlockService service) =>
        {
            var block = await service.GetByIdAsync(id);
            return block is not null ? Results.Ok(block) : Results.NotFound();
        }).WithName("GetLandBlock");

        group.MapGet("/lookup", async (string address, string state, ILandBlockService service) =>
        {
            var block = await service.LookupByAddressAsync(address, state);
            return block is not null ? Results.Ok(block) : Results.NotFound();
        }).WithName("LookupLandBlock");

        group.MapGet("/", async (string? suburb, string? state, double? minAreaSqm, ILandBlockService service) =>
        {
            var blocks = await service.SearchAsync(suburb, state, minAreaSqm);
            return Results.Ok(blocks);
        }).WithName("SearchLandBlocks");
    }
}
