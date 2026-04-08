// ============================================================================
// Tutorial 31 – Event Sourcing (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Event Sourcing pattern stores
//          domain events as an append-only log and reads them forward/backward.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. AppendAndReadForward_RoundTrip                    — append and read-forward round trip
//   2. AppendMultipleEvents_VersionsIncrement            — multiple events increment versions
//   3. ReadStreamBackward_ReturnsDescendingOrder         — backward read returns descending order
//   4. OptimisticConcurrency_ThrowsOnVersionMismatch     — version mismatch throws concurrency exception
//   5. ReadFromMiddleOfStream_ReturnsSubset              — read from middle returns subset
//   6. EmptyStream_ReturnsEmptyList                      — empty stream returns empty list
//   7. PublishAllEventsToMockEndpoint                    — events publish to endpoint
//
// INFRASTRUCTURE: MockEndpoint
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial31;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("event-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    private static InMemoryEventStore CreateStore(int maxPerRead = 1000) =>
        new(Options.Create(new EventSourcingOptions { MaxEventsPerRead = maxPerRead }),
            NullLogger<InMemoryEventStore>.Instance);

    private static EventEnvelope MakeEvent(string streamId, string type, string data, long version) =>
        new(Guid.NewGuid(), streamId, type, data, version,
            DateTimeOffset.UtcNow, new Dictionary<string, string>());


    // ── 1. Event Appending ───────────────────────────────────────────

    [Test]
    public async Task AppendAndReadForward_RoundTrip()
    {
        var store = CreateStore();
        var evt = MakeEvent("order-1", "OrderCreated", "{\"total\":100}", 1);
        var newVersion = await store.AppendAsync("order-1", [evt], 0);
        Assert.That(newVersion, Is.EqualTo(1));

        var events = await store.ReadStreamAsync("order-1", 1, 10);
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].EventType, Is.EqualTo("OrderCreated"));

        var envelope = IntegrationEnvelope<string>.Create(events[0].Data, "event-store", "OrderCreated");
        await _output.PublishAsync(envelope, "event-notifications", default);
        _output.AssertReceivedOnTopic("event-notifications", 1);
    }

    [Test]
    public async Task AppendMultipleEvents_VersionsIncrement()
    {
        var store = CreateStore();
        var e1 = MakeEvent("stream-a", "Created", "{}", 1);
        var e2 = MakeEvent("stream-a", "Updated", "{}", 2);
        var newVersion = await store.AppendAsync("stream-a", [e1, e2], 0);
        Assert.That(newVersion, Is.EqualTo(2));

        var events = await store.ReadStreamAsync("stream-a", 1, 10);
        Assert.That(events, Has.Count.EqualTo(2));
        Assert.That(events[0].Version, Is.EqualTo(1));
        Assert.That(events[1].Version, Is.EqualTo(2));
        await Task.CompletedTask;
    }


    // ── 2. Stream Navigation ─────────────────────────────────────────

    [Test]
    public async Task ReadStreamBackward_ReturnsDescendingOrder()
    {
        var store = CreateStore();
        var e1 = MakeEvent("s1", "A", "{}", 1);
        var e2 = MakeEvent("s1", "B", "{}", 2);
        var e3 = MakeEvent("s1", "C", "{}", 3);
        await store.AppendAsync("s1", [e1, e2, e3], 0);

        var events = await store.ReadStreamBackwardAsync("s1", 3, 10);
        Assert.That(events, Has.Count.EqualTo(3));
        Assert.That(events[0].EventType, Is.EqualTo("C"));
        Assert.That(events[2].EventType, Is.EqualTo("A"));
    }

    [Test]
    public async Task OptimisticConcurrency_ThrowsOnVersionMismatch()
    {
        var store = CreateStore();
        var e1 = MakeEvent("s2", "Init", "{}", 1);
        await store.AppendAsync("s2", [e1], 0);

        var e2 = MakeEvent("s2", "Conflict", "{}", 2);
        Assert.ThrowsAsync<OptimisticConcurrencyException>(
            () => store.AppendAsync("s2", [e2], 0));
    }

    [Test]
    public async Task ReadFromMiddleOfStream_ReturnsSubset()
    {
        var store = CreateStore();
        var events = Enumerable.Range(1, 5)
            .Select(i => MakeEvent("s3", $"E{i}", "{}", i))
            .ToList();
        await store.AppendAsync("s3", events, 0);

        var subset = await store.ReadStreamAsync("s3", 3, 10);
        Assert.That(subset, Has.Count.EqualTo(3));
        Assert.That(subset[0].Version, Is.EqualTo(3));
    }


    // ── 3. Notification Publishing ───────────────────────────────────

    [Test]
    public async Task EmptyStream_ReturnsEmptyList()
    {
        var store = CreateStore();
        var events = await store.ReadStreamAsync("nonexistent", 1, 10);
        Assert.That(events, Is.Empty);
    }

    [Test]
    public async Task PublishAllEventsToMockEndpoint()
    {
        var store = CreateStore();
        var e1 = MakeEvent("pub-1", "A", "{\"v\":1}", 1);
        var e2 = MakeEvent("pub-1", "B", "{\"v\":2}", 2);
        await store.AppendAsync("pub-1", [e1, e2], 0);

        var stream = await store.ReadStreamAsync("pub-1", 1, 10);
        foreach (var evt in stream)
        {
            var envelope = IntegrationEnvelope<string>.Create(evt.Data, "event-store", evt.EventType);
            await _output.PublishAsync(envelope, "event-stream", default);
        }

        _output.AssertReceivedOnTopic("event-stream", 2);
    }
}
