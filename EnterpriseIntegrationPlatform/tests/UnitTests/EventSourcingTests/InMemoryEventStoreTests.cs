using EnterpriseIntegrationPlatform.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.EventSourcingTests;

[TestFixture]
public class InMemoryEventStoreTests
{
    private InMemoryEventStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        var options = Options.Create(new EventSourcingOptions());
        _store = new InMemoryEventStore(options, NullLogger<InMemoryEventStore>.Instance);
    }

    private static EventEnvelope MakeEvent(string type = "TestEvent", string data = "{}")
    {
        return new EventEnvelope(
            EventId: Guid.NewGuid(),
            StreamId: "will-be-set",
            EventType: type,
            Data: data,
            Version: 0,
            Timestamp: DateTimeOffset.UtcNow,
            Metadata: new Dictionary<string, string>());
    }

    // --- Append ---

    [Test]
    public async Task AppendAsync_NewStream_ReturnsVersionEqualToEventCount()
    {
        var events = new[] { MakeEvent(), MakeEvent() };
        var version = await _store.AppendAsync("stream-1", events, 0);
        Assert.That(version, Is.EqualTo(2));
    }

    [Test]
    public async Task AppendAsync_SecondBatch_ContinuesVersionSequence()
    {
        await _store.AppendAsync("stream-1", [MakeEvent()], 0);
        var version = await _store.AppendAsync("stream-1", [MakeEvent(), MakeEvent()], 1);
        Assert.That(version, Is.EqualTo(3));
    }

    [Test]
    public void AppendAsync_WrongExpectedVersion_ThrowsOptimisticConcurrencyException()
    {
        Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
            await _store.AppendAsync("stream-1", [MakeEvent()], 5));
    }

    [Test]
    public async Task AppendAsync_ConcurrencyConflict_ContainsStreamIdAndVersions()
    {
        await _store.AppendAsync("stream-1", [MakeEvent()], 0);

        var ex = Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
            await _store.AppendAsync("stream-1", [MakeEvent()], 0));

        Assert.That(ex!.StreamId, Is.EqualTo("stream-1"));
        Assert.That(ex.ExpectedVersion, Is.EqualTo(0));
        Assert.That(ex.ActualVersion, Is.EqualTo(1));
    }

    [Test]
    public void AppendAsync_EmptyEventList_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _store.AppendAsync("stream-1", Array.Empty<EventEnvelope>(), 0));
    }

    [Test]
    public void AppendAsync_NullStreamId_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _store.AppendAsync(null!, [MakeEvent()], 0));
    }

    // --- Read Forward ---

    [Test]
    public async Task ReadStreamAsync_EmptyStream_ReturnsEmptyList()
    {
        var events = await _store.ReadStreamAsync("nonexistent", 1, 100);
        Assert.That(events, Is.Empty);
    }

    [Test]
    public async Task ReadStreamAsync_FromBeginning_ReturnsAllEventsInOrder()
    {
        await _store.AppendAsync("s1", [MakeEvent("A"), MakeEvent("B"), MakeEvent("C")], 0);

        var events = await _store.ReadStreamAsync("s1", 1, 100);

        Assert.That(events, Has.Count.EqualTo(3));
        Assert.That(events[0].Version, Is.EqualTo(1));
        Assert.That(events[1].Version, Is.EqualTo(2));
        Assert.That(events[2].Version, Is.EqualTo(3));
    }

    [Test]
    public async Task ReadStreamAsync_FromMiddle_ReturnsSubset()
    {
        await _store.AppendAsync("s1", [MakeEvent("A"), MakeEvent("B"), MakeEvent("C")], 0);

        var events = await _store.ReadStreamAsync("s1", 2, 100);

        Assert.That(events, Has.Count.EqualTo(2));
        Assert.That(events[0].EventType, Is.EqualTo("B"));
        Assert.That(events[1].EventType, Is.EqualTo("C"));
    }

    [Test]
    public async Task ReadStreamAsync_WithCountLimit_ReturnsRequestedCount()
    {
        await _store.AppendAsync("s1", [MakeEvent("A"), MakeEvent("B"), MakeEvent("C")], 0);

        var events = await _store.ReadStreamAsync("s1", 1, 2);

        Assert.That(events, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ReadStreamAsync_SetsCorrectStreamId()
    {
        await _store.AppendAsync("my-stream", [MakeEvent()], 0);

        var events = await _store.ReadStreamAsync("my-stream", 1, 10);

        Assert.That(events[0].StreamId, Is.EqualTo("my-stream"));
    }

    // --- Read Backward ---

    [Test]
    public async Task ReadStreamBackwardAsync_EmptyStream_ReturnsEmptyList()
    {
        var events = await _store.ReadStreamBackwardAsync("nonexistent", 100, 100);
        Assert.That(events, Is.Empty);
    }

    [Test]
    public async Task ReadStreamBackwardAsync_FromEnd_ReturnsEventsInDescendingOrder()
    {
        await _store.AppendAsync("s1", [MakeEvent("A"), MakeEvent("B"), MakeEvent("C")], 0);

        var events = await _store.ReadStreamBackwardAsync("s1", 3, 100);

        Assert.That(events, Has.Count.EqualTo(3));
        Assert.That(events[0].Version, Is.EqualTo(3));
        Assert.That(events[1].Version, Is.EqualTo(2));
        Assert.That(events[2].Version, Is.EqualTo(1));
    }

    [Test]
    public async Task ReadStreamBackwardAsync_FromMiddle_ReturnsSubset()
    {
        await _store.AppendAsync("s1", [MakeEvent("A"), MakeEvent("B"), MakeEvent("C")], 0);

        var events = await _store.ReadStreamBackwardAsync("s1", 2, 100);

        Assert.That(events, Has.Count.EqualTo(2));
        Assert.That(events[0].EventType, Is.EqualTo("B"));
        Assert.That(events[1].EventType, Is.EqualTo("A"));
    }

    [Test]
    public async Task ReadStreamBackwardAsync_WithCountLimit_ReturnsRequestedCount()
    {
        await _store.AppendAsync("s1", [MakeEvent("A"), MakeEvent("B"), MakeEvent("C")], 0);

        var events = await _store.ReadStreamBackwardAsync("s1", 3, 2);

        Assert.That(events, Has.Count.EqualTo(2));
        Assert.That(events[0].Version, Is.EqualTo(3));
        Assert.That(events[1].Version, Is.EqualTo(2));
    }

    // --- MaxEventsPerRead ---

    [Test]
    public async Task ReadStreamAsync_ExceedsMaxEventsPerRead_ClampedToMax()
    {
        var options = Options.Create(new EventSourcingOptions { MaxEventsPerRead = 2 });
        var store = new InMemoryEventStore(options, NullLogger<InMemoryEventStore>.Instance);

        await store.AppendAsync("s1", [MakeEvent(), MakeEvent(), MakeEvent()], 0);

        var events = await store.ReadStreamAsync("s1", 1, 999);

        Assert.That(events, Has.Count.EqualTo(2));
    }
}
