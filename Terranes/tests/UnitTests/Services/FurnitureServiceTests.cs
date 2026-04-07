using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.PartnerIntegration;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class FurnitureServiceTests
{
    private FurnitureService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new FurnitureService(NullLogger<FurnitureService>.Instance);

    // ── 1. Catalog Management ──

    [Test]
    public async Task AddItemAsync_ValidItem_ReturnsWithGeneratedId()
    {
        var item = MakeItem();
        var created = await _sut.AddItemAsync(item);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Name, Is.EqualTo("Oak Dining Table"));
    }

    [Test]
    public void AddItemAsync_EmptyName_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddItemAsync(MakeItem() with { Name = "" }));
    }

    [Test]
    public void AddItemAsync_NegativePrice_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddItemAsync(MakeItem() with { PriceAud = -1m }));
    }

    [Test]
    public void AddItemAsync_EmptySku_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddItemAsync(MakeItem() with { Sku = "" }));
    }

    [Test]
    public void AddItemAsync_ZeroDimension_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddItemAsync(MakeItem() with { WidthMetres = 0 }));
    }

    [Test]
    public async Task GetItemAsync_ExistingItem_ReturnsItem()
    {
        var created = await _sut.AddItemAsync(MakeItem());
        var retrieved = await _sut.GetItemAsync(created.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Sku, Is.EqualTo("FURN-DIN-001"));
    }

    [Test]
    public async Task GetItemAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetItemAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ── 2. Search & Filtering ──

    [Test]
    public async Task SearchCatalogAsync_ByCategory_FiltersCorrectly()
    {
        await _sut.AddItemAsync(MakeItem() with { Category = FurnitureCategory.Dining });
        await _sut.AddItemAsync(MakeItem() with { Category = FurnitureCategory.LivingRoom, Name = "Sofa", Sku = "FURN-LIV-001" });

        var results = await _sut.SearchCatalogAsync(category: FurnitureCategory.Dining);
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchCatalogAsync_ByMaxPrice_FiltersCorrectly()
    {
        await _sut.AddItemAsync(MakeItem() with { PriceAud = 500m });
        await _sut.AddItemAsync(MakeItem() with { PriceAud = 2000m, Sku = "FURN-DIN-002" });

        var results = await _sut.SearchCatalogAsync(maxPrice: 1000m);
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].PriceAud, Is.EqualTo(500m));
    }

    [Test]
    public async Task SearchCatalogAsync_ExcludesOutOfStock()
    {
        await _sut.AddItemAsync(MakeItem() with { InStock = true });
        await _sut.AddItemAsync(MakeItem() with { InStock = false, Sku = "FURN-DIN-OOS" });

        var results = await _sut.SearchCatalogAsync();
        Assert.That(results, Has.Count.EqualTo(1));
    }

    // ── 3. Room Fitting & Pricing ──

    [Test]
    public async Task FitItemAsync_ValidFitting_ReturnsWithId()
    {
        var item = await _sut.AddItemAsync(MakeItem());
        var fitting = new RoomFitting(Guid.Empty, Guid.NewGuid(), item.Id, "Dining Room", 2.0, 3.0, 0);
        var created = await _sut.FitItemAsync(fitting);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.RoomName, Is.EqualTo("Dining Room"));
    }

    [Test]
    public async Task FitItemAsync_EmptyRoomName_ThrowsArgumentException()
    {
        var item = await _sut.AddItemAsync(MakeItem());
        var fitting = new RoomFitting(Guid.Empty, Guid.NewGuid(), item.Id, "", 2.0, 3.0, 0);
        Assert.ThrowsAsync<ArgumentException>(() => _sut.FitItemAsync(fitting));
    }

    [Test]
    public void FitItemAsync_NonExistentItem_ThrowsInvalidOperationException()
    {
        var fitting = new RoomFitting(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Dining Room", 2.0, 3.0, 0);
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.FitItemAsync(fitting));
    }

    [Test]
    public async Task FitItemAsync_InvalidRotation_ThrowsArgumentException()
    {
        var item = await _sut.AddItemAsync(MakeItem());
        var fitting = new RoomFitting(Guid.Empty, Guid.NewGuid(), item.Id, "Room", 2.0, 3.0, 360);
        Assert.ThrowsAsync<ArgumentException>(() => _sut.FitItemAsync(fitting));
    }

    [Test]
    public async Task CalculateTotalAsync_MultipleFittings_SumsCorrectly()
    {
        var modelId = Guid.NewGuid();
        var item1 = await _sut.AddItemAsync(MakeItem() with { PriceAud = 1200m });
        var item2 = await _sut.AddItemAsync(MakeItem() with { PriceAud = 800m, Sku = "FURN-DIN-002" });

        await _sut.FitItemAsync(new RoomFitting(Guid.Empty, modelId, item1.Id, "Dining Room", 0, 0, 0));
        await _sut.FitItemAsync(new RoomFitting(Guid.Empty, modelId, item2.Id, "Kitchen", 0, 0, 0));

        var total = await _sut.CalculateTotalAsync(modelId);
        Assert.That(total, Is.EqualTo(2000m));
    }

    [Test]
    public async Task CalculateTotalAsync_NoFittings_ReturnsZero()
    {
        var total = await _sut.CalculateTotalAsync(Guid.NewGuid());
        Assert.That(total, Is.EqualTo(0m));
    }

    [Test]
    public async Task GetFittingsForModelAsync_MultipleFittings_ReturnsAll()
    {
        var modelId = Guid.NewGuid();
        var item = await _sut.AddItemAsync(MakeItem());

        await _sut.FitItemAsync(new RoomFitting(Guid.Empty, modelId, item.Id, "Dining Room", 0, 0, 0));
        await _sut.FitItemAsync(new RoomFitting(Guid.Empty, modelId, item.Id, "Kitchen", 1, 1, 90));

        var fittings = await _sut.GetFittingsForModelAsync(modelId);
        Assert.That(fittings, Has.Count.EqualTo(2));
    }

    private static FurnitureItem MakeItem() => new(
        Guid.Empty, Guid.NewGuid(), "Oak Dining Table", FurnitureCategory.Dining,
        1200m, 1.8, 0.9, 0.75, "FURN-DIN-001", true);
}
