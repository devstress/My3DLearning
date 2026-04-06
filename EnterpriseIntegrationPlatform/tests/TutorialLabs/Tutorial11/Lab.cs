// ============================================================================
// Tutorial 11 – Dynamic Router (Lab)
// ============================================================================
// This lab exercises the DynamicRouter pattern — a router whose routing table
// is updated at runtime by downstream participants via a control channel.
// You will register and unregister routes, verify routing decisions, test
// case-insensitive matching, and confirm fallback behaviour.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial11;

[TestFixture]
public sealed class Lab
{
    // ── Register a Route and Route a Matching Message ───────────────────────

    [Test]
    public async Task Route_RegisteredCondition_RoutesToRegisteredDestination()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "unmatched-topic",
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        // Register a dynamic route for "order.created" messages.
        await router.RegisterAsync("order.created", "orders-topic", "OrderService");

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("orders-topic"));
        Assert.That(decision.IsFallback, Is.False);
        Assert.That(decision.MatchedEntry, Is.Not.Null);
        Assert.That(decision.MatchedEntry!.ParticipantId, Is.EqualTo("OrderService"));
        Assert.That(decision.ConditionValue, Is.EqualTo("order.created"));
    }

    // ── Unmatched Message Falls Back to FallbackTopic ───────────────────────

    [Test]
    public async Task Route_NoMatchingRoute_UsesFallbackTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "catch-all-topic",
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        // No routes registered — everything falls back.
        var envelope = IntegrationEnvelope<string>.Create(
            "unknown-data", "UnknownService", "unknown.event");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("catch-all-topic"));
        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.MatchedEntry, Is.Null);
    }

    // ── Unregister Removes Route Entry ──────────────────────────────────────

    [Test]
    public async Task Unregister_RemovesRoute_SubsequentMessageUsesFallback()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "fallback-topic",
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        await router.RegisterAsync("order.created", "orders-topic");

        // Unregister the route.
        var removed = await router.UnregisterAsync("order.created");
        Assert.That(removed, Is.True);

        // Now routing should fall back.
        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");

        var decision = await router.RouteAsync(envelope);
        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.Destination, Is.EqualTo("fallback-topic"));
    }

    // ── Case-Insensitive Routing (Default) ──────────────────────────────────

    [Test]
    public async Task Route_CaseInsensitive_MatchesRegardlessOfCase()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "fallback",
            CaseInsensitive = true,
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        // Register with lowercase key.
        await router.RegisterAsync("order.created", "orders-topic");

        // Route with mixed case — should still match.
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Service", "Order.Created");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("orders-topic"));
        Assert.That(decision.IsFallback, Is.False);
    }

    // ── GetRoutingTable Returns Current Snapshot ─────────────────────────────

    [Test]
    public async Task GetRoutingTable_ReturnsAllRegisteredEntries()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "fallback",
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        await router.RegisterAsync("order.created", "orders-topic", "OrderService");
        await router.RegisterAsync("payment.received", "payments-topic", "PaymentService");

        var table = router.GetRoutingTable();

        Assert.That(table, Has.Count.EqualTo(2));
        Assert.That(table.ContainsKey("order.created"), Is.True);
        Assert.That(table.ContainsKey("payment.received"), Is.True);
        Assert.That(table["order.created"].Destination, Is.EqualTo("orders-topic"));
        Assert.That(table["payment.received"].Destination, Is.EqualTo("payments-topic"));
    }

    // ── No Fallback Configured Throws ───────────────────────────────────────

    [Test]
    public void Route_NoFallbackConfigured_ThrowsInvalidOperationException()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        // FallbackTopic is null — no safety net.
        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = null,
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Service", "unknown.event");

        Assert.ThrowsAsync<InvalidOperationException>(
            () => router.RouteAsync(envelope));
    }

    // ── Routing by Metadata Field ───────────────────────────────────────────

    [Test]
    public async Task Route_ByMetadataField_MatchesDynamicEntry()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "Metadata.region",
            FallbackTopic = "global-topic",
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        await router.RegisterAsync("eu-west", "eu-west-topic", "EUService");

        var envelope = IntegrationEnvelope<string>.Create(
            "eu-data", "RegionalService", "data.sync") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "eu-west",
            },
        };

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("eu-west-topic"));
        Assert.That(decision.IsFallback, Is.False);
        Assert.That(decision.MatchedEntry!.ParticipantId, Is.EqualTo("EUService"));
    }
}
