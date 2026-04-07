using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.UnitTests.Models;

[TestFixture]
public sealed class PartnerTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesRecord()
    {
        var id = Guid.NewGuid();
        var registeredAt = DateTimeOffset.UtcNow;

        var partner = new Partner(
            Id: id,
            BusinessName: "Smith & Sons Builders",
            Category: PartnerCategory.Builder,
            ContactEmail: "info@smithsons.com.au",
            ContactPhone: "02 9876 5432",
            ServiceRegions: ["NSW", "VIC"],
            IsActive: true,
            RegisteredAtUtc: registeredAt);

        Assert.That(partner.Id, Is.EqualTo(id));
        Assert.That(partner.BusinessName, Is.EqualTo("Smith & Sons Builders"));
        Assert.That(partner.Category, Is.EqualTo(PartnerCategory.Builder));
        Assert.That(partner.ServiceRegions, Has.Count.EqualTo(2));
        Assert.That(partner.ServiceRegions, Does.Contain("NSW"));
        Assert.That(partner.IsActive, Is.True);
    }

    [Test]
    public void With_Deactivated_CreatesNewRecordWithIsActiveFalse()
    {
        var partner = new Partner(
            Guid.NewGuid(), "Landscapers Inc", PartnerCategory.Landscaper,
            "info@landscapers.com.au", "03 1234 5678", ["VIC"],
            true, DateTimeOffset.UtcNow);

        var deactivated = partner with { IsActive = false };

        Assert.That(deactivated.IsActive, Is.False);
        Assert.That(deactivated.BusinessName, Is.EqualTo("Landscapers Inc"));
    }
}
