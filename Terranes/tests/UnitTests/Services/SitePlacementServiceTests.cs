using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Land;
using Terranes.Models3D;
using Terranes.SitePlacementEngine;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class SitePlacementServiceTests
{
    private HomeModelService _homeModelService = null!;
    private LandBlockService _landBlockService = null!;
    private SitePlacementService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _homeModelService = new HomeModelService(NullLogger<HomeModelService>.Instance);
        _landBlockService = new LandBlockService(NullLogger<LandBlockService>.Instance);
        _sut = new SitePlacementService(_homeModelService, _landBlockService, NullLogger<SitePlacementService>.Instance);
    }

    // ── 1. Placement ──

    [Test]
    public async Task PlaceAsync_ValidPlacement_ReturnsWithGeneratedId()
    {
        var placement = MakePlacement();
        var created = await _sut.PlaceAsync(placement);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.OffsetXMetres, Is.EqualTo(2.0));
    }

    [Test]
    public void PlaceAsync_ZeroScaleFactor_ThrowsArgumentException()
    {
        var placement = MakePlacement() with { ScaleFactor = 0 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.PlaceAsync(placement));
    }

    [Test]
    public void PlaceAsync_NegativeRotation_ThrowsArgumentException()
    {
        var placement = MakePlacement() with { RotationDegrees = -10 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.PlaceAsync(placement));
    }

    [Test]
    public void PlaceAsync_RotationExceeds360_ThrowsArgumentException()
    {
        var placement = MakePlacement() with { RotationDegrees = 360 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.PlaceAsync(placement));
    }

    // ── 2. Retrieval ──

    [Test]
    public async Task GetByIdAsync_ExistingPlacement_ReturnsPlacement()
    {
        var created = await _sut.PlaceAsync(MakePlacement());
        var retrieved = await _sut.GetByIdAsync(created.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.HomeModelId, Is.EqualTo(created.HomeModelId));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ── 3. Fit Validation ──

    [Test]
    public async Task ValidateFitAsync_SmallModelLargeBlock_ReturnsTrue()
    {
        var model = await _homeModelService.CreateAsync(new HomeModel(
            Guid.Empty, "Compact Home", "Small home", ModelFormat.Gltf,
            1024 * 1024, 3, 1, 1, 80.0, Guid.NewGuid(), default));

        var block = await _landBlockService.CreateAsync(new LandBlock(
            Guid.Empty, "10 Test St", "TestSuburb", "NSW", "2000",
            600.0, 20.0, 30.0, ZoningType.Residential, -33.0, 151.0));

        var placement = new Terranes.Contracts.Models.SitePlacement(
            Guid.Empty, model.Id, block.Id, 2.0, 2.0, 0, 1.0, Guid.NewGuid(), default);

        var fits = await _sut.ValidateFitAsync(placement);
        Assert.That(fits, Is.True);
    }

    [Test]
    public async Task ValidateFitAsync_LargeModelSmallBlock_ReturnsFalse()
    {
        var model = await _homeModelService.CreateAsync(new HomeModel(
            Guid.Empty, "Mansion", "Huge mansion", ModelFormat.Gltf,
            1024 * 1024, 6, 4, 3, 500.0, Guid.NewGuid(), default));

        var block = await _landBlockService.CreateAsync(new LandBlock(
            Guid.Empty, "5 Tiny Lane", "SmallTown", "VIC", "3000",
            200.0, 10.0, 20.0, ZoningType.Residential, -37.0, 145.0));

        var placement = new Terranes.Contracts.Models.SitePlacement(
            Guid.Empty, model.Id, block.Id, 0.0, 0.0, 0, 1.0, Guid.NewGuid(), default);

        var fits = await _sut.ValidateFitAsync(placement);
        Assert.That(fits, Is.False);
    }

    [Test]
    public async Task ValidateFitAsync_ModelNotFound_ReturnsFalse()
    {
        var placement = new Terranes.Contracts.Models.SitePlacement(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 0, 0, 0, 1.0, Guid.NewGuid(), default);

        var fits = await _sut.ValidateFitAsync(placement);
        Assert.That(fits, Is.False);
    }

    [Test]
    public async Task ValidateFitAsync_BlockNotFound_ReturnsFalse()
    {
        var model = await _homeModelService.CreateAsync(new HomeModel(
            Guid.Empty, "Test", "Test", ModelFormat.Gltf,
            1024, 3, 1, 1, 100.0, Guid.NewGuid(), default));

        var placement = new Terranes.Contracts.Models.SitePlacement(
            Guid.Empty, model.Id, Guid.NewGuid(), 0, 0, 0, 1.0, Guid.NewGuid(), default);

        var fits = await _sut.ValidateFitAsync(placement);
        Assert.That(fits, Is.False);
    }

    [Test]
    public async Task ValidateFitAsync_OffsetPushesModelOffBlock_ReturnsFalse()
    {
        var model = await _homeModelService.CreateAsync(new HomeModel(
            Guid.Empty, "Mid Home", "Medium home", ModelFormat.Gltf,
            1024 * 1024, 3, 2, 1, 120.0, Guid.NewGuid(), default));

        var block = await _landBlockService.CreateAsync(new LandBlock(
            Guid.Empty, "22 Edge Rd", "EdgeTown", "QLD", "4000",
            450.0, 15.0, 30.0, ZoningType.Residential, -27.0, 153.0));

        // Large offset pushes model past usable area
        var placement = new Terranes.Contracts.Models.SitePlacement(
            Guid.Empty, model.Id, block.Id, 10.0, 25.0, 0, 1.0, Guid.NewGuid(), default);

        var fits = await _sut.ValidateFitAsync(placement);
        Assert.That(fits, Is.False);
    }

    [Test]
    public async Task ValidateFitAsync_ScaleFactorIncreasesFootprint()
    {
        var model = await _homeModelService.CreateAsync(new HomeModel(
            Guid.Empty, "Scalable", "Scalable home", ModelFormat.Gltf,
            1024 * 1024, 3, 1, 1, 80.0, Guid.NewGuid(), default));

        var block = await _landBlockService.CreateAsync(new LandBlock(
            Guid.Empty, "33 Scale St", "ScaleVille", "NSW", "2000",
            400.0, 15.0, 27.0, ZoningType.Residential, -33.0, 151.0));

        // Scale 1.0 fits, scale 2.0 doesn't
        var placement1 = new Terranes.Contracts.Models.SitePlacement(
            Guid.Empty, model.Id, block.Id, 0.0, 0.0, 0, 1.0, Guid.NewGuid(), default);
        var placement2 = new Terranes.Contracts.Models.SitePlacement(
            Guid.Empty, model.Id, block.Id, 0.0, 0.0, 0, 2.0, Guid.NewGuid(), default);

        Assert.That(await _sut.ValidateFitAsync(placement1), Is.True);
        Assert.That(await _sut.ValidateFitAsync(placement2), Is.False);
    }

    private static Terranes.Contracts.Models.SitePlacement MakePlacement() => new(
        Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 2.0, 3.0, 45.0, 1.0, Guid.NewGuid(), default);
}
