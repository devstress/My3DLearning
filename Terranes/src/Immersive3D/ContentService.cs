using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Immersive3D;

/// <summary>
/// In-memory implementation of <see cref="IContentService"/>.
/// Manages user-generated content posts, publishing, and community ratings.
/// </summary>
public sealed class ContentService : IContentService
{
    private readonly ConcurrentDictionary<Guid, ContentPost> _posts = new();
    private readonly ConcurrentDictionary<Guid, ContentRating> _ratings = new();
    private readonly ILogger<ContentService> _logger;

    public ContentService(ILogger<ContentService> logger) => _logger = logger;

    public Task<ContentPost> CreatePostAsync(ContentPost post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        if (string.IsNullOrWhiteSpace(post.Title))
            throw new ArgumentException("Post title is required.", nameof(post));

        if (string.IsNullOrWhiteSpace(post.Description))
            throw new ArgumentException("Post description is required.", nameof(post));

        if (post.HomeModelId == Guid.Empty)
            throw new ArgumentException("Home model ID is required.", nameof(post));

        var persisted = post with
        {
            Id = post.Id == Guid.Empty ? Guid.NewGuid() : post.Id,
            Status = ContentPostStatus.Draft,
            AverageRating = 0,
            TotalRatings = 0,
            PostedAtUtc = DateTimeOffset.UtcNow
        };

        if (!_posts.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Post {persisted.Id} already exists.");

        _logger.LogInformation("Created content post {PostId}", persisted.Id);
        return Task.FromResult(persisted);
    }

    public Task<ContentPost?> GetPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        _posts.TryGetValue(postId, out var post);
        return Task.FromResult(post);
    }

    public Task<IReadOnlyList<ContentPost>> SearchPostsAsync(ContentPostStatus? status = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<ContentPost> query = _posts.Values;

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (userId.HasValue)
            query = query.Where(p => p.PostedByUserId == userId.Value);

        IReadOnlyList<ContentPost> result = query.OrderByDescending(p => p.PostedAtUtc).ToList();
        return Task.FromResult(result);
    }

    public Task<ContentPost> PublishPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        if (!_posts.TryGetValue(postId, out var post))
            throw new InvalidOperationException($"Post {postId} not found.");

        if (post.Status != ContentPostStatus.Draft)
            throw new InvalidOperationException($"Cannot publish post in status {post.Status}. Only Draft posts can be published.");

        var published = post with { Status = ContentPostStatus.Published };
        _posts[postId] = published;

        _logger.LogInformation("Published content post {PostId}", postId);
        return Task.FromResult(published);
    }

    public Task<ContentRating> RatePostAsync(ContentRating rating, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rating);

        if (!_posts.TryGetValue(rating.ContentPostId, out var post))
            throw new InvalidOperationException($"Post {rating.ContentPostId} not found.");

        if (post.Status != ContentPostStatus.Published)
            throw new InvalidOperationException("Can only rate published posts.");

        if (rating.Score < 1 || rating.Score > 5)
            throw new ArgumentException("Score must be between 1 and 5.", nameof(rating));

        // Check for duplicate rating by same user
        if (_ratings.Values.Any(r => r.ContentPostId == rating.ContentPostId && r.RatedByUserId == rating.RatedByUserId))
            throw new InvalidOperationException("User has already rated this post.");

        var persisted = rating with { Id = rating.Id == Guid.Empty ? Guid.NewGuid() : rating.Id, RatedAtUtc = DateTimeOffset.UtcNow };

        if (!_ratings.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Rating {persisted.Id} already exists.");

        // Recalculate average
        var allRatings = _ratings.Values.Where(r => r.ContentPostId == post.Id).ToList();
        var avgRating = allRatings.Average(r => r.Score);
        _posts[post.Id] = post with { AverageRating = avgRating, TotalRatings = allRatings.Count };

        _logger.LogInformation("Rated post {PostId} with score {Score}", post.Id, rating.Score);
        return Task.FromResult(persisted);
    }

    public Task<IReadOnlyList<ContentRating>> GetRatingsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ContentRating> result = _ratings.Values
            .Where(r => r.ContentPostId == postId)
            .OrderByDescending(r => r.RatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }
}
