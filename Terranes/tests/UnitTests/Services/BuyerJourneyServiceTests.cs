using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Journey;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class BuyerJourneyServiceTests
{
    private BuyerJourneyService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new BuyerJourneyService(NullLogger<BuyerJourneyService>.Instance);

    // ── 1. Journey Creation ──

    [Test]
    public async Task StartJourneyAsync_ValidBuyer_ReturnsJourneyInBrowsingStage()
    {
        var buyerId = Guid.NewGuid();
        var journey = await _sut.StartJourneyAsync(buyerId);

        Assert.That(journey.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(journey.BuyerId, Is.EqualTo(buyerId));
        Assert.That(journey.Stage, Is.EqualTo(JourneyStage.Browsing));
    }

    [Test]
    public async Task StartJourneyAsync_WithVillage_SetsVillageId()
    {
        var villageId = Guid.NewGuid();
        var journey = await _sut.StartJourneyAsync(Guid.NewGuid(), villageId);

        Assert.That(journey.VillageId, Is.EqualTo(villageId));
    }

    [Test]
    public void StartJourneyAsync_EmptyBuyerId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.StartJourneyAsync(Guid.Empty));
    }

    [Test]
    public async Task GetJourneyAsync_ExistingJourney_ReturnsIt()
    {
        var journey = await _sut.StartJourneyAsync(Guid.NewGuid());
        var retrieved = await _sut.GetJourneyAsync(journey.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Id, Is.EqualTo(journey.Id));
    }

    [Test]
    public async Task GetJourneyAsync_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetJourneyAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ── 2. Stage Advancement ──

    [Test]
    public async Task AdvanceStageAsync_ToDesignSelected_LinksHomeModel()
    {
        var journey = await _sut.StartJourneyAsync(Guid.NewGuid());
        var modelId = Guid.NewGuid();

        var advanced = await _sut.AdvanceStageAsync(journey.Id, JourneyStage.DesignSelected, modelId);

        Assert.That(advanced.Stage, Is.EqualTo(JourneyStage.DesignSelected));
        Assert.That(advanced.HomeModelId, Is.EqualTo(modelId));
    }

    [Test]
    public async Task AdvanceStageAsync_ToPlacedOnLand_LinksLandBlock()
    {
        var journey = await _sut.StartJourneyAsync(Guid.NewGuid());
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.DesignSelected, Guid.NewGuid());
        var landId = Guid.NewGuid();

        var advanced = await _sut.AdvanceStageAsync(journey.Id, JourneyStage.PlacedOnLand, landId);

        Assert.That(advanced.Stage, Is.EqualTo(JourneyStage.PlacedOnLand));
        Assert.That(advanced.LandBlockId, Is.EqualTo(landId));
    }

    [Test]
    public async Task AdvanceStageAsync_BackwardsStage_ThrowsInvalidOperationException()
    {
        var journey = await _sut.StartJourneyAsync(Guid.NewGuid());
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.DesignSelected);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AdvanceStageAsync(journey.Id, JourneyStage.Browsing));
    }

    [Test]
    public async Task AdvanceStageAsync_CompletedJourney_ThrowsInvalidOperationException()
    {
        var journey = await _sut.StartJourneyAsync(Guid.NewGuid());
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.DesignSelected);
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.PlacedOnLand);
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.Customising);
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.QuoteRequested);
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.QuoteReceived);
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.Referred);
        await _sut.AdvanceStageAsync(journey.Id, JourneyStage.Completed);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AdvanceStageAsync(journey.Id, JourneyStage.Referred));
    }

    // ── 3. Lifecycle ──

    [Test]
    public async Task AbandonJourneyAsync_ActiveJourney_SetsAbandoned()
    {
        var journey = await _sut.StartJourneyAsync(Guid.NewGuid());
        var abandoned = await _sut.AbandonJourneyAsync(journey.Id);

        Assert.That(abandoned.Stage, Is.EqualTo(JourneyStage.Abandoned));
    }

    [Test]
    public async Task GetBuyerJourneysAsync_MultipleBuyers_FiltersByBuyer()
    {
        var buyer1 = Guid.NewGuid();
        var buyer2 = Guid.NewGuid();
        await _sut.StartJourneyAsync(buyer1);
        await _sut.StartJourneyAsync(buyer1);
        await _sut.StartJourneyAsync(buyer2);

        var journeys = await _sut.GetBuyerJourneysAsync(buyer1);
        Assert.That(journeys, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetActiveJourneysAsync_ExcludesCompletedAndAbandoned()
    {
        var j1 = await _sut.StartJourneyAsync(Guid.NewGuid());
        var j2 = await _sut.StartJourneyAsync(Guid.NewGuid());
        await _sut.StartJourneyAsync(Guid.NewGuid());

        await _sut.AbandonJourneyAsync(j1.Id);
        await _sut.AdvanceStageAsync(j2.Id, JourneyStage.DesignSelected);
        await _sut.AdvanceStageAsync(j2.Id, JourneyStage.PlacedOnLand);
        await _sut.AdvanceStageAsync(j2.Id, JourneyStage.Customising);
        await _sut.AdvanceStageAsync(j2.Id, JourneyStage.QuoteRequested);
        await _sut.AdvanceStageAsync(j2.Id, JourneyStage.QuoteReceived);
        await _sut.AdvanceStageAsync(j2.Id, JourneyStage.Referred);
        await _sut.AdvanceStageAsync(j2.Id, JourneyStage.Completed);

        var active = await _sut.GetActiveJourneysAsync();
        Assert.That(active, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AdvanceStageAsync_NonExistentJourney_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AdvanceStageAsync(Guid.NewGuid(), JourneyStage.DesignSelected));
    }
}
