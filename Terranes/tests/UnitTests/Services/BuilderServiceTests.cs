using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.PartnerIntegration;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class BuilderServiceTests
{
    private BuilderService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new BuilderService(NullLogger<BuilderService>.Instance);

    // ── 1. Registration ──

    [Test]
    public async Task RegisterAsync_ValidBuilder_ReturnsProfile()
    {
        var (partner, profile) = MakeBuilder();
        var registered = await _sut.RegisterAsync(partner, profile);

        Assert.That(registered.PartnerId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(registered.BuilderType, Is.EqualTo(BuilderType.Volume));
    }

    [Test]
    public void RegisterAsync_EmptyBusinessName_ThrowsArgumentException()
    {
        var (partner, profile) = MakeBuilder();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner with { BusinessName = "" }, profile));
    }

    [Test]
    public void RegisterAsync_WrongCategory_ThrowsArgumentException()
    {
        var (partner, profile) = MakeBuilder();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner with { Category = PartnerCategory.Landscaper }, profile));
    }

    [Test]
    public void RegisterAsync_InvalidPriceRange_ThrowsArgumentException()
    {
        var (partner, profile) = MakeBuilder();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { MinBuildPriceAud = 500_000m, MaxBuildPriceAud = 100_000m }));
    }

    [Test]
    public void RegisterAsync_InvalidBedroomRange_ThrowsArgumentException()
    {
        var (partner, profile) = MakeBuilder();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { MinBedrooms = 5, MaxBedrooms = 3 }));
    }

    [Test]
    public void RegisterAsync_ZeroFloorArea_ThrowsArgumentException()
    {
        var (partner, profile) = MakeBuilder();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { MaxFloorAreaSquareMetres = 0 }));
    }

    // ── 2. Profile Retrieval & Search ──

    [Test]
    public async Task GetProfileAsync_ExistingPartner_ReturnsProfile()
    {
        var (partner, profile) = MakeBuilder();
        var registered = await _sut.RegisterAsync(partner, profile);
        var retrieved = await _sut.GetProfileAsync(registered.PartnerId);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.BuilderType, Is.EqualTo(BuilderType.Volume));
    }

    [Test]
    public async Task GetProfileAsync_NonExistentPartner_ReturnsNull()
    {
        var result = await _sut.GetProfileAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FindBuildersAsync_MatchingCriteria_ReturnsBuilders()
    {
        var (partner, profile) = MakeBuilder();
        await _sut.RegisterAsync(partner, profile);

        var results = await _sut.FindBuildersAsync(bedrooms: 3, floorArea: 200);
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FindBuildersAsync_TooManyBedrooms_ReturnsEmpty()
    {
        var (partner, profile) = MakeBuilder();
        await _sut.RegisterAsync(partner, profile);

        var results = await _sut.FindBuildersAsync(bedrooms: 10, floorArea: 200);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task FindBuildersAsync_FloorAreaTooLarge_ReturnsEmpty()
    {
        var (partner, profile) = MakeBuilder();
        await _sut.RegisterAsync(partner, profile);

        var results = await _sut.FindBuildersAsync(bedrooms: 3, floorArea: 1000);
        Assert.That(results, Is.Empty);
    }

    // ── 3. Quote Request/Response ──

    [Test]
    public async Task RequestQuoteAsync_ValidBuilder_ReturnsRequestedStatus()
    {
        var (partner, profile) = MakeBuilder();
        var registered = await _sut.RegisterAsync(partner, profile);

        var model = new HomeModel(Guid.NewGuid(), "Villa", "A villa", ModelFormat.Gltf, 1024, 4, 2, 2, 200, Guid.NewGuid(), default);
        var block = new LandBlock(Guid.NewGuid(), "1 Test St", "TestSuburb", "NSW", "2000", 500, 15, 30, ZoningType.Residential, -33, 151);

        var response = await _sut.RequestQuoteAsync(registered.PartnerId, Guid.NewGuid(), model, block);
        Assert.That(response.Status, Is.EqualTo(PartnerQuoteStatus.Requested));
        Assert.That(response.AmountAud, Is.Null);
    }

    [Test]
    public void RequestQuoteAsync_NonExistentBuilder_ThrowsInvalidOperationException()
    {
        var model = new HomeModel(Guid.NewGuid(), "Villa", "A villa", ModelFormat.Gltf, 1024, 4, 2, 2, 200, Guid.NewGuid(), default);
        var block = new LandBlock(Guid.NewGuid(), "1 Test St", "TestSuburb", "NSW", "2000", 500, 15, 30, ZoningType.Residential, -33, 151);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RequestQuoteAsync(Guid.NewGuid(), Guid.NewGuid(), model, block));
    }

    [Test]
    public async Task SubmitQuoteResponseAsync_ValidResponse_UpdatesToQuotedStatus()
    {
        var (partner, profile) = MakeBuilder();
        var registered = await _sut.RegisterAsync(partner, profile);

        var quoteId = Guid.NewGuid();
        var model = new HomeModel(Guid.NewGuid(), "Villa", "A villa", ModelFormat.Gltf, 1024, 4, 2, 2, 200, Guid.NewGuid(), default);
        var block = new LandBlock(Guid.NewGuid(), "1 Test St", "TestSuburb", "NSW", "2000", 500, 15, 30, ZoningType.Residential, -33, 151);

        await _sut.RequestQuoteAsync(registered.PartnerId, quoteId, model, block);
        var response = await _sut.SubmitQuoteResponseAsync(registered.PartnerId, quoteId, 380_000m, 180, "Full build including landscaping");

        Assert.That(response.Status, Is.EqualTo(PartnerQuoteStatus.Quoted));
        Assert.That(response.AmountAud, Is.EqualTo(380_000m));
        Assert.That(response.EstimatedDays, Is.EqualTo(180));
    }

    [Test]
    public async Task SubmitQuoteResponseAsync_NegativeAmount_ThrowsArgumentException()
    {
        var (partner, profile) = MakeBuilder();
        var registered = await _sut.RegisterAsync(partner, profile);

        var quoteId = Guid.NewGuid();
        var model = new HomeModel(Guid.NewGuid(), "Villa", "A villa", ModelFormat.Gltf, 1024, 4, 2, 2, 200, Guid.NewGuid(), default);
        var block = new LandBlock(Guid.NewGuid(), "1 Test St", "TestSuburb", "NSW", "2000", 500, 15, 30, ZoningType.Residential, -33, 151);

        await _sut.RequestQuoteAsync(registered.PartnerId, quoteId, model, block);
        Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitQuoteResponseAsync(registered.PartnerId, quoteId, -100m, 180, "Invalid"));
    }

    [Test]
    public async Task SubmitQuoteResponseAsync_ZeroDays_ThrowsArgumentException()
    {
        var (partner, profile) = MakeBuilder();
        var registered = await _sut.RegisterAsync(partner, profile);

        var quoteId = Guid.NewGuid();
        var model = new HomeModel(Guid.NewGuid(), "Villa", "A villa", ModelFormat.Gltf, 1024, 4, 2, 2, 200, Guid.NewGuid(), default);
        var block = new LandBlock(Guid.NewGuid(), "1 Test St", "TestSuburb", "NSW", "2000", 500, 15, 30, ZoningType.Residential, -33, 151);

        await _sut.RequestQuoteAsync(registered.PartnerId, quoteId, model, block);
        Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitQuoteResponseAsync(registered.PartnerId, quoteId, 100_000m, 0, "Invalid"));
    }

    private static (Partner Partner, BuilderProfile Profile) MakeBuilder() => (
        new Partner(Guid.Empty, "Smith Builders", PartnerCategory.Builder, "smith@builders.com.au", "+61400111222", ["NSW", "VIC"], true, default),
        new BuilderProfile(Guid.Empty, BuilderType.Volume, 2, 6, 200_000m, 800_000m, ["HIA", "MBA"], 500.0));
}
