using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Notifications;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class EventBusServiceTests
{
    private EventBusService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new EventBusService(NullLogger<EventBusService>.Instance);

    // ── 1. Publishing Events ──

    [Test]
    public async Task PublishAsync_ValidEvent_ReturnsWithId()
    {
        var evt = await _sut.PublishAsync("journey.started", "{\"journeyId\":\"abc\"}", Guid.NewGuid());

        Assert.That(evt.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(evt.Topic, Is.EqualTo("journey.started"));
    }

    [Test]
    public void PublishAsync_EmptyTopic_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.PublishAsync("", "{}", Guid.NewGuid()));
    }

    [Test]
    public void PublishAsync_EmptyPayload_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.PublishAsync("test.topic", "", Guid.NewGuid()));
    }

    [Test]
    public void PublishAsync_EmptyCorrelationId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.PublishAsync("test.topic", "{}", Guid.Empty));
    }

    // ── 2. Topic Queries ──

    [Test]
    public async Task GetEventsForTopicAsync_ReturnsMatchingEvents()
    {
        await _sut.PublishAsync("journey.started", "{}", Guid.NewGuid());
        await _sut.PublishAsync("journey.started", "{}", Guid.NewGuid());
        await _sut.PublishAsync("quote.completed", "{}", Guid.NewGuid());

        var events = await _sut.GetEventsForTopicAsync("journey.started");
        Assert.That(events, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetEventsForTopicAsync_UnknownTopic_ReturnsEmpty()
    {
        var events = await _sut.GetEventsForTopicAsync("no.such.topic");
        Assert.That(events, Is.Empty);
    }

    [Test]
    public async Task GetTopicSummaryAsync_ReturnsCorrectCounts()
    {
        await _sut.PublishAsync("a", "{}", Guid.NewGuid());
        await _sut.PublishAsync("a", "{}", Guid.NewGuid());
        await _sut.PublishAsync("b", "{}", Guid.NewGuid());

        var summary = await _sut.GetTopicSummaryAsync();
        Assert.That(summary["a"], Is.EqualTo(2));
        Assert.That(summary["b"], Is.EqualTo(1));
    }

    // ── 3. Correlation & Counts ──

    [Test]
    public async Task GetEventsForCorrelationAsync_ReturnsCorrelatedEvents()
    {
        var correlationId = Guid.NewGuid();
        await _sut.PublishAsync("a", "{}", correlationId);
        await _sut.PublishAsync("b", "{}", correlationId);
        await _sut.PublishAsync("c", "{}", Guid.NewGuid());

        var events = await _sut.GetEventsForCorrelationAsync(correlationId);
        Assert.That(events, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetTotalEventCountAsync_ReturnsCorrectCount()
    {
        await _sut.PublishAsync("a", "{}", Guid.NewGuid());
        await _sut.PublishAsync("b", "{}", Guid.NewGuid());

        var count = await _sut.GetTotalEventCountAsync();
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetEventsForTopicAsync_CaseInsensitiveTopicLookup()
    {
        await _sut.PublishAsync("Journey.Started", "{}", Guid.NewGuid());

        var events = await _sut.GetEventsForTopicAsync("journey.started");
        Assert.That(events, Has.Count.EqualTo(1));
    }
}
