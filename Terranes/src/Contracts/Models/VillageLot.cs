using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a single lot within a virtual village that can host a home model.
/// </summary>
/// <param name="Id">Unique identifier for this lot.</param>
/// <param name="VillageId">Reference to the parent <see cref="VirtualVillage"/>.</param>
/// <param name="LotNumber">Sequential lot number within the village (1-based).</param>
/// <param name="PositionX">X position of the lot within the village scene in metres.</param>
/// <param name="PositionY">Y position of the lot within the village scene in metres.</param>
/// <param name="WidthMetres">Width of the lot in metres.</param>
/// <param name="DepthMetres">Depth of the lot in metres.</param>
/// <param name="Status">Current status of the lot.</param>
/// <param name="SitePlacementId">Reference to the placed home, or null if vacant.</param>
public sealed record VillageLot(
    Guid Id,
    Guid VillageId,
    int LotNumber,
    double PositionX,
    double PositionY,
    double WidthMetres,
    double DepthMetres,
    VillageLotStatus Status,
    Guid? SitePlacementId);
