using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class MarketplaceEndpoints
{
    public static void MapMarketplaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/listings").WithTags("Marketplace");

        group.MapPost("/", async (PropertyListing listing, IMarketplaceService service) =>
        {
            var created = await service.CreateListingAsync(listing);
            return Results.Created($"/api/listings/{created.Id}", created);
        }).WithName("CreateListing");

        group.MapGet("/{id:guid}", async (Guid id, IMarketplaceService service) =>
        {
            var listing = await service.GetByIdAsync(id);
            return listing is not null ? Results.Ok(listing) : Results.NotFound();
        }).WithName("GetListing");

        group.MapGet("/", async (string? suburb, decimal? maxPriceAud, ListingStatus? status, IMarketplaceService service) =>
        {
            var listings = await service.SearchAsync(suburb, maxPriceAud, status);
            return Results.Ok(listings);
        }).WithName("SearchListings");

        group.MapPut("/{id:guid}/status", async (Guid id, ListingStatus newStatus, IMarketplaceService service) =>
        {
            var updated = await service.UpdateStatusAsync(id, newStatus);
            return Results.Ok(updated);
        }).WithName("UpdateListingStatus");
    }
}
