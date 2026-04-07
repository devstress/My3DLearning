using Terranes.Contracts.Enums;

namespace Terranes.UnitTests.Enums;

[TestFixture]
public sealed class EnumTests
{
    [Test]
    public void ModelFormat_HasExpectedValues()
    {
        var values = Enum.GetValues<ModelFormat>();

        Assert.That(values, Has.Length.EqualTo(5));
        Assert.That(values, Does.Contain(ModelFormat.Gltf));
        Assert.That(values, Does.Contain(ModelFormat.Glb));
        Assert.That(values, Does.Contain(ModelFormat.Obj));
        Assert.That(values, Does.Contain(ModelFormat.Fbx));
        Assert.That(values, Does.Contain(ModelFormat.Usd));
    }

    [Test]
    public void PartnerCategory_HasAllPartnerTypes()
    {
        var values = Enum.GetValues<PartnerCategory>();

        Assert.That(values, Has.Length.EqualTo(7));
        Assert.That(values, Does.Contain(PartnerCategory.Builder));
        Assert.That(values, Does.Contain(PartnerCategory.Landscaper));
        Assert.That(values, Does.Contain(PartnerCategory.InteriorFurnishing));
        Assert.That(values, Does.Contain(PartnerCategory.SmartHome));
        Assert.That(values, Does.Contain(PartnerCategory.Solicitor));
        Assert.That(values, Does.Contain(PartnerCategory.RealEstateAgent));
        Assert.That(values, Does.Contain(PartnerCategory.LandProvider));
    }

    [Test]
    public void QuoteStatus_HasFullLifecycle()
    {
        var values = Enum.GetValues<QuoteStatus>();

        Assert.That(values, Has.Length.EqualTo(6));
        Assert.That(values, Does.Contain(QuoteStatus.Pending));
        Assert.That(values, Does.Contain(QuoteStatus.InProgress));
        Assert.That(values, Does.Contain(QuoteStatus.Completed));
        Assert.That(values, Does.Contain(QuoteStatus.PartiallyCompleted));
        Assert.That(values, Does.Contain(QuoteStatus.Expired));
        Assert.That(values, Does.Contain(QuoteStatus.Cancelled));
    }

    [Test]
    public void ZoningType_HasResidentialAndCommercialTypes()
    {
        var values = Enum.GetValues<ZoningType>();

        Assert.That(values, Has.Length.EqualTo(7));
        Assert.That(values, Does.Contain(ZoningType.Residential));
        Assert.That(values, Does.Contain(ZoningType.Commercial));
        Assert.That(values, Does.Contain(ZoningType.MixedUse));
    }

    [Test]
    public void ListingStatus_HasFullLifecycle()
    {
        var values = Enum.GetValues<ListingStatus>();

        Assert.That(values, Has.Length.EqualTo(5));
        Assert.That(values, Does.Contain(ListingStatus.Draft));
        Assert.That(values, Does.Contain(ListingStatus.Active));
        Assert.That(values, Does.Contain(ListingStatus.UnderOffer));
        Assert.That(values, Does.Contain(ListingStatus.Sold));
        Assert.That(values, Does.Contain(ListingStatus.Withdrawn));
    }
}
