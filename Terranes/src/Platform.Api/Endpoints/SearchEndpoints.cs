using Terranes.Contracts.Abstractions;

namespace Terranes.Platform.Api.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/search").WithTags("Search");

        group.MapGet("/", async (string query, int? maxResults, ISearchService service) =>
        {
            var results = await service.SearchAsync(query, maxResults ?? 20);
            return Results.Ok(results);
        }).WithName("Search");

        group.MapGet("/{entityType}", async (string entityType, string query, int? maxResults, ISearchService service) =>
        {
            var results = await service.SearchByTypeAsync(entityType, query, maxResults ?? 20);
            return Results.Ok(results);
        }).WithName("SearchByType");

        group.MapPost("/index", async (string entityType, Guid entityId, string title, string summary, ISearchService service) =>
        {
            await service.IndexEntityAsync(entityType, entityId, title, summary);
            return Results.Ok();
        }).WithName("IndexEntity");

        group.MapDelete("/{entityType}/{entityId:guid}", async (string entityType, Guid entityId, ISearchService service) =>
        {
            await service.RemoveEntityAsync(entityType, entityId);
            return Results.NoContent();
        }).WithName("RemoveFromIndex");

        group.MapGet("/count", async (ISearchService service) =>
        {
            var count = await service.GetIndexedCountAsync();
            return Results.Ok(new { IndexedEntities = count });
        }).WithName("GetIndexedCount");
    }
}
