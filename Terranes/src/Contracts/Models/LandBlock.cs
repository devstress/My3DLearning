using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a block of land with real-world dimensions, location, and zoning data.
/// </summary>
/// <param name="Id">Unique identifier for the land block.</param>
/// <param name="Address">Full street address of the land.</param>
/// <param name="Suburb">Suburb or locality name.</param>
/// <param name="State">State or territory code (e.g. NSW, VIC, QLD).</param>
/// <param name="PostCode">Postal code.</param>
/// <param name="AreaSquareMetres">Total area of the land block in square metres.</param>
/// <param name="FrontageMetres">Frontage width of the block in metres.</param>
/// <param name="DepthMetres">Depth of the block in metres.</param>
/// <param name="Zoning">Zoning classification of the land.</param>
/// <param name="Latitude">Geographic latitude of the block centroid.</param>
/// <param name="Longitude">Geographic longitude of the block centroid.</param>
public sealed record LandBlock(
    Guid Id,
    string Address,
    string Suburb,
    string State,
    string PostCode,
    double AreaSquareMetres,
    double FrontageMetres,
    double DepthMetres,
    ZoningType Zoning,
    double Latitude,
    double Longitude);
