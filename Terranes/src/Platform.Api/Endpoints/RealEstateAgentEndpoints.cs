using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class RealEstateAgentEndpoints
{
    public static void MapRealEstateAgentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/partners/agents").WithTags("Real Estate Agents");

        group.MapPost("/register", async (RegisterAgentRequest request, IRealEstateAgentService service) =>
        {
            var created = await service.RegisterAsync(request.Partner, request.Profile);
            return Results.Created($"/api/partners/agents/{created.PartnerId}", created);
        }).WithName("RegisterAgent");

        group.MapGet("/{partnerId:guid}", async (Guid partnerId, IRealEstateAgentService service) =>
        {
            var profile = await service.GetProfileAsync(partnerId);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        }).WithName("GetAgentProfile");

        group.MapGet("/search", async (string? suburb, bool? acceptsSelfListings, IRealEstateAgentService service) =>
        {
            var results = await service.FindAgentsAsync(suburb, acceptsSelfListings);
            return Results.Ok(results);
        }).WithName("SearchAgents");

        group.MapGet("/{partnerId:guid}/listings", async (Guid partnerId, IRealEstateAgentService service) =>
        {
            var listings = await service.GetAgentListingsAsync(partnerId);
            return Results.Ok(listings);
        }).WithName("GetAgentListings");

        group.MapPost("/{partnerId:guid}/listings/sync", async (Guid partnerId, PropertyListing listing, IRealEstateAgentService service) =>
        {
            var synced = await service.SyncListingAsync(partnerId, listing);
            return Results.Ok(synced);
        }).WithName("SyncAgentListing");
    }
}

public sealed record RegisterAgentRequest(Partner Partner, AgentProfile Profile);