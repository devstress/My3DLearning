using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a furniture/interior item from a supplier catalog.
/// </summary>
public sealed record FurnitureItem(
    Guid Id,
    Guid SupplierId,
    string Name,
    FurnitureCategory Category,
    decimal PriceAud,
    double WidthMetres,
    double DepthMetres,
    double HeightMetres,
    string Sku,
    bool InStock);
