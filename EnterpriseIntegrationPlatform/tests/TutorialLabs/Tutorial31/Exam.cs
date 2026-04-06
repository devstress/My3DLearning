// ============================================================================
// Tutorial 31 – Event Sourcing (Exam)
// ============================================================================
// Coding challenges: projection that sums order totals, snapshot + rebuild
// state restore, and concurrent append detection via optimistic concurrency.
// ============================================================================

using EnterpriseIntegrationPlatform.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial31;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Projection That Sums Order Totals ──────────────────────

    [Test]
    public async Task Challenge1_Projection_SumsOrderTotals()
    {
        var options = Options.Create(new EventSourcingOptions());
        var store = new InMemoryEventStore(options, NullLogger<InMemoryEventStore>.Instance);
        var snapshots = new InMemorySnapshotStore<decimal>();

        var projection = Substitute.For<IEventProjection<decimal>>();
        projection
            .ProjectAsync(Arg.Any<decimal>(), Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var currentState = callInfo.ArgAt<decimal>(0);
                var envelope = callInfo.ArgAt<EventEnvelope>(1);
                // Parse the total from the event Data field
                if (decimal.TryParse(envelope.Data, out var amount))
                    return Task.FromResult(currentState + amount);
                return Task.FromResult(currentState);
            });

        var engine = new EventProjectionEngine<decimal>(
            store, snapshots, projection, options,
            NullLogger<EventProjectionEngine<decimal>>.Instance);

        var e1 = new EventEnvelope(Guid.NewGuid(), "orders", "OrderPlaced", "100.50", 0, DateTimeOffset.UtcNow, []);
        var e2 = new EventEnvelope(Guid.NewGuid(), "orders", "OrderPlaced", "200.25", 0, DateTimeOffset.UtcNow, []);
        var e3 = new EventEnvelope(Guid.NewGuid(), "orders", "OrderPlaced", "50.00", 0, DateTimeOffset.UtcNow, []);

        await store.AppendAsync("orders", [e1, e2, e3], expectedVersion: 0);

        var (state, version) = await engine.RebuildAsync("orders", 0m);

        Assert.That(state, Is.EqualTo(350.75m));
        Assert.That(version, Is.EqualTo(3));
    }

    // ── Challenge 2: Snapshot + Rebuild Restores State ───────────────────────

    [Test]
    public async Task Challenge2_SnapshotAndRebuild_RestoresState()
    {
        var options = Options.Create(new EventSourcingOptions { SnapshotInterval = 2 });
        var store = new InMemoryEventStore(options, NullLogger<InMemoryEventStore>.Instance);
        var snapshots = new InMemorySnapshotStore<int>();

        var projection = Substitute.For<IEventProjection<int>>();
        projection
            .ProjectAsync(Arg.Any<int>(), Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<int>(0) + 1));

        var engine = new EventProjectionEngine<int>(
            store, snapshots, projection, options,
            NullLogger<EventProjectionEngine<int>>.Instance);

        // Append 3 events (>= SnapshotInterval of 2, so snapshot should be saved)
        var events = Enumerable.Range(0, 3)
            .Select(_ => new EventEnvelope(Guid.NewGuid(), "s", "Evt", "d", 0, DateTimeOffset.UtcNow, []))
            .ToList();
        await store.AppendAsync("s", events, expectedVersion: 0);

        // First rebuild: processes all events, saves snapshot at version 3
        var (state1, ver1) = await engine.RebuildAsync("s", 0);
        Assert.That(state1, Is.EqualTo(3));
        Assert.That(ver1, Is.EqualTo(3));

        // Verify snapshot was saved
        var (snapState, snapVer) = await snapshots.LoadAsync("s");
        Assert.That(snapState, Is.EqualTo(3));
        Assert.That(snapVer, Is.EqualTo(3));

        // Add one more event
        var e4 = new EventEnvelope(Guid.NewGuid(), "s", "Evt", "d", 0, DateTimeOffset.UtcNow, []);
        await store.AppendAsync("s", [e4], expectedVersion: 3);

        // Second rebuild: starts from snapshot, only processes 1 new event
        var (state2, ver2) = await engine.RebuildAsync("s", 0);
        Assert.That(state2, Is.EqualTo(4));
        Assert.That(ver2, Is.EqualTo(4));
    }

    // ── Challenge 3: Concurrent Append Detection ────────────────────────────

    [Test]
    public async Task Challenge3_ConcurrentAppendDetection_OptimisticConcurrency()
    {
        var options = Options.Create(new EventSourcingOptions());
        var store = new InMemoryEventStore(options, NullLogger<InMemoryEventStore>.Instance);

        // Two writers both read version 0
        var writerA = new EventEnvelope(Guid.NewGuid(), "stream", "A", "a", 0, DateTimeOffset.UtcNow, []);
        var writerB = new EventEnvelope(Guid.NewGuid(), "stream", "B", "b", 0, DateTimeOffset.UtcNow, []);

        // Writer A succeeds
        var newVersion = await store.AppendAsync("stream", [writerA], expectedVersion: 0);
        Assert.That(newVersion, Is.EqualTo(1));

        // Writer B fails because stream is now at version 1, not 0
        var ex = Assert.ThrowsAsync<OptimisticConcurrencyException>(
            () => store.AppendAsync("stream", [writerB], expectedVersion: 0));

        Assert.That(ex!.StreamId, Is.EqualTo("stream"));
        Assert.That(ex.ExpectedVersion, Is.EqualTo(0));
        Assert.That(ex.ActualVersion, Is.EqualTo(1));

        // Writer B retries with correct version and succeeds
        var retryVersion = await store.AppendAsync("stream", [writerB], expectedVersion: 1);
        Assert.That(retryVersion, Is.EqualTo(2));

        // Verify both events are in the stream
        var allEvents = await store.ReadStreamAsync("stream", fromVersion: 1, count: 100);
        Assert.That(allEvents, Has.Count.EqualTo(2));
    }
}
