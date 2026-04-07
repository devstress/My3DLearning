namespace Terranes.Contracts.Enums;

/// <summary>
/// Represents the status of a partner referral.
/// </summary>
public enum ReferralStatus
{
    /// <summary>Referral has been created but not yet sent.</summary>
    Pending,

    /// <summary>Referral has been sent to the partner.</summary>
    Sent,

    /// <summary>Partner has accepted the referral.</summary>
    Accepted,

    /// <summary>Partner has declined the referral.</summary>
    Declined,

    /// <summary>Referral has expired without response.</summary>
    Expired
}
