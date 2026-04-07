using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for managing property listings in the Terranes marketplace.
/// </summary>
public interface IMarketplaceService
{
    /// <summary>
    /// Creates a new property listing.
    /// </summary>
    /// <param name="listing">The listing details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created listing with its assigned identifier.</returns>
    Task<PropertyListing> CreateListingAsync(PropertyListing listing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a listing by its unique identifier.
    /// </summary>
    /// <param name="id">The listing identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The listing, or <c>null</c> if not found.</returns>
    Task<PropertyListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches active listings with optional filters.
    /// </summary>
    /// <param name="suburb">Filter by suburb, or <c>null</c> for any.</param>
    /// <param name="maxPriceAud">Maximum price filter in AUD, or <c>null</c> for any.</param>
    /// <param name="status">Filter by listing status, or <c>null</c> for active listings only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All matching listings.</returns>
    Task<IReadOnlyList<PropertyListing>> SearchAsync(string? suburb = null, decimal? maxPriceAud = null, ListingStatus? status = null, CancellationToken cancellationToken = default);
}
