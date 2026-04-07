using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Immersive3D;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class VirtualVillageServiceTests
{
    private VirtualVillageService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new VirtualVillageService(NullLogger<VirtualVillageService>.Instance);

    // ── 1. Village Creation ──

    [Test]
    public async Task CreateAsync_ValidVillage_ReturnsWithGeneratedId()
    {
        var village = MakeVillage();
        var created = await _sut.CreateAsync(village);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Name, Is.EqualTo("Sunrise Gardens"));
        Assert.That(created.Layout, Is.EqualTo(VillageLayoutType.Grid));
    }

    [Test]
    public void CreateAsync_EmptyName_ThrowsArgumentException()
    {
        var village = MakeVillage() with { Name = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(village));
    }

    [Test]
    public void CreateAsync_ZeroMaxLots_ThrowsArgumentException()
    {
        var village = MakeVillage() with { MaxLots = 0 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(village));
    }

    [Test]
    public void CreateAsync_ExceedsMaxLots_ThrowsArgumentException()
    {
        var village = MakeVillage() with { MaxLots = 501 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(village));
    }

    // ── 2. Lot Management ──

    [Test]
    public async Task AddLotAsync_ValidLot_ReturnsVacantLot()
    {
        var village = await _sut.CreateAsync(MakeVillage());
        var lot = MakeLot(village.Id);
        var created = await _sut.AddLotAsync(lot);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Status, Is.EqualTo(VillageLotStatus.Vacant));
        Assert.That(created.SitePlacementId, Is.Null);
    }

    [Test]
    public async Task AddLotAsync_ExceedsMaxLots_ThrowsInvalidOperationException()
    {
        var village = await _sut.CreateAsync(MakeVillage() with { MaxLots = 1 });
        await _sut.AddLotAsync(MakeLot(village.Id, 1));

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddLotAsync(MakeLot(village.Id, 2)));
    }

    [Test]
    public async Task AddLotAsync_DuplicateLotNumber_ThrowsInvalidOperationException()
    {
        var village = await _sut.CreateAsync(MakeVillage());
        await _sut.AddLotAsync(MakeLot(village.Id, 1));

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddLotAsync(MakeLot(village.Id, 1)));
    }

    [Test]
    public void AddLotAsync_ZeroWidth_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddLotAsync(MakeLot(Guid.NewGuid())));
    }

    [Test]
    public async Task GetLotsAsync_ReturnsAllLotsSorted()
    {
        var village = await _sut.CreateAsync(MakeVillage());
        await _sut.AddLotAsync(MakeLot(village.Id, 3));
        await _sut.AddLotAsync(MakeLot(village.Id, 1));
        await _sut.AddLotAsync(MakeLot(village.Id, 2));

        var lots = await _sut.GetLotsAsync(village.Id);

        Assert.That(lots, Has.Count.EqualTo(3));
        Assert.That(lots[0].LotNumber, Is.EqualTo(1));
        Assert.That(lots[2].LotNumber, Is.EqualTo(3));
    }

    // ── 3. Placement & Stats ──

    [Test]
    public async Task AssignPlacementAsync_VacantLot_UpdatesToOccupied()
    {
        var village = await _sut.CreateAsync(MakeVillage());
        var lot = await _sut.AddLotAsync(MakeLot(village.Id));
        var placementId = Guid.NewGuid();

        var updated = await _sut.AssignPlacementAsync(lot.Id, placementId);

        Assert.That(updated.Status, Is.EqualTo(VillageLotStatus.Occupied));
        Assert.That(updated.SitePlacementId, Is.EqualTo(placementId));
    }

    [Test]
    public async Task AssignPlacementAsync_OccupiedLot_ThrowsInvalidOperationException()
    {
        var village = await _sut.CreateAsync(MakeVillage());
        var lot = await _sut.AddLotAsync(MakeLot(village.Id));
        await _sut.AssignPlacementAsync(lot.Id, Guid.NewGuid());

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AssignPlacementAsync(lot.Id, Guid.NewGuid()));
    }

    [Test]
    public async Task GetStatsAsync_ReturnsCorrectCounts()
    {
        var village = await _sut.CreateAsync(MakeVillage());
        var lot1 = await _sut.AddLotAsync(MakeLot(village.Id, 1));
        await _sut.AddLotAsync(MakeLot(village.Id, 2));
        await _sut.AssignPlacementAsync(lot1.Id, Guid.NewGuid());

        var (total, occupied, vacant) = await _sut.GetStatsAsync(village.Id);

        Assert.That(total, Is.EqualTo(2));
        Assert.That(occupied, Is.EqualTo(1));
        Assert.That(vacant, Is.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_ByLayout_FiltersCorrectly()
    {
        await _sut.CreateAsync(MakeVillage() with { Layout = VillageLayoutType.Grid });
        await _sut.CreateAsync(MakeVillage() with { Layout = VillageLayoutType.CulDeSac });

        var results = await _sut.SearchAsync(layout: VillageLayoutType.Grid);
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_ByName_FiltersCorrectly()
    {
        await _sut.CreateAsync(MakeVillage() with { Name = "Sunrise Gardens" });
        await _sut.CreateAsync(MakeVillage() with { Name = "Moonlight Terrace" });

        var results = await _sut.SearchAsync(name: "sunrise");
        Assert.That(results, Has.Count.EqualTo(1));
    }

    private static VirtualVillage MakeVillage() => new(
        Guid.Empty, "Sunrise Gardens", "A beautiful neighbourhood", VillageLayoutType.Grid,
        50, -33.87, 151.21, Guid.NewGuid(), default);

    private static VillageLot MakeLot(Guid villageId, int lotNumber = 1) => new(
        Guid.Empty, villageId, lotNumber, lotNumber * 20.0, 0, 15.0, 30.0,
        VillageLotStatus.Vacant, null);
}
