using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a quote response from a partner for a specific quote request.
/// </summary>
public sealed record PartnerQuoteResponse(
    Guid Id,
    Guid QuoteRequestId,
    Guid PartnerId,
    PartnerCategory Category,
    PartnerQuoteStatus Status,
    decimal? AmountAud,
    string? Description,
    int? EstimatedDays,
    DateTimeOffset? ValidUntilUtc,
    DateTimeOffset RespondedAtUtc);
