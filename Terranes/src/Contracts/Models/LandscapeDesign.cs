namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a landscaping design template that can be applied to a site placement.
/// </summary>
public sealed record LandscapeDesign(
    Guid Id,
    Guid LandscaperPartnerId,
    Guid SitePlacementId,
    string TemplateName,
    Terranes.Contracts.Enums.LandscapeStyle Style,
    decimal EstimatedCostAud,
    double CoverageAreaSquareMetres,
    DateTimeOffset CreatedAtUtc);
