using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.UnitTests.Models;

[TestFixture]
public sealed class QuoteLineItemTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesRecord()
    {
        var id = Guid.NewGuid();
        var quoteRequestId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var validUntil = DateTimeOffset.UtcNow.AddDays(30);
        var providedAt = DateTimeOffset.UtcNow;

        var item = new QuoteLineItem(
            Id: id,
            QuoteRequestId: quoteRequestId,
            PartnerId: partnerId,
            Category: PartnerCategory.Builder,
            AmountAud: 450_000m,
            Description: "4-bedroom single-storey home construction",
            ValidUntilUtc: validUntil,
            ProvidedAtUtc: providedAt);

        Assert.That(item.Id, Is.EqualTo(id));
        Assert.That(item.Category, Is.EqualTo(PartnerCategory.Builder));
        Assert.That(item.AmountAud, Is.EqualTo(450_000m));
        Assert.That(item.Description, Is.EqualTo("4-bedroom single-storey home construction"));
    }

    [Test]
    public void MultipleLineItems_DifferentCategories_RepresentEndToEndQuote()
    {
        var quoteRequestId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var validUntil = now.AddDays(30);

        var builder = new QuoteLineItem(Guid.NewGuid(), quoteRequestId, Guid.NewGuid(),
            PartnerCategory.Builder, 450_000m, "Home construction", validUntil, now);

        var landscaper = new QuoteLineItem(Guid.NewGuid(), quoteRequestId, Guid.NewGuid(),
            PartnerCategory.Landscaper, 35_000m, "Front and rear landscaping", validUntil, now);

        var furniture = new QuoteLineItem(Guid.NewGuid(), quoteRequestId, Guid.NewGuid(),
            PartnerCategory.InteriorFurnishing, 25_000m, "Full house furniture package", validUntil, now);

        var smartHome = new QuoteLineItem(Guid.NewGuid(), quoteRequestId, Guid.NewGuid(),
            PartnerCategory.SmartHome, 15_000m, "Smart home automation package", validUntil, now);

        var total = builder.AmountAud + landscaper.AmountAud + furniture.AmountAud + smartHome.AmountAud;

        Assert.That(total, Is.EqualTo(525_000m));
        Assert.That(builder.QuoteRequestId, Is.EqualTo(quoteRequestId));
        Assert.That(landscaper.QuoteRequestId, Is.EqualTo(quoteRequestId));
        Assert.That(furniture.QuoteRequestId, Is.EqualTo(quoteRequestId));
        Assert.That(smartHome.QuoteRequestId, Is.EqualTo(quoteRequestId));
    }
}
