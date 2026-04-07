using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for managing 3D home models — upload, retrieve, validate, and search.
/// </summary>
public interface IHomeModelService
{
    /// <summary>
    /// Stores a new home model with the given metadata.
    /// </summary>
    /// <param name="model">The home model metadata to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored home model with its assigned identifier.</returns>
    Task<HomeModel> CreateAsync(HomeModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a home model by its unique identifier.
    /// </summary>
    /// <param name="id">The model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The home model, or <c>null</c> if not found.</returns>
    Task<HomeModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches home models by criteria (bedrooms, format, floor area range).
    /// </summary>
    /// <param name="minBedrooms">Minimum number of bedrooms (inclusive).</param>
    /// <param name="format">Filter by model format, or <c>null</c> for any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All matching home models.</returns>
    Task<IReadOnlyList<HomeModel>> SearchAsync(int? minBedrooms = null, ModelFormat? format = null, CancellationToken cancellationToken = default);
}
