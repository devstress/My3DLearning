using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a builder partner with construction-specific details.
/// </summary>
public sealed record BuilderProfile(
    Guid PartnerId,
    BuilderType BuilderType,
    int MinBedrooms,
    int MaxBedrooms,
    decimal MinBuildPriceAud,
    decimal MaxBuildPriceAud,
    IReadOnlyList<string> Certifications,
    double MaxFloorAreaSquareMetres);
