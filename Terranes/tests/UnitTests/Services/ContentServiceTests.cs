using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Immersive3D;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class ContentServiceTests
{
    private ContentService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new ContentService(NullLogger<ContentService>.Instance);

    // ── 1. Post Creation ──

    [Test]
    public async Task CreatePostAsync_ValidPost_ReturnsDraft()
    {
        var post = MakePost();
        var created = await _sut.CreatePostAsync(post);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Status, Is.EqualTo(ContentPostStatus.Draft));
        Assert.That(created.AverageRating, Is.EqualTo(0));
    }

    [Test]
    public void CreatePostAsync_EmptyTitle_ThrowsArgumentException()
    {
        var post = MakePost() with { Title = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreatePostAsync(post));
    }

    [Test]
    public void CreatePostAsync_EmptyDescription_ThrowsArgumentException()
    {
        var post = MakePost() with { Description = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreatePostAsync(post));
    }

    [Test]
    public void CreatePostAsync_EmptyHomeModelId_ThrowsArgumentException()
    {
        var post = MakePost() with { HomeModelId = Guid.Empty };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreatePostAsync(post));
    }

    // ── 2. Publishing ──

    [Test]
    public async Task PublishPostAsync_DraftPost_UpdatesToPublished()
    {
        var post = await _sut.CreatePostAsync(MakePost());
        var published = await _sut.PublishPostAsync(post.Id);

        Assert.That(published.Status, Is.EqualTo(ContentPostStatus.Published));
    }

    [Test]
    public async Task PublishPostAsync_AlreadyPublished_ThrowsInvalidOperationException()
    {
        var post = await _sut.CreatePostAsync(MakePost());
        await _sut.PublishPostAsync(post.Id);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PublishPostAsync(post.Id));
    }

    [Test]
    public void PublishPostAsync_NonExistent_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PublishPostAsync(Guid.NewGuid()));
    }

    // ── 3. Ratings ──

    [Test]
    public async Task RatePostAsync_ValidRating_UpdatesAverage()
    {
        var post = await _sut.CreatePostAsync(MakePost());
        await _sut.PublishPostAsync(post.Id);

        var rating = new ContentRating(Guid.Empty, post.Id, 4, "Great build!", Guid.NewGuid(), default);
        var created = await _sut.RatePostAsync(rating);

        Assert.That(created.Score, Is.EqualTo(4));

        var updated = await _sut.GetPostAsync(post.Id);
        Assert.That(updated!.AverageRating, Is.EqualTo(4.0));
        Assert.That(updated.TotalRatings, Is.EqualTo(1));
    }

    [Test]
    public async Task RatePostAsync_MultipleRatings_CalculatesAverage()
    {
        var post = await _sut.CreatePostAsync(MakePost());
        await _sut.PublishPostAsync(post.Id);

        await _sut.RatePostAsync(new ContentRating(Guid.Empty, post.Id, 5, null, Guid.NewGuid(), default));
        await _sut.RatePostAsync(new ContentRating(Guid.Empty, post.Id, 3, null, Guid.NewGuid(), default));

        var updated = await _sut.GetPostAsync(post.Id);
        Assert.That(updated!.AverageRating, Is.EqualTo(4.0));
        Assert.That(updated.TotalRatings, Is.EqualTo(2));
    }

    [Test]
    public async Task RatePostAsync_InvalidScore_ThrowsArgumentException()
    {
        var post = await _sut.CreatePostAsync(MakePost());
        await _sut.PublishPostAsync(post.Id);

        var rating = new ContentRating(Guid.Empty, post.Id, 6, null, Guid.NewGuid(), default);
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RatePostAsync(rating));
    }

    [Test]
    public async Task RatePostAsync_DuplicateUser_ThrowsInvalidOperationException()
    {
        var post = await _sut.CreatePostAsync(MakePost());
        await _sut.PublishPostAsync(post.Id);

        var userId = Guid.NewGuid();
        await _sut.RatePostAsync(new ContentRating(Guid.Empty, post.Id, 4, null, userId, default));

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RatePostAsync(new ContentRating(Guid.Empty, post.Id, 5, null, userId, default)));
    }

    [Test]
    public async Task RatePostAsync_UnpublishedPost_ThrowsInvalidOperationException()
    {
        var post = await _sut.CreatePostAsync(MakePost());
        var rating = new ContentRating(Guid.Empty, post.Id, 4, null, Guid.NewGuid(), default);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RatePostAsync(rating));
    }

    [Test]
    public async Task SearchPostsAsync_ByStatus_FiltersCorrectly()
    {
        var post1 = await _sut.CreatePostAsync(MakePost());
        await _sut.CreatePostAsync(MakePost());
        await _sut.PublishPostAsync(post1.Id);

        var published = await _sut.SearchPostsAsync(status: ContentPostStatus.Published);
        Assert.That(published, Has.Count.EqualTo(1));

        var drafts = await _sut.SearchPostsAsync(status: ContentPostStatus.Draft);
        Assert.That(drafts, Has.Count.EqualTo(1));
    }

    private static ContentPost MakePost() => new(
        Guid.Empty, "My Dream Home Build", "Built a stunning modern home in Sydney",
        Guid.NewGuid(), null, ["https://example.com/photo1.jpg"],
        ContentPostStatus.Draft, 0, 0, Guid.NewGuid(), default);
}
