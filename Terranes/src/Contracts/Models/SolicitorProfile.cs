namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a solicitor partner profile for property legal services.
/// </summary>
public sealed record SolicitorProfile(
    Guid PartnerId,
    IReadOnlyList<string> Specialisations,
    decimal FixedFeeAud,
    decimal? HourlyRateAud,
    bool OffersConveyancing,
    bool OffersContractReview,
    int YearsExperience);
