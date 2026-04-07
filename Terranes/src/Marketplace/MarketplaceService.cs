using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Marketplace;

/// <summary>
/// In-memory implementation of <see cref="IMarketplaceService"/>.
/// Manages property listings, search, and filtering.
/// </summary>
public sealed class MarketplaceService : IMarketplaceService
{
    private readonly ConcurrentDictionary<Guid, PropertyListing> _store = new();
    private readonly ILogger<MarketplaceService> _logger;

    public MarketplaceService(ILogger<MarketplaceService> logger) => _logger = logger;

    public Task<PropertyListing> CreateListingAsync(PropertyListing listing, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listing);

        if (string.IsNullOrWhiteSpace(listing.Title))
            throw new ArgumentException("Listing title is required.", nameof(listing));

        if (listing.AskingPriceAud.HasValue && listing.AskingPriceAud.Value < 0)
            throw new ArgumentException("Asking price cannot be negative.", nameof(listing));

        var persisted = listing with
        {
            Id = listing.Id == Guid.Empty ? Guid.NewGuid() : listing.Id,
            Status = ListingStatus.Draft,
            ListedAtUtc = DateTimeOffset.UtcNow
        };

        if (!_store.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Listing with ID {persisted.Id} already exists.");

        _logger.LogInformation("Created listing {ListingId}", persisted.Id);
        return Task.FromResult(persisted);
    }

    public Task<PropertyListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var listing);
        return Task.FromResult(listing);
    }

    public Task<IReadOnlyList<PropertyListing>> SearchAsync(string? suburb = null, decimal? maxPriceAud = null, ListingStatus? status = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<PropertyListing> query = _store.Values;

        if (!string.IsNullOrWhiteSpace(suburb))
            query = query.Where(l => l.Title.Contains(suburb, StringComparison.OrdinalIgnoreCase) ||
                                     l.Description.Contains(suburb, StringComparison.OrdinalIgnoreCase));

        if (maxPriceAud.HasValue)
            query = query.Where(l => l.AskingPriceAud.HasValue && l.AskingPriceAud.Value <= maxPriceAud.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        IReadOnlyList<PropertyListing> result = query.OrderByDescending(l => l.ListedAtUtc).ToList();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Updates the status of a listing (e.g., Draft → Active, Active → UnderOffer).
    /// </summary>
    public Task<PropertyListing> UpdateStatusAsync(Guid listingId, ListingStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(listingId, out var existing))
            throw new InvalidOperationException($"Listing {listingId} not found.");

        ValidateStatusTransition(existing.Status, newStatus);

        var updated = existing with { Status = newStatus };
        _store[listingId] = updated;

        _logger.LogInformation("Listing {ListingId} status changed: {OldStatus} → {NewStatus}", listingId, existing.Status, newStatus);
        return Task.FromResult(updated);
    }

    private static void ValidateStatusTransition(ListingStatus current, ListingStatus next)
    {
        var valid = (current, next) switch
        {
            (ListingStatus.Draft, ListingStatus.Active) => true,
            (ListingStatus.Active, ListingStatus.UnderOffer) => true,
            (ListingStatus.Active, ListingStatus.Withdrawn) => true,
            (ListingStatus.UnderOffer, ListingStatus.Sold) => true,
            (ListingStatus.UnderOffer, ListingStatus.Active) => true, // fallthrough from under offer
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException($"Cannot transition listing from {current} to {next}.");
    }
}
