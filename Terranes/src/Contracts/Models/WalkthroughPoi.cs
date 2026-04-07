using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a point of interest within a 3D walkthrough.
/// </summary>
/// <param name="Id">Unique identifier for the POI.</param>
/// <param name="WalkthroughId">Reference to the parent <see cref="HomeWalkthrough"/>.</param>
/// <param name="Type">Type of point of interest.</param>
/// <param name="Label">Display label for the POI.</param>
/// <param name="Description">Detailed description.</param>
/// <param name="PositionX">X position in the 3D scene.</param>
/// <param name="PositionY">Y position in the 3D scene.</param>
/// <param name="PositionZ">Z position in the 3D scene.</param>
/// <param name="RoomName">Room name this POI belongs to, or null if outdoor.</param>
public sealed record WalkthroughPoi(
    Guid Id,
    Guid WalkthroughId,
    WalkthroughPoiType Type,
    string Label,
    string Description,
    double PositionX,
    double PositionY,
    double PositionZ,
    string? RoomName);
