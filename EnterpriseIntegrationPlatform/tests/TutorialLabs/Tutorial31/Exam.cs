// ============================================================================
// Tutorial 31 – Event Sourcing (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Event Sourcing pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Projection engine rebuilds sum from events
//   🟡 Intermediate — Snapshot accelerates rebuild from midstream
//   🔴 Advanced     — Concurrent append detects optimistic concurrency conflict
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial31;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Projection rebuild ──────────────────────────────
    //
    // SCENARIO: Two deposit events are appended. The projection engine
    //           replays them to rebuild the running total from scratch.
    //
    // WHAT YOU PROVE: The projection engine rebuilds aggregate state
    //                 correctly by replaying all events in the stream.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_ProjectionEngine_RebuildsSumFromEvents()
    {
        await using var output = new MockEndpoint("exam-es");
        var store = new InMemoryEventStore(
            Options.Create(new EventSourcingOptions()),
            NullLogger<InMemoryEventStore>.Instance);

        var e1 = new EventEnvelope(Guid.NewGuid(), "account-1", "Deposit", "50", 1,
            DateTimeOffset.UtcNow, new Dictionary<string, string>());
        var e2 = new EventEnvelope(Guid.NewGuid(), "account-1", "Deposit", "30", 2,
            DateTimeOffset.UtcNow, new Dictionary<string, string>());
        await store.AppendAsync("account-1", [e1, e2], 0);

        var projection = new MockEventProjection<int>((state, envelope) => state + int.Parse(envelope.Data));

        var snapStore = new InMemorySnapshotStore<int>();
        var engine = new EventProjectionEngine<int>(
            store, snapStore, projection,
            Options.Create(new EventSourcingOptions { SnapshotInterval = 100 }),
            NullLogger<EventProjectionEngine<int>>.Instance);

        var (state, version) = await engine.RebuildAsync("account-1", 0);
        Assert.That(state, Is.EqualTo(80));
        Assert.That(version, Is.EqualTo(2));

        var envelope = IntegrationEnvelope<string>.Create(
            state.ToString(), "projection", "BalanceRebuilt");
        await output.PublishAsync(envelope, "projections", default);
        output.AssertReceivedOnTopic("projections", 1);
    }

    // ── 🟡 INTERMEDIATE — Snapshot-accelerated rebuild ──────────────────
    //
    // SCENARIO: Five events are appended but a snapshot at version 3 with
    //           state=30 is pre-saved. The rebuild starts from the snapshot
    //           and only replays events 4-5.
    //
    // WHAT YOU PROVE: Snapshots accelerate rebuilds by skipping already-
    //                 projected events, and the final state is correct.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_SnapshotAcceleratesRebuild()
    {
        await using var output = new MockEndpoint("exam-snap");
        var store = new InMemoryEventStore(
            Options.Create(new EventSourcingOptions()),
            NullLogger<InMemoryEventStore>.Instance);

        var events = Enumerable.Range(1, 5)
            .Select(i => new EventEnvelope(Guid.NewGuid(), "s1", "Inc", "10", i,
                DateTimeOffset.UtcNow, new Dictionary<string, string>()))
            .ToList();
        await store.AppendAsync("s1", events, 0);

        var snapStore = new InMemorySnapshotStore<int>();
        await snapStore.SaveAsync("s1", 30, 3);

        var projection = new MockEventProjection<int>((state, envelope) => state + int.Parse(envelope.Data));

        var engine = new EventProjectionEngine<int>(
            store, snapStore, projection,
            Options.Create(new EventSourcingOptions { SnapshotInterval = 100 }),
            NullLogger<EventProjectionEngine<int>>.Instance);

        var (state, version) = await engine.RebuildAsync("s1", 0);
        Assert.That(state, Is.EqualTo(50));
        Assert.That(version, Is.EqualTo(5));

        var envelope = IntegrationEnvelope<string>.Create(
            state.ToString(), "projection", "Rebuilt");
        await output.PublishAsync(envelope, "snapshot-results", default);
        output.AssertReceivedOnTopic("snapshot-results", 1);
    }

    // ── 🔴 ADVANCED — Concurrent append conflict detection ─────────────
    //
    // SCENARIO: One event is appended at version 0→1. A second append also
    //           claims expectedVersion=0, triggering a conflict.
    //
    // WHAT YOU PROVE: Optimistic concurrency correctly detects the version
    //                 mismatch and throws OptimisticConcurrencyException
    //                 with accurate StreamId, ExpectedVersion, and ActualVersion.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_ConcurrentAppend_DetectsConflict()
    {
        await using var output = new MockEndpoint("exam-conflict");
        var store = new InMemoryEventStore(
            Options.Create(new EventSourcingOptions()),
            NullLogger<InMemoryEventStore>.Instance);

        var e1 = new EventEnvelope(Guid.NewGuid(), "conflict-s", "Init", "{}", 1,
            DateTimeOffset.UtcNow, new Dictionary<string, string>());
        await store.AppendAsync("conflict-s", [e1], 0);

        var conflict = new EventEnvelope(Guid.NewGuid(), "conflict-s", "Bad", "{}", 2,
            DateTimeOffset.UtcNow, new Dictionary<string, string>());
        var ex = Assert.ThrowsAsync<OptimisticConcurrencyException>(
            () => store.AppendAsync("conflict-s", [conflict], 0));

        Assert.That(ex!.StreamId, Is.EqualTo("conflict-s"));
        Assert.That(ex.ExpectedVersion, Is.EqualTo(0));
        Assert.That(ex.ActualVersion, Is.EqualTo(1));

        var envelope = IntegrationEnvelope<string>.Create(
            $"Conflict on {ex.StreamId}", "guard", "ConflictDetected");
        await output.PublishAsync(envelope, "conflicts", default);
        output.AssertReceivedOnTopic("conflicts", 1);
    }
}
