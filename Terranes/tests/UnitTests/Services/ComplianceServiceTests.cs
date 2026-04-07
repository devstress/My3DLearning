using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Compliance;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Land;
using Terranes.Models3D;
using Terranes.SitePlacementEngine;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class ComplianceServiceTests
{
    private HomeModelService _homeModelService = null!;
    private LandBlockService _landBlockService = null!;
    private SitePlacementService _sitePlacementService = null!;
    private ComplianceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _homeModelService = new HomeModelService(NullLogger<HomeModelService>.Instance);
        _landBlockService = new LandBlockService(NullLogger<LandBlockService>.Instance);
        _sitePlacementService = new SitePlacementService(_homeModelService, _landBlockService, NullLogger<SitePlacementService>.Instance);
        _sut = new ComplianceService(_sitePlacementService, _homeModelService, _landBlockService, NullLogger<ComplianceService>.Instance);
    }

    // ── 1. Compliant Scenarios ──

    [Test]
    public async Task CheckAsync_SmallModelLargeBlock_ReturnsCompliant()
    {
        var (_, _, placement) = await CreateStandardPlacement(floorArea: 100, blockArea: 600, frontage: 20, depth: 30, offsetX: 3, offsetY: 3);
        var result = await _sut.CheckAsync(placement.Id, "NSW");

        Assert.That(result.Outcome, Is.EqualTo(ComplianceOutcome.Compliant));
        Assert.That(result.Violations, Is.Empty);
        Assert.That(result.Jurisdiction, Is.EqualTo("NSW"));
    }

    [Test]
    public async Task CheckAsync_GeneratesUniqueId()
    {
        var (_, _, placement) = await CreateStandardPlacement(floorArea: 100, blockArea: 600, frontage: 20, depth: 30, offsetX: 3, offsetY: 3);
        var result = await _sut.CheckAsync(placement.Id, "NSW");

        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
    }

    // ── 2. Violation Scenarios ──

    [Test]
    public async Task CheckAsync_ExceedsSiteCoverage_ReturnsNonCompliant()
    {
        // 400m² floor on 500m² block = 80% coverage (> 60% max)
        var (_, _, placement) = await CreateStandardPlacement(floorArea: 400, blockArea: 500, frontage: 25, depth: 20, offsetX: 2, offsetY: 2);
        var result = await _sut.CheckAsync(placement.Id, "VIC");

        Assert.That(result.Outcome, Is.EqualTo(ComplianceOutcome.NonCompliant));
        Assert.That(result.Violations.Any(v => v.RuleCode == "BCA-COVERAGE-001"), Is.True);
    }

    [Test]
    public async Task CheckAsync_InsufficientSetback_ReturnsNonCompliant()
    {
        var (_, _, placement) = await CreateStandardPlacement(floorArea: 80, blockArea: 600, frontage: 20, depth: 30, offsetX: 0.5, offsetY: 3);
        var result = await _sut.CheckAsync(placement.Id, "QLD");

        Assert.That(result.Outcome, Is.EqualTo(ComplianceOutcome.NonCompliant));
        Assert.That(result.Violations.Any(v => v.RuleCode == "BCA-SETBACK-001"), Is.True);
    }

    [Test]
    public async Task CheckAsync_BlockBelowMinLotSize_ReturnsNonCompliant()
    {
        // 200m² lot for Residential (min 300m²)
        var (_, _, placement) = await CreateStandardPlacement(
            floorArea: 50, blockArea: 200, frontage: 10, depth: 20, offsetX: 2, offsetY: 2,
            zoning: ZoningType.Residential);
        var result = await _sut.CheckAsync(placement.Id, "SA");

        Assert.That(result.Violations.Any(v => v.RuleCode == "BCA-LOTSIZE-001"), Is.True);
    }

    [Test]
    public async Task CheckAsync_NarrowFrontage_ReturnsConditionallyCompliant()
    {
        // 8m frontage for Residential (min 10m), but everything else OK
        var (_, _, placement) = await CreateStandardPlacement(
            floorArea: 80, blockArea: 400, frontage: 8, depth: 50, offsetX: 2, offsetY: 2,
            zoning: ZoningType.Residential);
        var result = await _sut.CheckAsync(placement.Id, "NSW");

        Assert.That(result.Violations.Any(v => v.RuleCode == "BCA-FRONTAGE-001"), Is.True);
        Assert.That(result.Outcome, Is.EqualTo(ComplianceOutcome.ConditionallyCompliant));
    }

    [Test]
    public async Task CheckAsync_MediumDensityZoning_AllowsSmallerLots()
    {
        // 250m² lot for MediumDensity (min 200m²) — should pass
        var (_, _, placement) = await CreateStandardPlacement(
            floorArea: 60, blockArea: 250, frontage: 12, depth: 21, offsetX: 2, offsetY: 2,
            zoning: ZoningType.ResidentialMediumDensity);
        var result = await _sut.CheckAsync(placement.Id, "VIC");

        Assert.That(result.Violations.Where(v => v.RuleCode == "BCA-LOTSIZE-001"), Is.Empty);
    }

    // ── 3. Retrieval & Error Handling ──

    [Test]
    public async Task GetByIdAsync_ExistingResult_ReturnsResult()
    {
        var (_, _, placement) = await CreateStandardPlacement(floorArea: 100, blockArea: 600, frontage: 20, depth: 30, offsetX: 3, offsetY: 3);
        var result = await _sut.CheckAsync(placement.Id, "NSW");

        var retrieved = await _sut.GetByIdAsync(result.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Jurisdiction, Is.EqualTo("NSW"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetBySitePlacementAsync_MultipleChecks_ReturnsAll()
    {
        var (_, _, placement) = await CreateStandardPlacement(floorArea: 100, blockArea: 600, frontage: 20, depth: 30, offsetX: 3, offsetY: 3);
        await _sut.CheckAsync(placement.Id, "NSW");
        await _sut.CheckAsync(placement.Id, "VIC");

        var results = await _sut.GetBySitePlacementAsync(placement.Id);
        Assert.That(results, Has.Count.EqualTo(2));
    }

    [Test]
    public void CheckAsync_EmptyJurisdiction_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CheckAsync(Guid.NewGuid(), ""));
    }

    [Test]
    public void CheckAsync_NonExistentPlacement_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CheckAsync(Guid.NewGuid(), "NSW"));
    }

    private async Task<(HomeModel Model, LandBlock Block, Terranes.Contracts.Models.SitePlacement Placement)> CreateStandardPlacement(
        double floorArea, double blockArea, double frontage, double depth,
        double offsetX, double offsetY, ZoningType zoning = ZoningType.Residential)
    {
        var model = await _homeModelService.CreateAsync(new HomeModel(
            Guid.Empty, "Test Home", "A test home", ModelFormat.Gltf,
            1024 * 1024, 3, 2, 1, floorArea, Guid.NewGuid(), default));

        var block = await _landBlockService.CreateAsync(new LandBlock(
            Guid.Empty, "1 Test St", "TestSuburb", "NSW", "2000",
            blockArea, frontage, depth, zoning, -33.0, 151.0));

        var placement = await _sitePlacementService.PlaceAsync(new Terranes.Contracts.Models.SitePlacement(
            Guid.Empty, model.Id, block.Id, offsetX, offsetY, 0, 1.0, Guid.NewGuid(), default));

        return (model, block, placement);
    }
}
