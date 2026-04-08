// ============================================================================
// Tutorial 31 – Event Sourcing (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Projection engine rebuilds sum from events
//   🟡 Intermediate  — Snapshot accelerates rebuild from midstream
//   🔴 Advanced      — Concurrent append detects optimistic concurrency conflict
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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
        // TODO: Create a InMemoryEventStore with appropriate configuration
        dynamic store = null!;

        // TODO: Create a EventEnvelope with appropriate configuration
        dynamic e1 = null!;
        // TODO: Create a EventEnvelope with appropriate configuration
        dynamic e2 = null!;
        await store.AppendAsync("account-1", [e1, e2], 0);

        // TODO: Create a MockEventProjection with appropriate configuration
        dynamic projection = null!;

        // TODO: Create a InMemorySnapshotStore with appropriate configuration
        dynamic snapStore = null!;
        // TODO: Create a EventProjectionEngine with appropriate configuration
        dynamic engine = null!;

        var (state, version) = await engine.RebuildAsync("account-1", 0);
        Assert.That(state, Is.EqualTo(80));
        Assert.That(version, Is.EqualTo(2));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
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
        // TODO: Create a InMemoryEventStore with appropriate configuration
        dynamic store = null!;

        var events = Enumerable.Range(1, 5)
            .Select(i => new EventEnvelope(Guid.NewGuid(), "s1", "Inc", "10", i,
                DateTimeOffset.UtcNow, new Dictionary<string, string>()))
            .ToList();
        await store.AppendAsync("s1", events, 0);

        // TODO: Create a InMemorySnapshotStore with appropriate configuration
        dynamic snapStore = null!;
        await snapStore.SaveAsync("s1", 30, 3);

        // TODO: Create a MockEventProjection with appropriate configuration
        dynamic projection = null!;

        // TODO: Create a EventProjectionEngine with appropriate configuration
        dynamic engine = null!;

        var (state, version) = await engine.RebuildAsync("s1", 0);
        Assert.That(state, Is.EqualTo(50));
        Assert.That(version, Is.EqualTo(5));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
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
        // TODO: Create a InMemoryEventStore with appropriate configuration
        dynamic store = null!;

        // TODO: Create a EventEnvelope with appropriate configuration
        dynamic e1 = null!;
        await store.AppendAsync("conflict-s", [e1], 0);

        // TODO: Create a EventEnvelope with appropriate configuration
        dynamic conflict = null!;
        var ex = Assert.ThrowsAsync<OptimisticConcurrencyException>(
            () => store.AppendAsync("conflict-s", [conflict], 0));

        Assert.That(ex!.StreamId, Is.EqualTo("conflict-s"));
        Assert.That(ex.ExpectedVersion, Is.EqualTo(0));
        Assert.That(ex.ActualVersion, Is.EqualTo(1));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("conflicts", 1);
    }
}
#endif
