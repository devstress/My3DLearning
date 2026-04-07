using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a room fitting — a furniture item placed in a specific room of a home model.
/// </summary>
public sealed record RoomFitting(
    Guid Id,
    Guid HomeModelId,
    Guid FurnitureItemId,
    string RoomName,
    double PositionX,
    double PositionY,
    double RotationDegrees);
