using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Analytics;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class SearchServiceTests
{
    private SearchService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new SearchService(NullLogger<SearchService>.Instance);

    // ── 1. Indexing ──

    [Test]
    public async Task IndexEntityAsync_AddsToIndex()
    {
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Modern Family Home", "3-bed home");
        var count = await _sut.GetIndexedCountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task RemoveEntityAsync_RemovesFromIndex()
    {
        var id = Guid.NewGuid();
        await _sut.IndexEntityAsync("HomeModel", id, "Test", "Test");
        await _sut.RemoveEntityAsync("HomeModel", id);

        var count = await _sut.GetIndexedCountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task IndexEntityAsync_OverwritesExistingEntry()
    {
        var id = Guid.NewGuid();
        await _sut.IndexEntityAsync("HomeModel", id, "Old Title", "Old");
        await _sut.IndexEntityAsync("HomeModel", id, "New Title", "New");

        var results = await _sut.SearchAsync("New Title");
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Title, Is.EqualTo("New Title"));
    }

    // ── 2. Search ──

    [Test]
    public async Task SearchAsync_ByTitle_ReturnsMatches()
    {
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Modern Family Home", "3-bed");
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Coastal Retreat", "Beach house");

        var results = await _sut.SearchAsync("Modern");
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Title, Is.EqualTo("Modern Family Home"));
    }

    [Test]
    public async Task SearchAsync_BySummary_ReturnsMatches()
    {
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Test Home", "Beautiful beachfront property");

        var results = await _sut.SearchAsync("beachfront");
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_NoMatch_ReturnsEmpty()
    {
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Test", "Test");
        var results = await _sut.SearchAsync("nonexistent");
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void SearchAsync_EmptyQuery_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.SearchAsync(""));
    }

    // ── 3. Typed Search ──

    [Test]
    public async Task SearchByTypeAsync_FiltersCorrectly()
    {
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Modern Home", "test");
        await _sut.IndexEntityAsync("VirtualVillage", Guid.NewGuid(), "Modern Village", "test");

        var results = await _sut.SearchByTypeAsync("HomeModel", "Modern");
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].EntityType, Is.EqualTo("HomeModel"));
    }

    [Test]
    public async Task SearchAsync_TitleMatchRanksHigherThanSummaryMatch()
    {
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Modern Design", "Simple summary");
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Simple Home", "Modern design summary");

        var results = await _sut.SearchAsync("Modern");
        Assert.That(results[0].Title, Is.EqualTo("Modern Design"));
    }

    [Test]
    public async Task SearchAsync_MaxResults_LimitsOutput()
    {
        for (var i = 0; i < 5; i++)
            await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), $"Home {i}", "Modern design");

        var results = await _sut.SearchAsync("Modern", maxResults: 2);
        Assert.That(results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SearchAsync_CaseInsensitive()
    {
        await _sut.IndexEntityAsync("HomeModel", Guid.NewGuid(), "Modern Home", "test");
        var results = await _sut.SearchAsync("modern home");
        Assert.That(results, Has.Count.EqualTo(1));
    }
}
