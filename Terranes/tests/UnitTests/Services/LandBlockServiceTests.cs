using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Land;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class LandBlockServiceTests
{
    private LandBlockService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new LandBlockService(NullLogger<LandBlockService>.Instance);

    // ── 1. Creation ──

    [Test]
    public async Task CreateAsync_ValidBlock_ReturnsWithGeneratedId()
    {
        var block = MakeBlock();
        var created = await _sut.CreateAsync(block);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Address, Is.EqualTo("12 Smith Street"));
    }

    [Test]
    public void CreateAsync_EmptyAddress_ThrowsArgumentException()
    {
        var block = MakeBlock() with { Address = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(block));
    }

    [Test]
    public void CreateAsync_EmptyState_ThrowsArgumentException()
    {
        var block = MakeBlock() with { State = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(block));
    }

    [Test]
    public void CreateAsync_ZeroArea_ThrowsArgumentException()
    {
        var block = MakeBlock() with { AreaSquareMetres = 0 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(block));
    }

    [Test]
    public void CreateAsync_ZeroFrontage_ThrowsArgumentException()
    {
        var block = MakeBlock() with { FrontageMetres = 0 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(block));
    }

    [Test]
    public void CreateAsync_ZeroDepth_ThrowsArgumentException()
    {
        var block = MakeBlock() with { DepthMetres = 0 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(block));
    }

    // ── 2. Retrieval ──

    [Test]
    public async Task GetByIdAsync_ExistingBlock_ReturnsBlock()
    {
        var created = await _sut.CreateAsync(MakeBlock());
        var retrieved = await _sut.GetByIdAsync(created.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Address, Is.EqualTo("12 Smith Street"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task LookupByAddressAsync_Matching_ReturnsBlock()
    {
        await _sut.CreateAsync(MakeBlock());
        var found = await _sut.LookupByAddressAsync("Smith Street", "NSW");

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Suburb, Is.EqualTo("Bella Vista"));
    }

    [Test]
    public async Task LookupByAddressAsync_NoMatch_ReturnsNull()
    {
        await _sut.CreateAsync(MakeBlock());
        var result = await _sut.LookupByAddressAsync("NonExistent Road", "NSW");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task LookupByAddressAsync_WrongState_ReturnsNull()
    {
        await _sut.CreateAsync(MakeBlock());
        var result = await _sut.LookupByAddressAsync("Smith Street", "VIC");

        Assert.That(result, Is.Null);
    }

    // ── 3. Search ──

    [Test]
    public async Task SearchAsync_BySuburb_FiltersCorrectly()
    {
        await _sut.CreateAsync(MakeBlock() with { Suburb = "Bella Vista" });
        await _sut.CreateAsync(MakeBlock() with { Suburb = "Castle Hill" });

        var results = await _sut.SearchAsync(suburb: "Bella Vista");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Suburb, Is.EqualTo("Bella Vista"));
    }

    [Test]
    public async Task SearchAsync_ByMinArea_FiltersCorrectly()
    {
        await _sut.CreateAsync(MakeBlock() with { AreaSquareMetres = 300 });
        await _sut.CreateAsync(MakeBlock() with { AreaSquareMetres = 600 });

        var results = await _sut.SearchAsync(minAreaSqm: 500);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].AreaSquareMetres, Is.EqualTo(600));
    }

    [Test]
    public async Task SearchAsync_NoFilters_ReturnsAll()
    {
        await _sut.CreateAsync(MakeBlock());
        await _sut.CreateAsync(MakeBlock() with { Address = "14 Jones Ave" });

        var results = await _sut.SearchAsync();
        Assert.That(results, Has.Count.EqualTo(2));
    }

    private static LandBlock MakeBlock() => new(
        Guid.Empty, "12 Smith Street", "Bella Vista", "NSW", "2153",
        450.0, 15.0, 30.0, ZoningType.Residential, -33.7290, 150.9574);
}
