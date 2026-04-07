using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a landscaper partner profile with design capabilities.
/// </summary>
public sealed record LandscaperProfile(
    Guid PartnerId,
    IReadOnlyList<LandscapeStyle> SupportedStyles,
    double MaxAreaSquareMetres,
    decimal MinPriceAud,
    decimal MaxPriceAud,
    bool OffersMaintenancePlans);
