// ============================================================================
// Tutorial 31 – Event Sourcing (Lab)
// ============================================================================
// This lab exercises the InMemoryEventStore, InMemorySnapshotStore,
// EventProjectionEngine, EventEnvelope, OptimisticConcurrencyException,
// and EventSourcingOptions to learn the event sourcing subsystem.
// ============================================================================

using EnterpriseIntegrationPlatform.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial31;

[TestFixture]
public sealed class Lab
{
    private InMemoryEventStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        var options = Options.Create(new EventSourcingOptions());
        _store = new InMemoryEventStore(options, NullLogger<InMemoryEventStore>.Instance);
    }

    // ── Append and Read Roundtrip ───────────────────────────────────────────

    [Test]
    public async Task AppendAsync_AndReadStreamAsync_Roundtrip()
    {
        var envelope = new EventEnvelope(
            Guid.NewGuid(), "stream-1", "OrderCreated",
            """{"total":42}""", 0, DateTimeOffset.UtcNow, []);

        await _store.AppendAsync("stream-1", [envelope], expectedVersion: 0);

        var events = await _store.ReadStreamAsync("stream-1", fromVersion: 1, count: 100);

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].StreamId, Is.EqualTo("stream-1"));
        Assert.That(events[0].EventType, Is.EqualTo("OrderCreated"));
        Assert.That(events[0].Version, Is.EqualTo(1));
    }

    // ── Append Multiple and Read All Back in Order ──────────────────────────

    [Test]
    public async Task AppendMultiple_ReadAllBack_InOrder()
    {
        var e1 = new EventEnvelope(Guid.NewGuid(), "s", "A", "d1", 0, DateTimeOffset.UtcNow, []);
        var e2 = new EventEnvelope(Guid.NewGuid(), "s", "B", "d2", 0, DateTimeOffset.UtcNow, []);
        var e3 = new EventEnvelope(Guid.NewGuid(), "s", "C", "d3", 0, DateTimeOffset.UtcNow, []);

        await _store.AppendAsync("s", [e1], expectedVersion: 0);
        await _store.AppendAsync("s", [e2], expectedVersion: 1);
        await _store.AppendAsync("s", [e3], expectedVersion: 2);

        var events = await _store.ReadStreamAsync("s", fromVersion: 1, count: 100);

        Assert.That(events, Has.Count.EqualTo(3));
        Assert.That(events[0].Version, Is.EqualTo(1));
        Assert.That(events[1].Version, Is.EqualTo(2));
        Assert.That(events[2].Version, Is.EqualTo(3));
        Assert.That(events[0].EventType, Is.EqualTo("A"));
        Assert.That(events[2].EventType, Is.EqualTo("C"));
    }

    // ── OptimisticConcurrencyException on Version Conflict ──────────────────

    [Test]
    public async Task AppendAsync_VersionConflict_ThrowsOptimisticConcurrencyException()
    {
        var e = new EventEnvelope(Guid.NewGuid(), "s", "E", "d", 0, DateTimeOffset.UtcNow, []);
        await _store.AppendAsync("s", [e], expectedVersion: 0);

        var e2 = new EventEnvelope(Guid.NewGuid(), "s", "E2", "d2", 0, DateTimeOffset.UtcNow, []);

        var ex = Assert.ThrowsAsync<OptimisticConcurrencyException>(
            () => _store.AppendAsync("s", [e2], expectedVersion: 0));

        Assert.That(ex!.StreamId, Is.EqualTo("s"));
        Assert.That(ex.ExpectedVersion, Is.EqualTo(0));
        Assert.That(ex.ActualVersion, Is.EqualTo(1));
    }

    // ── ReadStreamBackwardAsync Returns Reversed Order ──────────────────────

    [Test]
    public async Task ReadStreamBackwardAsync_ReturnsReversedOrder()
    {
        var e1 = new EventEnvelope(Guid.NewGuid(), "s", "A", "d1", 0, DateTimeOffset.UtcNow, []);
        var e2 = new EventEnvelope(Guid.NewGuid(), "s", "B", "d2", 0, DateTimeOffset.UtcNow, []);
        var e3 = new EventEnvelope(Guid.NewGuid(), "s", "C", "d3", 0, DateTimeOffset.UtcNow, []);

        await _store.AppendAsync("s", [e1, e2, e3], expectedVersion: 0);

        var events = await _store.ReadStreamBackwardAsync("s", fromVersion: 3, count: 100);

        Assert.That(events, Has.Count.EqualTo(3));
        Assert.That(events[0].Version, Is.EqualTo(3));
        Assert.That(events[1].Version, Is.EqualTo(2));
        Assert.That(events[2].Version, Is.EqualTo(1));
    }

    // ── InMemorySnapshotStore Save and Load Roundtrip ───────────────────────

    [Test]
    public async Task SnapshotStore_SaveAndLoad_Roundtrip()
    {
        var snapshots = new InMemorySnapshotStore<int>();

        await snapshots.SaveAsync("stream-1", 42, 5);
        var (state, version) = await snapshots.LoadAsync("stream-1");

        Assert.That(state, Is.EqualTo(42));
        Assert.That(version, Is.EqualTo(5));
    }

    // ── EventSourcingOptions Defaults ────────────────────────────────────────

    [Test]
    public void EventSourcingOptions_Defaults()
    {
        var opts = new EventSourcingOptions();

        Assert.That(opts.SnapshotInterval, Is.EqualTo(50));
        Assert.That(opts.MaxEventsPerRead, Is.EqualTo(1000));
    }

    // ── EventEnvelope Record Shape ──────────────────────────────────────────

    [Test]
    public void EventEnvelope_RecordShape_AllPropertiesAccessible()
    {
        var id = Guid.NewGuid();
        var ts = DateTimeOffset.UtcNow;
        var meta = new Dictionary<string, string> { ["key"] = "value" };

        var envelope = new EventEnvelope(id, "stream-1", "OrderCreated", """{"x":1}""", 7, ts, meta);

        Assert.That(envelope.EventId, Is.EqualTo(id));
        Assert.That(envelope.StreamId, Is.EqualTo("stream-1"));
        Assert.That(envelope.EventType, Is.EqualTo("OrderCreated"));
        Assert.That(envelope.Data, Is.EqualTo("""{"x":1}"""));
        Assert.That(envelope.Version, Is.EqualTo(7));
        Assert.That(envelope.Timestamp, Is.EqualTo(ts));
        Assert.That(envelope.Metadata["key"], Is.EqualTo("value"));
    }
}
