// ============================================================================
// Tutorial 11 – Dynamic Router (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Dynamic Router builds and
//   manages its routing table at runtime, resolves destinations, and handles
//   fallback scenarios.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Route resolution — a registered key routes to its destination topic
//   2. Fallback routing — an unmatched key falls back to the default topic
//   3. No fallback — missing fallback topic throws InvalidOperationException
//   4. Route update — re-registering a key replaces the previous destination
//   5. Unregister — removing a key causes subsequent messages to fall back
//   6. Unregister miss — unregistering a non-existent key returns false
//   7. Routing table — GetRoutingTable returns a snapshot of all entries
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial11;

[TestFixture]
public sealed class Lab
{
    // ── 1. Route Resolution (Real NATS) ──────────────────────────────

    [Test]
    public async Task Route_RegisteredKey_RoutesToDestination()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t11-reg");
        var targetTopic = AspireFixture.UniqueTopic("t11-orders");
        var router = CreateRouter(nats);
        await router.RegisterAsync("order.created", targetTopic);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo(targetTopic));
        Assert.That(decision.IsFallback, Is.False);
        Assert.That(decision.MatchedEntry, Is.Not.Null);
        Assert.That(decision.ConditionValue, Is.EqualTo("order.created"));
        nats.AssertReceivedOnTopic(targetTopic, 1);
    }

    [Test]
    public async Task Route_UnregisteredKey_FallsBackToDefault()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t11-fallback");
        var fallback = AspireFixture.UniqueTopic("t11-dl");
        var router = CreateRouter(nats, fallback: fallback);
        await router.RegisterAsync("order.created", "orders-topic");

        var envelope = IntegrationEnvelope<string>.Create(
            "unknown", "Svc", "unknown.event");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo(fallback));
        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.MatchedEntry, Is.Null);
        nats.AssertReceivedOnTopic(fallback, 1);
    }

    [Test]
    public async Task Route_NoMatchNoFallback_ThrowsInvalidOperation()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t11-nofb");
        var router = CreateRouter(nats, fallback: null);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "no.match");

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await router.RouteAsync(envelope));
        nats.AssertNoneReceived();
    }


    // ── 2. Runtime Route Management (Real NATS) ──────────────────────

    [Test]
    public async Task Register_UpdatesExistingRoute()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t11-update");
        var newTopic = AspireFixture.UniqueTopic("t11-new");
        var router = CreateRouter(nats);
        await router.RegisterAsync("order.created", "old-topic");
        await router.RegisterAsync("order.created", newTopic);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo(newTopic));
        nats.AssertReceivedOnTopic(newTopic, 1);
    }

    [Test]
    public async Task Unregister_RemovesRoute_FallsBack()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t11-unreg");
        var fallback = AspireFixture.UniqueTopic("t11-fb");
        var router = CreateRouter(nats, fallback: fallback);
        await router.RegisterAsync("order.created", "orders-topic");
        var removed = await router.UnregisterAsync("order.created");

        Assert.That(removed, Is.True);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.IsFallback, Is.True);
        nats.AssertReceivedOnTopic(fallback, 1);
    }

    [Test]
    public async Task Unregister_NonExistentKey_ReturnsFalse()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t11-nokey");
        var router = CreateRouter(nats);

        var removed = await router.UnregisterAsync("no-such-key");

        Assert.That(removed, Is.False);
    }


    // ── 3. Routing Table Introspection ───────────────────────────────

    [Test]
    public async Task GetRoutingTable_ReturnsSnapshot()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t11-table");
        var router = CreateRouter(nats);
        await router.RegisterAsync("order.created", "orders-topic", "participant-1");
        await router.RegisterAsync("payment.received", "payments-topic", "participant-2");

        var table = router.GetRoutingTable();

        Assert.That(table, Has.Count.EqualTo(2));
        Assert.That(table.ContainsKey("order.created"), Is.True);
        Assert.That(table["order.created"].Destination, Is.EqualTo("orders-topic"));
        Assert.That(table["order.created"].ParticipantId, Is.EqualTo("participant-1"));
    }

    private static DynamicRouter CreateRouter(
        NatsBrokerEndpoint nats, string? fallback = "catch-all")
    {
        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = fallback,
            CaseInsensitive = true,
        });
        return new DynamicRouter(
            nats, options, NullLogger<DynamicRouter>.Instance);
    }
}
