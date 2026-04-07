using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents an immersive 3D walkthrough session for a home model.
/// </summary>
/// <param name="Id">Unique identifier for the walkthrough.</param>
/// <param name="HomeModelId">Reference to the <see cref="HomeModel"/> being toured.</param>
/// <param name="SitePlacementId">Optional reference to a specific placement.</param>
/// <param name="Status">Generation status of the walkthrough.</param>
/// <param name="TotalRooms">Total number of rooms identified in the model.</param>
/// <param name="DurationSeconds">Estimated walkthrough duration in seconds.</param>
/// <param name="CreatedByUserId">User who initiated the walkthrough.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the walkthrough was created.</param>
public sealed record HomeWalkthrough(
    Guid Id,
    Guid HomeModelId,
    Guid? SitePlacementId,
    WalkthroughStatus Status,
    int TotalRooms,
    int DurationSeconds,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc);
