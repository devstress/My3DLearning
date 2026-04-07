namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a 3D home model placed on a specific land block at a defined position and orientation.
/// </summary>
/// <param name="Id">Unique identifier for this placement.</param>
/// <param name="HomeModelId">Reference to the placed <see cref="HomeModel"/>.</param>
/// <param name="LandBlockId">Reference to the target <see cref="LandBlock"/>.</param>
/// <param name="OffsetXMetres">Offset from the block origin along the X axis (left-right) in metres.</param>
/// <param name="OffsetYMetres">Offset from the block origin along the Y axis (front-back) in metres.</param>
/// <param name="RotationDegrees">Rotation of the model in degrees (0–360, clockwise from north).</param>
/// <param name="ScaleFactor">Uniform scale factor applied to the model (1.0 = original size).</param>
/// <param name="PlacedByUserId">Identifier of the user who created this placement.</param>
/// <param name="PlacedAtUtc">UTC timestamp when the placement was created.</param>
public sealed record SitePlacement(
    Guid Id,
    Guid HomeModelId,
    Guid LandBlockId,
    double OffsetXMetres,
    double OffsetYMetres,
    double RotationDegrees,
    double ScaleFactor,
    Guid PlacedByUserId,
    DateTimeOffset PlacedAtUtc);
