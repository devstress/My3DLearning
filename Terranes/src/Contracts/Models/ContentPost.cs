using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a user-generated content post — users/agents sharing their built homes.
/// </summary>
/// <param name="Id">Unique identifier for the post.</param>
/// <param name="Title">Display title of the post.</param>
/// <param name="Description">Full description and story behind the build.</param>
/// <param name="HomeModelId">Reference to the <see cref="HomeModel"/> being showcased.</param>
/// <param name="SitePlacementId">Optional reference to the actual site placement.</param>
/// <param name="ImageUrls">URLs of photos/renders uploaded with the post.</param>
/// <param name="Status">Current post status.</param>
/// <param name="AverageRating">Community average rating (0–5 scale).</param>
/// <param name="TotalRatings">Total number of ratings received.</param>
/// <param name="PostedByUserId">User who created the post.</param>
/// <param name="PostedAtUtc">UTC timestamp when the post was created.</param>
public sealed record ContentPost(
    Guid Id,
    string Title,
    string Description,
    Guid HomeModelId,
    Guid? SitePlacementId,
    IReadOnlyList<string> ImageUrls,
    ContentPostStatus Status,
    double AverageRating,
    int TotalRatings,
    Guid PostedByUserId,
    DateTimeOffset PostedAtUtc);
