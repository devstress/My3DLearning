using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class LandscaperEndpoints
{
    public static void MapLandscaperEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/partners/landscapers").WithTags("Landscapers");

        group.MapPost("/register", async (RegisterLandscaperRequest request, ILandscaperService service) =>
        {
            var created = await service.RegisterAsync(request.Partner, request.Profile);
            return Results.Created($"/api/partners/landscapers/{created.PartnerId}", created);
        }).WithName("RegisterLandscaper");

        group.MapGet("/{partnerId:guid}", async (Guid partnerId, ILandscaperService service) =>
        {
            var profile = await service.GetProfileAsync(partnerId);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        }).WithName("GetLandscaperProfile");

        group.MapGet("/search", async (LandscapeStyle? style, double? minArea, ILandscaperService service) =>
        {
            var results = await service.FindLandscapersAsync(style, minArea);
            return Results.Ok(results);
        }).WithName("SearchLandscapers");

        group.MapPost("/designs", async (LandscapeDesign design, ILandscaperService service) =>
        {
            var created = await service.CreateDesignAsync(design);
            return Results.Created($"/api/partners/landscapers/designs/{created.Id}", created);
        }).WithName("CreateLandscapeDesign");

        group.MapGet("/designs/placement/{sitePlacementId:guid}", async (Guid sitePlacementId, ILandscaperService service) =>
        {
            var designs = await service.GetDesignsForPlacementAsync(sitePlacementId);
            return Results.Ok(designs);
        }).WithName("GetDesignsForPlacement");
    }
}

public sealed record RegisterLandscaperRequest(Partner Partner, LandscaperProfile Profile);