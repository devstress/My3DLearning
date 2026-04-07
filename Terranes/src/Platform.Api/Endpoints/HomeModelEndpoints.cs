using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class HomeModelEndpoints
{
    public static void MapHomeModelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/home-models").WithTags("Home Models");

        group.MapPost("/", async (HomeModel model, IHomeModelService service) =>
        {
            var created = await service.CreateAsync(model);
            return Results.Created($"/api/home-models/{created.Id}", created);
        }).WithName("CreateHomeModel");

        group.MapGet("/{id:guid}", async (Guid id, IHomeModelService service) =>
        {
            var model = await service.GetByIdAsync(id);
            return model is not null ? Results.Ok(model) : Results.NotFound();
        }).WithName("GetHomeModel");

        group.MapGet("/", async (int? minBedrooms, ModelFormat? format, IHomeModelService service) =>
        {
            var models = await service.SearchAsync(minBedrooms, format);
            return Results.Ok(models);
        }).WithName("SearchHomeModels");
    }
}
