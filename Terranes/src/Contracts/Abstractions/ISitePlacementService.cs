using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for placing 3D home models onto land blocks and validating site fit.
/// </summary>
public interface ISitePlacementService
{
    /// <summary>
    /// Creates a new site placement of a home model on a land block.
    /// </summary>
    /// <param name="placement">The placement details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created placement.</returns>
    Task<SitePlacement> PlaceAsync(SitePlacement placement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a site placement by its unique identifier.
    /// </summary>
    /// <param name="id">The placement identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The site placement, or <c>null</c> if not found.</returns>
    Task<SitePlacement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a home model fits on the specified land block
    /// given the placement position and orientation.
    /// </summary>
    /// <param name="placement">The proposed placement to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the model fits within the block boundaries; otherwise <c>false</c>.</returns>
    Task<bool> ValidateFitAsync(SitePlacement placement, CancellationToken cancellationToken = default);
}
