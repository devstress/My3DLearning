using EnterpriseIntegrationPlatform.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.EventSourcingTests;

[TestFixture]
public class EventProjectionEngineTests
{
    private InMemoryEventStore _eventStore = null!;
    private InMemorySnapshotStore<int> _snapshotStore = null!;
    private CounterProjection _projection = null!;

    [SetUp]
    public void SetUp()
    {
        var options = Options.Create(new EventSourcingOptions { SnapshotInterval = 5, MaxEventsPerRead = 1000 });
        _eventStore = new InMemoryEventStore(options, NullLogger<InMemoryEventStore>.Instance);
        _snapshotStore = new InMemorySnapshotStore<int>();
        _projection = new CounterProjection();
    }

    private EventProjectionEngine<int> BuildEngine(int snapshotInterval = 5, int maxPerRead = 1000)
    {
        var options = Options.Create(new EventSourcingOptions
        {
            SnapshotInterval = snapshotInterval,
            MaxEventsPerRead = maxPerRead
        });
        return new EventProjectionEngine<int>(
            _eventStore,
            _snapshotStore,
            _projection,
            options,
            NullLogger<EventProjectionEngine<int>>.Instance);
    }

    private static EventEnvelope MakeEvent(string type = "Increment", string data = "{}", DateTimeOffset? timestamp = null)
    {
        return new EventEnvelope(
            EventId: Guid.NewGuid(),
            StreamId: "will-be-set",
            EventType: type,
            Data: data,
            Version: 0,
            Timestamp: timestamp ?? DateTimeOffset.UtcNow,
            Metadata: new Dictionary<string, string>());
    }

    // --- Rebuild from empty ---

    [Test]
    public async Task RebuildAsync_EmptyStream_ReturnsInitialState()
    {
        var engine = BuildEngine();

        var (state, version) = await engine.RebuildAsync("empty", 0);

        Assert.That(state, Is.EqualTo(0));
        Assert.That(version, Is.EqualTo(0));
    }

    [Test]
    public async Task RebuildAsync_WithEvents_ProjectsAllEvents()
    {
        await _eventStore.AppendAsync("s1", [MakeEvent(), MakeEvent(), MakeEvent()], 0);
        var engine = BuildEngine();

        var (state, version) = await engine.RebuildAsync("s1", 0);

        Assert.That(state, Is.EqualTo(3));
        Assert.That(version, Is.EqualTo(3));
    }

    [Test]
    public async Task RebuildAsync_AppliesEventsInOrder()
    {
        var ordered = new OrderTrackingProjection();
        var options = Options.Create(new EventSourcingOptions { SnapshotInterval = 100, MaxEventsPerRead = 1000 });
        var engine = new EventProjectionEngine<List<string>>(
            _eventStore,
            new InMemorySnapshotStore<List<string>>(),
            ordered,
            options,
            NullLogger<EventProjectionEngine<List<string>>>.Instance);

        await _eventStore.AppendAsync("s1", [MakeEvent("A"), MakeEvent("B"), MakeEvent("C")], 0);

        var (state, _) = await engine.RebuildAsync("s1", new List<string>());

        Assert.That(state, Is.EqualTo(new List<string> { "A", "B", "C" }));
    }

    // --- Rebuild with snapshot ---

    [Test]
    public async Task RebuildAsync_WithSnapshot_StartsFromSnapshotState()
    {
        await _snapshotStore.SaveAsync("s1", 10, 3);
        await _eventStore.AppendAsync("s1", [MakeEvent(), MakeEvent(), MakeEvent()], 0);
        // Events at versions 1,2,3 already snapshotted as state=10, add more:
        await _eventStore.AppendAsync("s1", [MakeEvent(), MakeEvent()], 3);

        var engine = BuildEngine(snapshotInterval: 100);

        var (state, version) = await engine.RebuildAsync("s1", 0);

        // Snapshot = 10 at version 3, then 2 more events => 12
        Assert.That(state, Is.EqualTo(12));
        Assert.That(version, Is.EqualTo(5));
    }

    [Test]
    public async Task RebuildAsync_SnapshotIntervalMet_SavesNewSnapshot()
    {
        var events = Enumerable.Range(0, 6).Select(_ => MakeEvent()).ToArray();
        await _eventStore.AppendAsync("s1", events, 0);

        var engine = BuildEngine(snapshotInterval: 5);
        await engine.RebuildAsync("s1", 0);

        var (snapshot, snapshotVersion) = await _snapshotStore.LoadAsync("s1");
        Assert.That(snapshot, Is.EqualTo(6));
        Assert.That(snapshotVersion, Is.EqualTo(6));
    }

