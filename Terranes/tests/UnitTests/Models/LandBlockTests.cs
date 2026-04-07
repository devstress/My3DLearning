using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.UnitTests.Models;

[TestFixture]
public sealed class LandBlockTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesRecord()
    {
        var id = Guid.NewGuid();

        var block = new LandBlock(
            Id: id,
            Address: "42 Eucalyptus Drive",
            Suburb: "Kellyville",
            State: "NSW",
            PostCode: "2155",
            AreaSquareMetres: 650.0,
            FrontageMetres: 18.5,
            DepthMetres: 35.0,
            Zoning: ZoningType.Residential,
            Latitude: -33.7035,
            Longitude: 150.9553);

        Assert.That(block.Id, Is.EqualTo(id));
        Assert.That(block.Address, Is.EqualTo("42 Eucalyptus Drive"));
        Assert.That(block.Suburb, Is.EqualTo("Kellyville"));
        Assert.That(block.State, Is.EqualTo("NSW"));
        Assert.That(block.Zoning, Is.EqualTo(ZoningType.Residential));
        Assert.That(block.AreaSquareMetres, Is.EqualTo(650.0));
    }

    [Test]
    public void Equality_TwoIdenticalBlocks_AreEqual()
    {
        var id = Guid.NewGuid();

        var block1 = new LandBlock(id, "1 Main St", "Sydney", "NSW", "2000", 500.0, 15.0, 33.3, ZoningType.MixedUse, -33.8688, 151.2093);
        var block2 = new LandBlock(id, "1 Main St", "Sydney", "NSW", "2000", 500.0, 15.0, 33.3, ZoningType.MixedUse, -33.8688, 151.2093);

        Assert.That(block1, Is.EqualTo(block2));
    }
}
