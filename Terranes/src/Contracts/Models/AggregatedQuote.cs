namespace Terranes.Contracts.Models;

/// <summary>
/// Aggregated cost breakdown from multiple partner categories for a buyer journey.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="JourneyId">Associated buyer journey.</param>
/// <param name="BuilderEstimateAud">Estimated builder cost in AUD.</param>
/// <param name="LandscapingEstimateAud">Estimated landscaping cost in AUD.</param>
/// <param name="FurnitureEstimateAud">Estimated furniture cost in AUD.</param>
/// <param name="SmartHomeEstimateAud">Estimated smart home cost in AUD.</param>
/// <param name="SolicitorEstimateAud">Estimated solicitor cost in AUD.</param>
/// <param name="TotalEstimateAud">Total estimated cost across all categories.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the aggregate was generated.</param>
public sealed record AggregatedQuote(
    Guid Id,
    Guid JourneyId,
    decimal BuilderEstimateAud,
    decimal LandscapingEstimateAud,
    decimal FurnitureEstimateAud,
    decimal SmartHomeEstimateAud,
    decimal SolicitorEstimateAud,
    decimal TotalEstimateAud,
    DateTimeOffset CreatedAtUtc);
