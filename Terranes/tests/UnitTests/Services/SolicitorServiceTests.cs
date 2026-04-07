using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.PartnerIntegration;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class SolicitorServiceTests
{
    private SolicitorService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new SolicitorService(NullLogger<SolicitorService>.Instance);

    // ── 1. Registration ──

    [Test]
    public async Task RegisterAsync_ValidSolicitor_ReturnsProfile()
    {
        var (partner, profile) = MakeSolicitor();
        var registered = await _sut.RegisterAsync(partner, profile);

        Assert.That(registered.PartnerId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(registered.OffersConveyancing, Is.True);
    }

    [Test]
    public void RegisterAsync_EmptyBusinessName_ThrowsArgumentException()
    {
        var (partner, profile) = MakeSolicitor();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner with { BusinessName = "" }, profile));
    }

    [Test]
    public void RegisterAsync_WrongCategory_ThrowsArgumentException()
    {
        var (partner, profile) = MakeSolicitor();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner with { Category = PartnerCategory.Builder }, profile));
    }

    [Test]
    public void RegisterAsync_NegativeFee_ThrowsArgumentException()
    {
        var (partner, profile) = MakeSolicitor();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { FixedFeeAud = -1m }));
    }

    [Test]
    public void RegisterAsync_NegativeExperience_ThrowsArgumentException()
    {
        var (partner, profile) = MakeSolicitor();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { YearsExperience = -1 }));
    }

    [Test]
    public void RegisterAsync_NoServices_ThrowsArgumentException()
    {
        var (partner, profile) = MakeSolicitor();
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(partner, profile with { OffersConveyancing = false, OffersContractReview = false }));
    }

    // ── 2. Search ──

    [Test]
    public async Task FindSolicitorsAsync_ByConveyancing_FiltersCorrectly()
    {
        var (partner, profile) = MakeSolicitor();
        await _sut.RegisterAsync(partner, profile);

        var results = await _sut.FindSolicitorsAsync(conveyancing: true);
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FindSolicitorsAsync_ByContractReview_FiltersCorrectly()
    {
        var (partner, profile) = MakeSolicitor();
        await _sut.RegisterAsync(partner, profile with { OffersContractReview = true, OffersConveyancing = false });

        var results = await _sut.FindSolicitorsAsync(contractReview: true);
        Assert.That(results, Has.Count.EqualTo(1));

        var convResults = await _sut.FindSolicitorsAsync(conveyancing: true);
        Assert.That(convResults, Is.Empty);
    }

    [Test]
    public async Task GetProfileAsync_ExistingPartner_ReturnsProfile()
    {
        var (partner, profile) = MakeSolicitor();
        var registered = await _sut.RegisterAsync(partner, profile);
        var retrieved = await _sut.GetProfileAsync(registered.PartnerId);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.FixedFeeAud, Is.EqualTo(2500m));
    }

    // ── 3. Quote Request ──

    [Test]
    public async Task RequestQuoteAsync_ValidSolicitor_ReturnsQuotedWithFixedFee()
    {
        var (partner, profile) = MakeSolicitor();
        var registered = await _sut.RegisterAsync(partner, profile);

        var response = await _sut.RequestQuoteAsync(registered.PartnerId, Guid.NewGuid(), "Conveyancing");

        Assert.That(response.Status, Is.EqualTo(PartnerQuoteStatus.Quoted));
        Assert.That(response.AmountAud, Is.EqualTo(2500m));
        Assert.That(response.Category, Is.EqualTo(PartnerCategory.Solicitor));
    }

    [Test]
    public void RequestQuoteAsync_NonExistentPartner_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RequestQuoteAsync(Guid.NewGuid(), Guid.NewGuid(), "Conveyancing"));
    }

    [Test]
    public async Task RequestQuoteAsync_EmptyServiceType_ThrowsArgumentException()
    {
        var (partner, profile) = MakeSolicitor();
        var registered = await _sut.RegisterAsync(partner, profile);

        Assert.ThrowsAsync<ArgumentException>(() => _sut.RequestQuoteAsync(registered.PartnerId, Guid.NewGuid(), ""));
    }

    private static (Partner Partner, SolicitorProfile Profile) MakeSolicitor() => (
        new Partner(Guid.Empty, "Jones & Partners", PartnerCategory.Solicitor, "legal@jones.com.au", "+61400555666", ["NSW", "ACT"], true, default),
        new SolicitorProfile(Guid.Empty, ["Property", "Conveyancing"], 2500m, 450m, true, false, 15));
}
