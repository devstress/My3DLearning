namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a community rating on a user-generated content post.
/// </summary>
/// <param name="Id">Unique identifier for the rating.</param>
/// <param name="ContentPostId">Reference to the <see cref="ContentPost"/> being rated.</param>
/// <param name="Score">Rating score (1–5).</param>
/// <param name="Comment">Optional comment with the rating.</param>
/// <param name="RatedByUserId">User who submitted the rating.</param>
/// <param name="RatedAtUtc">UTC timestamp of the rating.</param>
public sealed record ContentRating(
    Guid Id,
    Guid ContentPostId,
    int Score,
    string? Comment,
    Guid RatedByUserId,
    DateTimeOffset RatedAtUtc);
