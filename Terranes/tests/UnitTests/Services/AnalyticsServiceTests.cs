using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Analytics;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class AnalyticsServiceTests
{
    private AnalyticsService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new AnalyticsService(NullLogger<AnalyticsService>.Instance);

    // ── 1. Event Tracking ──

    [Test]
    public async Task TrackAsync_ValidInput_ReturnsTrackedEvent()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var evt = await _sut.TrackAsync(userId, tenantId, AnalyticsEventType.VillageView);

        Assert.That(evt.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(evt.UserId, Is.EqualTo(userId));
        Assert.That(evt.TenantId, Is.EqualTo(tenantId));
        Assert.That(evt.EventType, Is.EqualTo(AnalyticsEventType.VillageView));
    }

    [Test]
    public async Task TrackAsync_WithEntity_SetsEntityId()
    {
        var entityId = Guid.NewGuid();
        var evt = await _sut.TrackAsync(Guid.NewGuid(), Guid.NewGuid(), AnalyticsEventType.HomeModelView, entityId);
        Assert.That(evt.EntityId, Is.EqualTo(entityId));
    }

    [Test]
    public void TrackAsync_EmptyUserId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.TrackAsync(Guid.Empty, Guid.NewGuid(), AnalyticsEventType.VillageView));
    }

    [Test]
    public void TrackAsync_EmptyTenantId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.TrackAsync(Guid.NewGuid(), Guid.Empty, AnalyticsEventType.VillageView));
    }

    // ── 2. User Queries ──

    [Test]
    public async Task GetUserEventsAsync_ReturnsUserEvents()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await _sut.TrackAsync(userId, tenantId, AnalyticsEventType.VillageView);
        await _sut.TrackAsync(userId, tenantId, AnalyticsEventType.HomeModelView);
        await _sut.TrackAsync(Guid.NewGuid(), tenantId, AnalyticsEventType.VillageView);

        var events = await _sut.GetUserEventsAsync(userId);
        Assert.That(events, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetTotalEventCountAsync_ReturnsTotal()
    {
        await _sut.TrackAsync(Guid.NewGuid(), Guid.NewGuid(), AnalyticsEventType.VillageView);
        await _sut.TrackAsync(Guid.NewGuid(), Guid.NewGuid(), AnalyticsEventType.HomeModelView);

        var count = await _sut.GetTotalEventCountAsync();
        Assert.That(count, Is.EqualTo(2));
    }

    // ── 3. Summaries & Popular Entities ──

    [Test]
    public async Task GetSummaryAsync_ReturnsCorrectCounts()
    {
        var tenantId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await _sut.TrackAsync(user1, tenantId, AnalyticsEventType.VillageView);
        await _sut.TrackAsync(user2, tenantId, AnalyticsEventType.VillageView);
        await _sut.TrackAsync(user1, tenantId, AnalyticsEventType.VillageView);

        var summary = await _sut.GetSummaryAsync(AnalyticsEventType.VillageView, now.AddMinutes(-1), now.AddMinutes(1));

        Assert.That(summary.Count, Is.EqualTo(3));
        Assert.That(summary.UniqueUsers, Is.EqualTo(2));
    }

    [Test]
    public async Task GetSummaryAsync_OutsidePeriod_ReturnsZero()
    {
        var now = DateTimeOffset.UtcNow;
        await _sut.TrackAsync(Guid.NewGuid(), Guid.NewGuid(), AnalyticsEventType.VillageView);

        var summary = await _sut.GetSummaryAsync(AnalyticsEventType.VillageView, now.AddHours(-2), now.AddHours(-1));

        Assert.That(summary.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetPopularEntitiesAsync_ReturnsTopEntities()
    {
        var tenantId = Guid.NewGuid();
        var entity1 = Guid.NewGuid();
        var entity2 = Guid.NewGuid();

        await _sut.TrackAsync(Guid.NewGuid(), tenantId, AnalyticsEventType.HomeModelView, entity1);
        await _sut.TrackAsync(Guid.NewGuid(), tenantId, AnalyticsEventType.HomeModelView, entity1);
        await _sut.TrackAsync(Guid.NewGuid(), tenantId, AnalyticsEventType.HomeModelView, entity1);
        await _sut.TrackAsync(Guid.NewGuid(), tenantId, AnalyticsEventType.HomeModelView, entity2);

        var popular = await _sut.GetPopularEntitiesAsync(AnalyticsEventType.HomeModelView, top: 2);

        Assert.That(popular, Has.Count.EqualTo(2));
        Assert.That(popular[0].EntityId, Is.EqualTo(entity1));
        Assert.That(popular[0].Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetPopularEntitiesAsync_NoEntityId_ExcludesFromResults()
    {
        var tenantId = Guid.NewGuid();
        await _sut.TrackAsync(Guid.NewGuid(), tenantId, AnalyticsEventType.Login);

        var popular = await _sut.GetPopularEntitiesAsync(AnalyticsEventType.Login);
        Assert.That(popular, Is.Empty);
    }

    [Test]
    public async Task TrackAsync_WithMetadata_SetsMetadata()
    {
        var evt = await _sut.TrackAsync(Guid.NewGuid(), Guid.NewGuid(), AnalyticsEventType.Search, metadata: "{\"query\":\"modern\"}");
        Assert.That(evt.Metadata, Is.EqualTo("{\"query\":\"modern\"}"));
    }
}
