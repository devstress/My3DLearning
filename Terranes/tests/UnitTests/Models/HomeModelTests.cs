using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.UnitTests.Models;

[TestFixture]
public sealed class HomeModelTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesRecord()
    {
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var model = new HomeModel(
            Id: id,
            Name: "Modern Villa",
            Description: "A 4-bedroom modern villa with open plan living",
            Format: ModelFormat.Glb,
            FileSizeBytes: 15_000_000,
            Bedrooms: 4,
            Bathrooms: 2,
            GarageSpaces: 2,
            FloorAreaSquareMetres: 280.5,
            OwnerId: ownerId,
            CreatedAtUtc: createdAt);

        Assert.That(model.Id, Is.EqualTo(id));
        Assert.That(model.Name, Is.EqualTo("Modern Villa"));
        Assert.That(model.Format, Is.EqualTo(ModelFormat.Glb));
        Assert.That(model.Bedrooms, Is.EqualTo(4));
        Assert.That(model.FloorAreaSquareMetres, Is.EqualTo(280.5));
        Assert.That(model.OwnerId, Is.EqualTo(ownerId));
    }

    [Test]
    public void Equality_TwoIdenticalRecords_AreEqual()
    {
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var model1 = new HomeModel(id, "Villa", "Desc", ModelFormat.Gltf, 1000, 3, 2, 1, 200.0, ownerId, createdAt);
        var model2 = new HomeModel(id, "Villa", "Desc", ModelFormat.Gltf, 1000, 3, 2, 1, 200.0, ownerId, createdAt);

        Assert.That(model1, Is.EqualTo(model2));
    }

    [Test]
    public void With_ChangedBedrooms_CreatesNewRecordWithUpdatedValue()
    {
        var original = new HomeModel(
            Guid.NewGuid(), "Villa", "Desc", ModelFormat.Glb, 1000, 3, 2, 1, 200.0,
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        var modified = original with { Bedrooms = 5 };

        Assert.That(modified.Bedrooms, Is.EqualTo(5));
        Assert.That(modified.Name, Is.EqualTo(original.Name));
        Assert.That(modified.Id, Is.EqualTo(original.Id));
    }
}
