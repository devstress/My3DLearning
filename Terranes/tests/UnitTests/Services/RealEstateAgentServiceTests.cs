using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Marketplace;
using Terranes.PartnerIntegration;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class RealEstateAgentServiceTests
{
    private MarketplaceService _marketplaceService = null!;
    private RealEstateAgentService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _marketplaceService = new MarketplaceService(NullLogger<MarketplaceService>.Instance);
        _sut = new RealEstateAgentService(_marketplaceService, NullLogger<RealEstateAgentService>.Instance);
    }

    // ── 1. Registration ──

    [Test]
    public async Task RegisterAsync_ValidAgent_ReturnsProfile()
    {
        var (partner, profile) = MakeAgent();
        var registered = await _sut.RegisterAsync(partner, profile);

        Assert.That(registered.PartnerId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(registered.LicenseNumber, Is.EqualTo("REA-NSW-12345"));
    }

    [Test]
    public void RegisterAsync_EmptyBusinessName_ThrowsArgumentException()
    {
        var (partner, profile) = MakeAgent();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner with { BusinessName = "" }, profile));
    }

    [Test]
    public void RegisterAsync_WrongCategory_ThrowsArgumentException()
    {
        var (partner, profile) = MakeAgent();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner with { Category = PartnerCategory.Builder }, profile));
    }

    [Test]
    public void RegisterAsync_EmptyLicense_ThrowsArgumentException()
    {
        var (partner, profile) = MakeAgent();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { LicenseNumber = "" }));
    }

    [Test]
    public void RegisterAsync_InvalidCommission_ThrowsArgumentException()
    {
        var (partner, profile) = MakeAgent();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { CommissionPercentage = 101m }));
    }

    [Test]
    public void RegisterAsync_NoCoverageSuburbs_ThrowsArgumentException()
    {
        var (partner, profile) = MakeAgent();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { CoverageSuburbs = [] }));
    }

    // ── 2. Search ──

    [Test]
    public async Task FindAgentsAsync_BySuburb_FiltersCorrectly()
    {
        var (partner, profile) = MakeAgent();
        await _sut.RegisterAsync(partner, profile);

        var results = await _sut.FindAgentsAsync(suburb: "Bella Vista");
        Assert.That(results, Has.Count.EqualTo(1));

        var noResults = await _sut.FindAgentsAsync(suburb: "Parramatta");
        Assert.That(noResults, Is.Empty);
    }

    [Test]
    public async Task FindAgentsAsync_BySelfListings_FiltersCorrectly()
    {
        var (partner, profile) = MakeAgent();
        await _sut.RegisterAsync(partner, profile with { AcceptsSelfListings = true });

        var results = await _sut.FindAgentsAsync(acceptsSelfListings: true);
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetProfileAsync_ExistingPartner_ReturnsProfile()
    {
        var (partner, profile) = MakeAgent();
        var registered = await _sut.RegisterAsync(partner, profile);
        var retrieved = await _sut.GetProfileAsync(registered.PartnerId);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.CommissionPercentage, Is.EqualTo(2.5m));
    }

    // ── 3. Listings Sync ──

    [Test]
    public async Task SyncListingAsync_ValidListing_CreateInMarketplace()
    {
        var (partner, profile) = MakeAgent();
        var registered = await _sut.RegisterAsync(partner, profile);

        var listing = new PropertyListing(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Modern Home in Bella Vista", "Beautiful home", 750_000m, ListingStatus.Draft, Guid.NewGuid(), default);
        var synced = await _sut.SyncListingAsync(registered.PartnerId, listing);

        Assert.That(synced.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(synced.ListedByUserId, Is.EqualTo(registered.PartnerId));
    }

    [Test]
    public async Task SyncListingAsync_UpdatesActiveListingsCount()
    {
        var (partner, profile) = MakeAgent();
        var registered = await _sut.RegisterAsync(partner, profile);

        var listing = new PropertyListing(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Listing A", "Desc", 500_000m, ListingStatus.Draft, Guid.NewGuid(), default);
        await _sut.SyncListingAsync(registered.PartnerId, listing);

        var updatedProfile = await _sut.GetProfileAsync(registered.PartnerId);
        Assert.That(updatedProfile!.ActiveListingsCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAgentListingsAsync_MultipleSyncedListings_ReturnsAll()
    {
        var (partner, profile) = MakeAgent();
        var registered = await _sut.RegisterAsync(partner, profile);

        await _sut.SyncListingAsync(registered.PartnerId, MakeListing("Listing 1"));
        await _sut.SyncListingAsync(registered.PartnerId, MakeListing("Listing 2"));
        await _sut.SyncListingAsync(registered.PartnerId, MakeListing("Listing 3"));

        var listings = await _sut.GetAgentListingsAsync(registered.PartnerId);
        Assert.That(listings, Has.Count.EqualTo(3));
    }

    [Test]
    public void GetAgentListingsAsync_NonExistentAgent_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetAgentListingsAsync(Guid.NewGuid()));
    }

    [Test]
    public void SyncListingAsync_NonExistentAgent_ThrowsInvalidOperationException()
    {
        var listing = MakeListing("Test");
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SyncListingAsync(Guid.NewGuid(), listing));
    }

    private static (Partner Partner, AgentProfile Profile) MakeAgent() => (
        new Partner(Guid.Empty, "Hills Realty", PartnerCategory.RealEstateAgent, "info@hillsrealty.com.au", "+61400777888", ["NSW"], true, default),
        new AgentProfile(Guid.Empty, "REA-NSW-12345", ["Bella Vista", "Castle Hill", "Kellyville"], 2.5m, 0, false));

    private static PropertyListing MakeListing(string title) => new(
        Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), title, "Description", 650_000m, ListingStatus.Draft, Guid.NewGuid(), default);
}
