using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a referral of a qualified buyer to a partner.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="JourneyId">Associated buyer journey.</param>
/// <param name="PartnerId">Identifier of the partner being referred to.</param>
/// <param name="PartnerCategory">Type of partner (Builder, Landscaper, etc.).</param>
/// <param name="Status">Current referral status.</param>
/// <param name="BuyerName">Display name of the buyer.</param>
/// <param name="Notes">Optional notes for the partner.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the referral was created.</param>
/// <param name="RespondedAtUtc">UTC timestamp when the partner responded, or null.</param>
public sealed record PartnerReferral(
    Guid Id,
    Guid JourneyId,
    Guid PartnerId,
    PartnerCategory PartnerCategory,
    ReferralStatus Status,
    string BuyerName,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? RespondedAtUtc);
