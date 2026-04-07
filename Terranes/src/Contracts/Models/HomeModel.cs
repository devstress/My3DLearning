using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a 3D home model that can be explored, placed on land, and modified.
/// </summary>
/// <param name="Id">Unique identifier for the model.</param>
/// <param name="Name">Display name of the home design.</param>
/// <param name="Description">Detailed description of the home design.</param>
/// <param name="Format">3D file format of the model.</param>
/// <param name="FileSizeBytes">Size of the 3D model file in bytes.</param>
/// <param name="Bedrooms">Number of bedrooms.</param>
/// <param name="Bathrooms">Number of bathrooms.</param>
/// <param name="GarageSpaces">Number of garage spaces.</param>
/// <param name="FloorAreaSquareMetres">Total floor area in square metres.</param>
/// <param name="OwnerId">Identifier of the user or agent who uploaded the model.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the model was created.</param>
public sealed record HomeModel(
    Guid Id,
    string Name,
    string Description,
    ModelFormat Format,
    long FileSizeBytes,
    int Bedrooms,
    int Bathrooms,
    int GarageSpaces,
    double FloorAreaSquareMetres,
    Guid OwnerId,
    DateTimeOffset CreatedAtUtc);
