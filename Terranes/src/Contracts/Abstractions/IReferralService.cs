using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Manages referrals of qualified buyers to partners.
/// </summary>
public interface IReferralService
{
    /// <summary>Creates a referral for a buyer journey to a specific partner.</summary>
    Task<PartnerReferral> CreateReferralAsync(Guid journeyId, Guid partnerId, PartnerCategory category, string buyerName, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a referral by ID.</summary>
    Task<PartnerReferral?> GetReferralAsync(Guid referralId, CancellationToken cancellationToken = default);

    /// <summary>Gets all referrals for a journey.</summary>
    Task<IReadOnlyList<PartnerReferral>> GetReferralsForJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>Gets all referrals sent to a partner.</summary>
    Task<IReadOnlyList<PartnerReferral>> GetReferralsForPartnerAsync(Guid partnerId, CancellationToken cancellationToken = default);

    /// <summary>Updates the referral status (accept/decline).</summary>
    Task<PartnerReferral> UpdateStatusAsync(Guid referralId, ReferralStatus newStatus, CancellationToken cancellationToken = default);
}
