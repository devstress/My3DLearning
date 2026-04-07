using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a property listing in the Terranes marketplace.
/// A listing can be created by agents, builders, or homeowners.
/// </summary>
/// <param name="Id">Unique identifier for this listing.</param>
/// <param name="HomeModelId">Reference to the <see cref="HomeModel"/> displayed in this listing.</param>
/// <param name="LandBlockId">Optional reference to a <see cref="LandBlock"/> if the listing includes land.</param>
/// <param name="Title">Display title of the listing.</param>
/// <param name="Description">Full description of the property.</param>
/// <param name="AskingPriceAud">Asking price in Australian dollars (null if price on application).</param>
/// <param name="Status">Current listing status.</param>
/// <param name="ListedByUserId">Identifier of the user or agent who created the listing.</param>
/// <param name="ListedAtUtc">UTC timestamp when the listing was published.</param>
public sealed record PropertyListing(
    Guid Id,
    Guid HomeModelId,
    Guid? LandBlockId,
    string Title,
    string Description,
    decimal? AskingPriceAud,
    ListingStatus Status,
    Guid ListedByUserId,
    DateTimeOffset ListedAtUtc);
