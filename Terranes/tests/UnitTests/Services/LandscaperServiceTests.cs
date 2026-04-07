using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.PartnerIntegration;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class LandscaperServiceTests
{
    private LandscaperService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new LandscaperService(NullLogger<LandscaperService>.Instance);

    // ── 1. Registration ──

    [Test]
    public async Task RegisterAsync_ValidLandscaper_ReturnsProfile()
    {
        var (partner, profile) = MakeLandscaper();
        var registered = await _sut.RegisterAsync(partner, profile);

        Assert.That(registered.PartnerId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(registered.SupportedStyles, Has.Count.EqualTo(3));
    }

    [Test]
    public void RegisterAsync_EmptyBusinessName_ThrowsArgumentException()
    {
        var (partner, profile) = MakeLandscaper();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner with { BusinessName = "" }, profile));
    }

    [Test]
    public void RegisterAsync_WrongCategory_ThrowsArgumentException()
    {
        var (partner, profile) = MakeLandscaper();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner with { Category = PartnerCategory.Builder }, profile));
    }

    [Test]
    public void RegisterAsync_ZeroArea_ThrowsArgumentException()
    {
        var (partner, profile) = MakeLandscaper();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { MaxAreaSquareMetres = 0 }));
    }

    [Test]
    public void RegisterAsync_InvalidPriceRange_ThrowsArgumentException()
    {
        var (partner, profile) = MakeLandscaper();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { MinPriceAud = 50_000m, MaxPriceAud = 10_000m }));
    }

    [Test]
    public void RegisterAsync_EmptyStyles_ThrowsArgumentException()
    {
        var (partner, profile) = MakeLandscaper();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { SupportedStyles = [] }));
    }

    // ── 2. Search & Design ──

    [Test]
    public async Task FindLandscapersAsync_ByStyle_FiltersCorrectly()
    {
        var (partner, profile) = MakeLandscaper();
        await _sut.RegisterAsync(partner, profile);

        var results = await _sut.FindLandscapersAsync(style: LandscapeStyle.Native);
        Assert.That(results, Has.Count.EqualTo(1));

        var noResults = await _sut.FindLandscapersAsync(style: LandscapeStyle.Japanese);
        Assert.That(noResults, Is.Empty);
    }

    [Test]
    public async Task FindLandscapersAsync_ByMinArea_FiltersCorrectly()
    {
        var (partner, profile) = MakeLandscaper();
        await _sut.RegisterAsync(partner, profile);

        var results = await _sut.FindLandscapersAsync(minArea: 500);
        Assert.That(results, Has.Count.EqualTo(1));

        var tooLarge = await _sut.FindLandscapersAsync(minArea: 2000);
        Assert.That(tooLarge, Is.Empty);
    }

    [Test]
    public async Task CreateDesignAsync_ValidDesign_ReturnsWithId()
    {
        var design = new LandscapeDesign(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Aussie Native Garden", LandscapeStyle.Native, 25_000m, 200.0, default);
        var created = await _sut.CreateDesignAsync(design);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.TemplateName, Is.EqualTo("Aussie Native Garden"));
    }

    [Test]
    public void CreateDesignAsync_EmptyTemplateName_ThrowsArgumentException()
    {
        var design = new LandscapeDesign(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "", LandscapeStyle.Native, 25_000m, 200, default);
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateDesignAsync(design));
    }

    [Test]
    public void CreateDesignAsync_NegativeCost_ThrowsArgumentException()
    {
        var design = new LandscapeDesign(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Test", LandscapeStyle.Modern, -1m, 200, default);
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateDesignAsync(design));
    }

    [Test]
    public void CreateDesignAsync_ZeroCoverageArea_ThrowsArgumentException()
    {
        var design = new LandscapeDesign(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Test", LandscapeStyle.Modern, 1000m, 0, default);
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateDesignAsync(design));
    }

    // ── 3. Quote Request ──

    [Test]
    public async Task RequestQuoteAsync_ValidLandscaper_ReturnsRequestedStatus()
    {
        var (partner, profile) = MakeLandscaper();
        var registered = await _sut.RegisterAsync(partner, profile);

        var placement = new Terranes.Contracts.Models.SitePlacement(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, 3, 0, 1, Guid.NewGuid(), default);
        var response = await _sut.RequestQuoteAsync(registered.PartnerId, Guid.NewGuid(), placement, LandscapeStyle.Native);

        Assert.That(response.Status, Is.EqualTo(PartnerQuoteStatus.Requested));
        Assert.That(response.Category, Is.EqualTo(PartnerCategory.Landscaper));
    }

    [Test]
    public async Task GetDesignsForPlacementAsync_MultipleDesigns_ReturnsAll()
    {
        var placementId = Guid.NewGuid();
        await _sut.CreateDesignAsync(new LandscapeDesign(Guid.Empty, Guid.NewGuid(), placementId, "Design A", LandscapeStyle.Native, 20_000m, 150, default));
        await _sut.CreateDesignAsync(new LandscapeDesign(Guid.Empty, Guid.NewGuid(), placementId, "Design B", LandscapeStyle.Modern, 30_000m, 200, default));

        var designs = await _sut.GetDesignsForPlacementAsync(placementId);
        Assert.That(designs, Has.Count.EqualTo(2));
    }

    private static (Partner Partner, LandscaperProfile Profile) MakeLandscaper() => (
        new Partner(Guid.Empty, "Green Thumb Landscapes", PartnerCategory.Landscaper, "info@greenthumb.com.au", "+61400333444", ["NSW"], true, default),
        new LandscaperProfile(Guid.Empty, [LandscapeStyle.Native, LandscapeStyle.Modern, LandscapeStyle.Tropical], 1000.0, 10_000m, 100_000m, true));
}
