using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for immersive 3D home walkthroughs — virtual tours with room navigation and POIs.
/// </summary>
public interface IWalkthroughService
{
    /// <summary>Generates a new walkthrough for a home model.</summary>
    Task<HomeWalkthrough> GenerateAsync(Guid homeModelId, Guid? sitePlacementId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a walkthrough by its unique identifier.</summary>
    Task<HomeWalkthrough?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all walkthroughs for a home model.</summary>
    Task<IReadOnlyList<HomeWalkthrough>> GetByHomeModelAsync(Guid homeModelId, CancellationToken cancellationToken = default);

    /// <summary>Adds a point of interest to a walkthrough.</summary>
    Task<WalkthroughPoi> AddPoiAsync(WalkthroughPoi poi, CancellationToken cancellationToken = default);

    /// <summary>Gets all POIs for a walkthrough.</summary>
    Task<IReadOnlyList<WalkthroughPoi>> GetPoisAsync(Guid walkthroughId, CancellationToken cancellationToken = default);

    /// <summary>Gets POIs filtered by room name.</summary>
    Task<IReadOnlyList<WalkthroughPoi>> GetPoisByRoomAsync(Guid walkthroughId, string roomName, CancellationToken cancellationToken = default);
}
