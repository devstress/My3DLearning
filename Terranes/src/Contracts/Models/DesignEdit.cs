using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a single edit operation in the real-time 3D editor.
/// </summary>
/// <param name="Id">Unique identifier for this edit.</param>
/// <param name="SitePlacementId">The site placement being edited.</param>
/// <param name="Operation">Type of edit operation.</param>
/// <param name="PropertyName">Name of the property being modified (e.g. material name, colour hex).</param>
/// <param name="OldValue">Previous value before the edit.</param>
/// <param name="NewValue">New value after the edit.</param>
/// <param name="EditedByUserId">User who performed the edit.</param>
/// <param name="EditedAtUtc">UTC timestamp of the edit.</param>
public sealed record DesignEdit(
    Guid Id,
    Guid SitePlacementId,
    EditOperationType Operation,
    string PropertyName,
    string? OldValue,
    string NewValue,
    Guid EditedByUserId,
    DateTimeOffset EditedAtUtc);
