using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Journey;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class ReferralServiceTests
{
    private ReferralService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new ReferralService(NullLogger<ReferralService>.Instance);

    // ── 1. Referral Creation ──

    [Test]
    public async Task CreateReferralAsync_ValidInput_ReturnsReferralInPendingStatus()
    {
        var referral = await _sut.CreateReferralAsync(Guid.NewGuid(), Guid.NewGuid(), PartnerCategory.Builder, "John Doe");

        Assert.That(referral.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(referral.Status, Is.EqualTo(ReferralStatus.Pending));
        Assert.That(referral.BuyerName, Is.EqualTo("John Doe"));
    }

    [Test]
    public async Task CreateReferralAsync_WithNotes_SetsNotes()
    {
        var referral = await _sut.CreateReferralAsync(Guid.NewGuid(), Guid.NewGuid(), PartnerCategory.Landscaper, "Jane", "Priority client");
        Assert.That(referral.Notes, Is.EqualTo("Priority client"));
    }

    [Test]
    public void CreateReferralAsync_EmptyJourneyId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateReferralAsync(Guid.Empty, Guid.NewGuid(), PartnerCategory.Builder, "John"));
    }

    [Test]
    public void CreateReferralAsync_EmptyPartnerId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateReferralAsync(Guid.NewGuid(), Guid.Empty, PartnerCategory.Builder, "John"));
    }

    [Test]
    public void CreateReferralAsync_EmptyBuyerName_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateReferralAsync(Guid.NewGuid(), Guid.NewGuid(), PartnerCategory.Builder, ""));
    }

    // ── 2. Status Management ──

    [Test]
    public async Task UpdateStatusAsync_AcceptReferral_SetsRespondedAt()
    {
        var referral = await _sut.CreateReferralAsync(Guid.NewGuid(), Guid.NewGuid(), PartnerCategory.Builder, "John");
        var updated = await _sut.UpdateStatusAsync(referral.Id, ReferralStatus.Accepted);

        Assert.That(updated.Status, Is.EqualTo(ReferralStatus.Accepted));
        Assert.That(updated.RespondedAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task UpdateStatusAsync_DeclineReferral_SetsRespondedAt()
    {
        var referral = await _sut.CreateReferralAsync(Guid.NewGuid(), Guid.NewGuid(), PartnerCategory.InteriorFurnishing, "Jane");
        var updated = await _sut.UpdateStatusAsync(referral.Id, ReferralStatus.Declined);

        Assert.That(updated.Status, Is.EqualTo(ReferralStatus.Declined));
        Assert.That(updated.RespondedAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task UpdateStatusAsync_AlreadyAccepted_ThrowsInvalidOperationException()
    {
        var referral = await _sut.CreateReferralAsync(Guid.NewGuid(), Guid.NewGuid(), PartnerCategory.Builder, "John");
        await _sut.UpdateStatusAsync(referral.Id, ReferralStatus.Accepted);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateStatusAsync(referral.Id, ReferralStatus.Declined));
    }

    // ── 3. Queries ──

    [Test]
    public async Task GetReferralsForJourneyAsync_MultipleReferrals_ReturnsAll()
    {
        var journeyId = Guid.NewGuid();
        await _sut.CreateReferralAsync(journeyId, Guid.NewGuid(), PartnerCategory.Builder, "John");
        await _sut.CreateReferralAsync(journeyId, Guid.NewGuid(), PartnerCategory.Landscaper, "John");

        var referrals = await _sut.GetReferralsForJourneyAsync(journeyId);
        Assert.That(referrals, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetReferralsForPartnerAsync_FiltersByPartner()
    {
        var partnerId = Guid.NewGuid();
        await _sut.CreateReferralAsync(Guid.NewGuid(), partnerId, PartnerCategory.Builder, "John");
        await _sut.CreateReferralAsync(Guid.NewGuid(), Guid.NewGuid(), PartnerCategory.Builder, "Jane");

        var referrals = await _sut.GetReferralsForPartnerAsync(partnerId);
        Assert.That(referrals, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetReferralAsync_ExistingReferral_ReturnsIt()
    {
        var referral = await _sut.CreateReferralAsync(Guid.NewGuid(), Guid.NewGuid(), PartnerCategory.Builder, "John");
        var retrieved = await _sut.GetReferralAsync(referral.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.BuyerName, Is.EqualTo("John"));
    }

    [Test]
    public void UpdateStatusAsync_NonExistentReferral_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateStatusAsync(Guid.NewGuid(), ReferralStatus.Accepted));
    }
}
