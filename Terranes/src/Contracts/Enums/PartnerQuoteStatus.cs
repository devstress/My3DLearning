namespace Terranes.Contracts.Enums;

/// <summary>
/// Status of a partner quote response for a specific quote request.
/// </summary>
public enum PartnerQuoteStatus
{
    Requested,
    Quoted,
    Declined,
    Expired,
    Accepted,
    Rejected
}
