using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for user-generated content — users/agents posting their built homes, ratings, and galleries.
/// </summary>
public interface IContentService
{
    /// <summary>Creates a new content post.</summary>
    Task<ContentPost> CreatePostAsync(ContentPost post, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a post by its unique identifier.</summary>
    Task<ContentPost?> GetPostAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Searches posts by status or user.</summary>
    Task<IReadOnlyList<ContentPost>> SearchPostsAsync(ContentPostStatus? status = null, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>Publishes a draft post.</summary>
    Task<ContentPost> PublishPostAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Adds a rating to a published post.</summary>
    Task<ContentRating> RatePostAsync(ContentRating rating, CancellationToken cancellationToken = default);

    /// <summary>Gets all ratings for a post.</summary>
    Task<IReadOnlyList<ContentRating>> GetRatingsAsync(Guid postId, CancellationToken cancellationToken = default);
}
