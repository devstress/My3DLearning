using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.UnitTests.Models;

[TestFixture]
public sealed class PropertyListingTests
{
    [Test]
    public void Constructor_WithAllParameters_CreatesRecord()
    {
        var id = Guid.NewGuid();
        var homeModelId = Guid.NewGuid();
        var landBlockId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var listedAt = DateTimeOffset.UtcNow;

        var listing = new PropertyListing(
            Id: id,
            HomeModelId: homeModelId,
            LandBlockId: landBlockId,
            Title: "Modern 4BR Villa in Kellyville",
            Description: "Brand new modern villa on 650sqm block",
            AskingPriceAud: 1_200_000m,
            Status: ListingStatus.Active,
            ListedByUserId: userId,
            ListedAtUtc: listedAt);

        Assert.That(listing.Id, Is.EqualTo(id));
        Assert.That(listing.Title, Is.EqualTo("Modern 4BR Villa in Kellyville"));
        Assert.That(listing.AskingPriceAud, Is.EqualTo(1_200_000m));
        Assert.That(listing.Status, Is.EqualTo(ListingStatus.Active));
    }

    [Test]
    public void Constructor_WithNullLandBlock_RepresentsDesignOnlyListing()
    {
        var listing = new PropertyListing(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Display Home Design", "A showcase design without land",
            null, ListingStatus.Active, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.That(listing.LandBlockId, Is.Null);
        Assert.That(listing.AskingPriceAud, Is.Null);
    }

    [Test]
    public void With_StatusChange_CreatesNewRecordWithUpdatedStatus()
    {
        var listing = new PropertyListing(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Home", "Desc", 800_000m, ListingStatus.Active,
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        var sold = listing with { Status = ListingStatus.Sold };

        Assert.That(sold.Status, Is.EqualTo(ListingStatus.Sold));
        Assert.That(sold.Id, Is.EqualTo(listing.Id));
    }
}
