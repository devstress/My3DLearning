using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a request for end-to-end quotes across all partner categories.
/// </summary>
/// <param name="Id">Unique identifier for this quote request.</param>
/// <param name="SitePlacementId">Reference to the <see cref="SitePlacement"/> being quoted.</param>
/// <param name="RequestedByUserId">Identifier of the user who requested the quote.</param>
/// <param name="Status">Current lifecycle status of the quote request.</param>
/// <param name="RequestedAtUtc">UTC timestamp when the quote was requested.</param>
public sealed record QuoteRequest(
    Guid Id,
    Guid SitePlacementId,
    Guid RequestedByUserId,
    QuoteStatus Status,
    DateTimeOffset RequestedAtUtc);
