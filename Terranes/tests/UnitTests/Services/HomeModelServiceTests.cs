using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Models3D;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class HomeModelServiceTests
{
    private HomeModelService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new HomeModelService(NullLogger<HomeModelService>.Instance);

    // ── 1. Creation ──

    [Test]
    public async Task CreateAsync_ValidModel_ReturnsWithGeneratedId()
    {
        var model = MakeModel();
        var created = await _sut.CreateAsync(model);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Name, Is.EqualTo("Modern Villa"));
        Assert.That(created.Format, Is.EqualTo(ModelFormat.Gltf));
    }

    [Test]
    public async Task CreateAsync_PreservesExistingId_WhenProvided()
    {
        var id = Guid.NewGuid();
        var model = MakeModel() with { Id = id };
        var created = await _sut.CreateAsync(model);

        Assert.That(created.Id, Is.EqualTo(id));
    }

    [Test]
    public void CreateAsync_EmptyName_ThrowsArgumentException()
    {
        var model = MakeModel() with { Name = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(model));
    }

    [Test]
    public void CreateAsync_ZeroFileSize_ThrowsArgumentException()
    {
        var model = MakeModel() with { FileSizeBytes = 0 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(model));
    }

    [Test]
    public void CreateAsync_ExceedsMaxFileSize_ThrowsArgumentException()
    {
        var model = MakeModel() with { FileSizeBytes = 600 * 1024 * 1024L };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(model));
    }

    [Test]
    public void CreateAsync_NegativeBedrooms_ThrowsArgumentException()
    {
        var model = MakeModel() with { Bedrooms = -1 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(model));
    }

    [Test]
    public void CreateAsync_ZeroFloorArea_ThrowsArgumentException()
    {
        var model = MakeModel() with { FloorAreaSquareMetres = 0 };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(model));
    }

    [Test]
    public async Task CreateAsync_DuplicateId_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var model = MakeModel() with { Id = id };
        await _sut.CreateAsync(model);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(model));
    }

    // ── 2. Retrieval ──

    [Test]
    public async Task GetByIdAsync_ExistingModel_ReturnsModel()
    {
        var created = await _sut.CreateAsync(MakeModel());
        var retrieved = await _sut.GetByIdAsync(created.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("Modern Villa"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ── 3. Search ──

    [Test]
    public async Task SearchAsync_ByMinBedrooms_FiltersCorrectly()
    {
        await _sut.CreateAsync(MakeModel() with { Bedrooms = 2 });
        await _sut.CreateAsync(MakeModel() with { Bedrooms = 4 });
        await _sut.CreateAsync(MakeModel() with { Bedrooms = 5 });

        var results = await _sut.SearchAsync(minBedrooms: 4);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(m => m.Bedrooms >= 4), Is.True);
    }

    [Test]
    public async Task SearchAsync_ByFormat_FiltersCorrectly()
    {
        await _sut.CreateAsync(MakeModel() with { Format = ModelFormat.Gltf });
        await _sut.CreateAsync(MakeModel() with { Format = ModelFormat.Fbx });

        var results = await _sut.SearchAsync(format: ModelFormat.Gltf);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Format, Is.EqualTo(ModelFormat.Gltf));
    }

    [Test]
    public async Task SearchAsync_NoFilters_ReturnsAll()
    {
        await _sut.CreateAsync(MakeModel());
        await _sut.CreateAsync(MakeModel());

        var results = await _sut.SearchAsync();
        Assert.That(results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SearchAsync_AllFormatsSupported()
    {
        foreach (var format in Enum.GetValues<ModelFormat>())
            await _sut.CreateAsync(MakeModel() with { Format = format });

        var all = await _sut.SearchAsync();
        Assert.That(all, Has.Count.EqualTo(5));
    }

    private static HomeModel MakeModel() => new(
        Guid.Empty, "Modern Villa", "A beautiful villa", ModelFormat.Gltf,
        1024 * 1024, 4, 2, 2, 220.0, Guid.NewGuid(), default);
}
