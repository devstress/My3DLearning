using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Marketplace;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class MarketplaceServiceTests
{
    private MarketplaceService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new MarketplaceService(NullLogger<MarketplaceService>.Instance);

    // ── 1. Listing Creation ──

    [Test]
    public async Task CreateListingAsync_ValidListing_ReturnsWithDraftStatus()
    {
        var listing = MakeListing();
        var created = await _sut.CreateListingAsync(listing);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Status, Is.EqualTo(ListingStatus.Draft));
        Assert.That(created.Title, Is.EqualTo("Modern 4BR Villa in Bella Vista"));
    }

    [Test]
    public void CreateListingAsync_EmptyTitle_ThrowsArgumentException()
    {
        var listing = MakeListing() with { Title = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateListingAsync(listing));
    }

    [Test]
    public void CreateListingAsync_NegativePrice_ThrowsArgumentException()
    {
        var listing = MakeListing() with { AskingPriceAud = -1m };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateListingAsync(listing));
    }

    [Test]
    public async Task CreateListingAsync_NullPrice_IsAllowed()
    {
        var listing = MakeListing() with { AskingPriceAud = null };
        var created = await _sut.CreateListingAsync(listing);

        Assert.That(created.AskingPriceAud, Is.Null);
    }

    // ── 2. Retrieval & Search ──

    [Test]
    public async Task GetByIdAsync_ExistingListing_ReturnsListing()
    {
        var created = await _sut.CreateListingAsync(MakeListing());
        var retrieved = await _sut.GetByIdAsync(created.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Title, Is.EqualTo("Modern 4BR Villa in Bella Vista"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SearchAsync_BySuburb_MatchesTitleOrDescription()
    {
        await _sut.CreateListingAsync(MakeListing() with { Title = "Home in Bella Vista" });
        await _sut.CreateListingAsync(MakeListing() with { Title = "Home in Castle Hill" });

        var results = await _sut.SearchAsync(suburb: "Bella Vista");
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_ByMaxPrice_FiltersCorrectly()
    {
        await _sut.CreateListingAsync(MakeListing() with { AskingPriceAud = 500_000m });
        await _sut.CreateListingAsync(MakeListing() with { AskingPriceAud = 900_000m });

        var results = await _sut.SearchAsync(maxPriceAud: 600_000m);
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_ByStatus_FiltersCorrectly()
    {
        var listing = await _sut.CreateListingAsync(MakeListing());
        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Active);
        await _sut.CreateListingAsync(MakeListing()); // remains Draft

        var activeOnly = await _sut.SearchAsync(status: ListingStatus.Active);
        Assert.That(activeOnly, Has.Count.EqualTo(1));
    }

    // ── 3. Status Transitions ──

    [Test]
    public async Task UpdateStatusAsync_DraftToActive_Succeeds()
    {
        var listing = await _sut.CreateListingAsync(MakeListing());
        var updated = await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Active);

        Assert.That(updated.Status, Is.EqualTo(ListingStatus.Active));
    }

    [Test]
    public async Task UpdateStatusAsync_ActiveToUnderOffer_Succeeds()
    {
        var listing = await _sut.CreateListingAsync(MakeListing());
        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Active);
        var updated = await _sut.UpdateStatusAsync(listing.Id, ListingStatus.UnderOffer);

        Assert.That(updated.Status, Is.EqualTo(ListingStatus.UnderOffer));
    }

    [Test]
    public async Task UpdateStatusAsync_UnderOfferToSold_Succeeds()
    {
        var listing = await _sut.CreateListingAsync(MakeListing());
        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Active);
        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.UnderOffer);
        var sold = await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Sold);

        Assert.That(sold.Status, Is.EqualTo(ListingStatus.Sold));
    }

    [Test]
    public async Task UpdateStatusAsync_ActiveToWithdrawn_Succeeds()
    {
        var listing = await _sut.CreateListingAsync(MakeListing());
        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Active);
        var withdrawn = await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Withdrawn);

        Assert.That(withdrawn.Status, Is.EqualTo(ListingStatus.Withdrawn));
    }

    [Test]
    public async Task UpdateStatusAsync_UnderOfferBackToActive_Succeeds()
    {
        var listing = await _sut.CreateListingAsync(MakeListing());
        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Active);
        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.UnderOffer);
        var backToActive = await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Active);

        Assert.That(backToActive.Status, Is.EqualTo(ListingStatus.Active));
    }

    [Test]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        var listing = await _sut.CreateListingAsync(MakeListing());

        // Draft → Sold is not valid
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateStatusAsync(listing.Id, ListingStatus.Sold));
    }

    [Test]
    public void UpdateStatusAsync_NonExistentListing_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateStatusAsync(Guid.NewGuid(), ListingStatus.Active));
    }

    [Test]
    public async Task EndToEnd_FullListingLifecycle()
    {
        // Create → Activate → UnderOffer → Sold
        var listing = await _sut.CreateListingAsync(MakeListing());
        Assert.That(listing.Status, Is.EqualTo(ListingStatus.Draft));

        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Active);
        await _sut.UpdateStatusAsync(listing.Id, ListingStatus.UnderOffer);
        var sold = await _sut.UpdateStatusAsync(listing.Id, ListingStatus.Sold);
        Assert.That(sold.Status, Is.EqualTo(ListingStatus.Sold));
    }

    private static PropertyListing MakeListing() => new(
        Guid.Empty, Guid.NewGuid(), Guid.NewGuid(),
        "Modern 4BR Villa in Bella Vista",
        "Beautiful modern villa with 4 bedrooms and landscaped garden",
        750_000m, ListingStatus.Draft, Guid.NewGuid(), default);
}
