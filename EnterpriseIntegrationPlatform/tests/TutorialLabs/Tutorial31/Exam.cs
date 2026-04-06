// ============================================================================
// Tutorial 31 – Event Sourcing (Exam)
// ============================================================================
// EIP Pattern: Event Sourcing
// E2E: Projection rebuild, snapshot integration, and concurrent append
//      detection with MockEndpoint for event notification publishing.
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
    [Test]
    public async Task Challenge1_ProjectionEngine_RebuildsSumFromEvents()
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

    [Test]
    public async Task Challenge2_SnapshotAcceleratesRebuild()
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

    [Test]
    public async Task Challenge3_ConcurrentAppend_DetectsConflict()
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
