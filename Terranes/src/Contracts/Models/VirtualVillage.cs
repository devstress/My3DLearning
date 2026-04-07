using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a virtual 3D village — a neighbourhood of fully designed homes.
/// </summary>
/// <param name="Id">Unique identifier for the village.</param>
/// <param name="Name">Display name of the village.</param>
/// <param name="Description">Description of the village concept.</param>
/// <param name="Layout">Layout arrangement of the village.</param>
/// <param name="MaxLots">Maximum number of lots in this village.</param>
/// <param name="CentreLatitude">Geographic latitude of the village centre.</param>
/// <param name="CentreLongitude">Geographic longitude of the village centre.</param>
/// <param name="CreatedByUserId">Identifier of the user who created the village.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the village was created.</param>
public sealed record VirtualVillage(
    Guid Id,
    string Name,
    string Description,
    VillageLayoutType Layout,
    int MaxLots,
    double CentreLatitude,
    double CentreLongitude,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc);
