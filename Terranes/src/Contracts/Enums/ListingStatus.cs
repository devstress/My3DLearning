namespace Terranes.Contracts.Enums;

/// <summary>
/// Listing status for properties in the marketplace.
/// </summary>
public enum ListingStatus
{
    /// <summary>Listing is in draft and not yet published.</summary>
    Draft,

    /// <summary>Listing is active and visible to buyers.</summary>
    Active,

    /// <summary>Listing is under offer from a buyer.</summary>
    UnderOffer,

    /// <summary>Property has been sold.</summary>
    Sold,

    /// <summary>Listing has been withdrawn by the owner or agent.</summary>
    Withdrawn
}
