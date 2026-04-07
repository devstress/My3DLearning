using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for managing land blocks — lookup, store, and search by address or coordinates.
/// </summary>
public interface ILandBlockService
{
    /// <summary>
    /// Retrieves a land block by its unique identifier.
    /// </summary>
    /// <param name="id">The land block identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The land block, or <c>null</c> if not found.</returns>
    Task<LandBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a land block by street address. This may call government land data APIs.
    /// </summary>
    /// <param name="address">Full street address to look up.</param>
    /// <param name="state">State or territory code (e.g. NSW, VIC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The land block data, or <c>null</c> if the address could not be resolved.</returns>
    Task<LandBlock?> LookupByAddressAsync(string address, string state, CancellationToken cancellationToken = default);
}
