using Terranes.Contracts.Models;

namespace Terranes.UnitTests.Models;

[TestFixture]
public sealed class SitePlacementTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesRecord()
    {
        var id = Guid.NewGuid();
        var homeModelId = Guid.NewGuid();
        var landBlockId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var placedAt = DateTimeOffset.UtcNow;

        var placement = new SitePlacement(
            Id: id,
            HomeModelId: homeModelId,
            LandBlockId: landBlockId,
            OffsetXMetres: 3.5,
            OffsetYMetres: 5.0,
            RotationDegrees: 45.0,
            ScaleFactor: 1.0,
            PlacedByUserId: userId,
            PlacedAtUtc: placedAt);

        Assert.That(placement.Id, Is.EqualTo(id));
        Assert.That(placement.HomeModelId, Is.EqualTo(homeModelId));
        Assert.That(placement.LandBlockId, Is.EqualTo(landBlockId));
        Assert.That(placement.OffsetXMetres, Is.EqualTo(3.5));
        Assert.That(placement.RotationDegrees, Is.EqualTo(45.0));
        Assert.That(placement.ScaleFactor, Is.EqualTo(1.0));
    }

    [Test]
    public void With_UpdatedPosition_CreatesNewRecordWithChangedOffset()
    {
        var placement = new SitePlacement(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            0.0, 0.0, 0.0, 1.0, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var moved = placement with { OffsetXMetres = 10.0, OffsetYMetres = 8.0 };

        Assert.That(moved.OffsetXMetres, Is.EqualTo(10.0));
        Assert.That(moved.OffsetYMetres, Is.EqualTo(8.0));
        Assert.That(moved.Id, Is.EqualTo(placement.Id));
    }
}
