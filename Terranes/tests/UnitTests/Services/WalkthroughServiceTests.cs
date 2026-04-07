using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Immersive3D;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class WalkthroughServiceTests
{
    private WalkthroughService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new WalkthroughService(NullLogger<WalkthroughService>.Instance);

    // ── 1. Generation ──

    [Test]
    public async Task GenerateAsync_ValidInput_ReturnsReadyWalkthrough()
    {
        var modelId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var wt = await _sut.GenerateAsync(modelId, null, userId);

        Assert.That(wt.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(wt.Status, Is.EqualTo(WalkthroughStatus.Ready));
        Assert.That(wt.HomeModelId, Is.EqualTo(modelId));
        Assert.That(wt.TotalRooms, Is.GreaterThan(0));
    }

    [Test]
    public void GenerateAsync_EmptyModelId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.GenerateAsync(Guid.Empty, null, Guid.NewGuid()));
    }

    [Test]
    public void GenerateAsync_EmptyUserId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.GenerateAsync(Guid.NewGuid(), null, Guid.Empty));
    }

    [Test]
    public async Task GenerateAsync_WithSitePlacement_IncludesPlacementRef()
    {
        var placementId = Guid.NewGuid();
        var wt = await _sut.GenerateAsync(Guid.NewGuid(), placementId, Guid.NewGuid());

        Assert.That(wt.SitePlacementId, Is.EqualTo(placementId));
    }

    // ── 2. POI Management ──

    [Test]
    public async Task AddPoiAsync_ValidPoi_ReturnsPoi()
    {
        var wt = await _sut.GenerateAsync(Guid.NewGuid(), null, Guid.NewGuid());
        var poi = MakePoi(wt.Id);

        var created = await _sut.AddPoiAsync(poi);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Label, Is.EqualTo("Kitchen Island"));
    }

    [Test]
    public void AddPoiAsync_EmptyLabel_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddPoiAsync(MakePoi(Guid.NewGuid())));
    }

    [Test]
    public async Task AddPoiAsync_NonExistentWalkthrough_ThrowsInvalidOperationException()
    {
        var poi = MakePoi(Guid.NewGuid());
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddPoiAsync(poi));
    }

    // ── 3. Retrieval & Filtering ──

    [Test]
    public async Task GetByHomeModelAsync_ReturnsMatchingWalkthroughs()
    {
        var modelId = Guid.NewGuid();
        await _sut.GenerateAsync(modelId, null, Guid.NewGuid());
        await _sut.GenerateAsync(modelId, null, Guid.NewGuid());
        await _sut.GenerateAsync(Guid.NewGuid(), null, Guid.NewGuid()); // different model

        var results = await _sut.GetByHomeModelAsync(modelId);
        Assert.That(results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetPoisByRoomAsync_FiltersCorrectly()
    {
        var wt = await _sut.GenerateAsync(Guid.NewGuid(), null, Guid.NewGuid());
        await _sut.AddPoiAsync(MakePoi(wt.Id) with { RoomName = "Kitchen" });
        await _sut.AddPoiAsync(MakePoi(wt.Id) with { RoomName = "Kitchen", Label = "Sink" });
        await _sut.AddPoiAsync(MakePoi(wt.Id) with { RoomName = "Bedroom", Label = "Bed" });

        var kitchenPois = await _sut.GetPoisByRoomAsync(wt.Id, "Kitchen");
        Assert.That(kitchenPois, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsCorrectWalkthrough()
    {
        var wt = await _sut.GenerateAsync(Guid.NewGuid(), null, Guid.NewGuid());
        var retrieved = await _sut.GetByIdAsync(wt.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Id, Is.EqualTo(wt.Id));
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    private static WalkthroughPoi MakePoi(Guid walkthroughId) => new(
        Guid.Empty, walkthroughId, WalkthroughPoiType.Feature,
        "Kitchen Island", "Large stone benchtop island",
        5.0, 3.0, 1.0, "Kitchen");
}