    [Test]
    public async Task RebuildAsync_BelowSnapshotInterval_DoesNotSaveSnapshot()
    {
        await _eventStore.AppendAsync("s1", [MakeEvent(), MakeEvent()], 0);

        var engine = BuildEngine(snapshotInterval: 5);
        await engine.RebuildAsync("s1", 0);

        var (snapshot, snapshotVersion) = await _snapshotStore.LoadAsync("s1");
        Assert.That(snapshotVersion, Is.EqualTo(0));
    }

    // --- Paged reads (MaxEventsPerRead) ---

    [Test]
    public async Task RebuildAsync_LargeStreamWithSmallPageSize_RebuildsCorrectly()
    {
        var events = Enumerable.Range(0, 10).Select(_ => MakeEvent()).ToArray();
        await _eventStore.AppendAsync("s1", events, 0);

        // Use maxPerRead=3 to force multiple read batches.
        // We need the event store also configured with same max.
        var storeOptions = Options.Create(new EventSourcingOptions { MaxEventsPerRead = 3 });
        var pagedStore = new InMemoryEventStore(storeOptions, NullLogger<InMemoryEventStore>.Instance);
        var pagedEvents = Enumerable.Range(0, 10).Select(_ => MakeEvent()).ToArray();
        await pagedStore.AppendAsync("s1", pagedEvents, 0);

        var engineOptions = Options.Create(new EventSourcingOptions { SnapshotInterval = 100, MaxEventsPerRead = 3 });
        var engine = new EventProjectionEngine<int>(
            pagedStore,
            new InMemorySnapshotStore<int>(),
            _projection,
            engineOptions,
            NullLogger<EventProjectionEngine<int>>.Instance);

        var (state, version) = await engine.RebuildAsync("s1", 0);

        Assert.That(state, Is.EqualTo(10));
        Assert.That(version, Is.EqualTo(10));
    }

    // --- Temporal query ---

    [Test]
    public async Task ReplayToPointInTimeAsync_StopsAtCutoff()
    {
        var t1 = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        await _eventStore.AppendAsync("s1", [
            MakeEvent(timestamp: t1),
            MakeEvent(timestamp: t2),
            MakeEvent(timestamp: t3)
        ], 0);

        var cutoff = new DateTimeOffset(2024, 1, 1, 11, 30, 0, TimeSpan.Zero);
        var (state, version) = await TemporalQuery.ReplayToPointInTimeAsync(
            _eventStore, _projection, "s1", cutoff, 0);

        Assert.That(state, Is.EqualTo(2));
        Assert.That(version, Is.EqualTo(2));
    }

    [Test]
    public async Task ReplayToPointInTimeAsync_AllEventsBeforeCutoff_ReturnsAll()
    {
        var t1 = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero);

        await _eventStore.AppendAsync("s1", [MakeEvent(timestamp: t1), MakeEvent(timestamp: t2)], 0);

        var cutoff = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (state, version) = await TemporalQuery.ReplayToPointInTimeAsync(
            _eventStore, _projection, "s1", cutoff, 0);

        Assert.That(state, Is.EqualTo(2));
        Assert.That(version, Is.EqualTo(2));
    }

    [Test]
    public async Task ReplayToPointInTimeAsync_NoEventsBeforeCutoff_ReturnsInitial()
    {
        var t1 = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        await _eventStore.AppendAsync("s1", [MakeEvent(timestamp: t1)], 0);

        var cutoff = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (state, version) = await TemporalQuery.ReplayToPointInTimeAsync(
            _eventStore, _projection, "s1", cutoff, 0);

        Assert.That(state, Is.EqualTo(0));
        Assert.That(version, Is.EqualTo(0));
    }

    [Test]
    public async Task ReplayToPointInTimeAsync_EmptyStream_ReturnsInitialState()
    {
        var cutoff = DateTimeOffset.UtcNow;
        var (state, version) = await TemporalQuery.ReplayToPointInTimeAsync(
            _eventStore, _projection, "empty", cutoff, 42);

        Assert.That(state, Is.EqualTo(42));
        Assert.That(version, Is.EqualTo(0));
    }

    // --- Helper projections ---

    private sealed class CounterProjection : IEventProjection<int>
    {
        public Task<int> ProjectAsync(int state, EventEnvelope envelope, CancellationToken cancellationToken = default)
            => Task.FromResult(state + 1);
    }

    private sealed class OrderTrackingProjection : IEventProjection<List<string>>
    {
        public Task<List<string>> ProjectAsync(List<string> state, EventEnvelope envelope, CancellationToken cancellationToken = default)
        {
            var next = new List<string>(state) { envelope.EventType };
            return Task.FromResult(next);
        }
    }
}
