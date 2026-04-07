using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents an individual partner's quote for a specific category within a <see cref="QuoteRequest"/>.
/// </summary>
/// <param name="Id">Unique identifier for this quote line item.</param>
/// <param name="QuoteRequestId">Reference to the parent <see cref="QuoteRequest"/>.</param>
/// <param name="PartnerId">Identifier of the partner providing this quote.</param>
/// <param name="Category">The partner category this quote covers.</param>
/// <param name="AmountAud">Indicative quote amount in Australian dollars.</param>
/// <param name="Description">Description of what is included in this quote.</param>
/// <param name="ValidUntilUtc">UTC timestamp until which this quote remains valid.</param>
/// <param name="ProvidedAtUtc">UTC timestamp when the partner provided this quote.</param>
public sealed record QuoteLineItem(
    Guid Id,
    Guid QuoteRequestId,
    Guid PartnerId,
    PartnerCategory Category,
    decimal AmountAud,
    string Description,
    DateTimeOffset ValidUntilUtc,
    DateTimeOffset ProvidedAtUtc);
