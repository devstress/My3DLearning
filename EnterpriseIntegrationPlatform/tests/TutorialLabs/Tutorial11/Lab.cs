// ============================================================================
// Tutorial 11 – Dynamic Router (Lab)
// ============================================================================
// EIP Pattern: Dynamic Router
// E2E: Wire real DynamicRouter with MockEndpoint as producer, register/
// unregister routes at runtime, verify routing decisions and message delivery.
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
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("dynamic-router-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task Route_RegisteredKey_RoutesToDestination()
    {
        var router = CreateRouter();
        await router.RegisterAsync("order.created", "orders-topic");

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("orders-topic"));
        Assert.That(decision.IsFallback, Is.False);
        Assert.That(decision.MatchedEntry, Is.Not.Null);
        Assert.That(decision.ConditionValue, Is.EqualTo("order.created"));
        _output.AssertReceivedOnTopic("orders-topic", 1);
    }

    [Test]
    public async Task Route_UnregisteredKey_FallsBackToDefault()
    {
        var router = CreateRouter(fallback: "dead-letter");
        await router.RegisterAsync("order.created", "orders-topic");

        var envelope = IntegrationEnvelope<string>.Create(
            "unknown", "Svc", "unknown.event");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("dead-letter"));
        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.MatchedEntry, Is.Null);
        _output.AssertReceivedOnTopic("dead-letter", 1);
    }

    [Test]
    public async Task Route_NoMatchNoFallback_ThrowsInvalidOperation()
    {
        var router = CreateRouter(fallback: null);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "no.match");

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await router.RouteAsync(envelope));
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task Register_UpdatesExistingRoute()
    {
        var router = CreateRouter();
        await router.RegisterAsync("order.created", "old-topic");
        await router.RegisterAsync("order.created", "new-topic");

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("new-topic"));
        _output.AssertReceivedOnTopic("new-topic", 1);
    }

    [Test]
    public async Task Unregister_RemovesRoute_FallsBack()
    {
        var router = CreateRouter(fallback: "fallback-topic");
        await router.RegisterAsync("order.created", "orders-topic");
        var removed = await router.UnregisterAsync("order.created");

        Assert.That(removed, Is.True);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.IsFallback, Is.True);
        _output.AssertReceivedOnTopic("fallback-topic", 1);
    }

    [Test]
    public async Task Unregister_NonExistentKey_ReturnsFalse()
    {
        var router = CreateRouter();

        var removed = await router.UnregisterAsync("no-such-key");

        Assert.That(removed, Is.False);
    }

    [Test]
    public async Task GetRoutingTable_ReturnsSnapshot()
    {
        var router = CreateRouter();
        await router.RegisterAsync("order.created", "orders-topic", "participant-1");
        await router.RegisterAsync("payment.received", "payments-topic", "participant-2");

        var table = router.GetRoutingTable();

        Assert.That(table, Has.Count.EqualTo(2));
        Assert.That(table.ContainsKey("order.created"), Is.True);
        Assert.That(table["order.created"].Destination, Is.EqualTo("orders-topic"));
        Assert.That(table["order.created"].ParticipantId, Is.EqualTo("participant-1"));
    }

    private DynamicRouter CreateRouter(string? fallback = "catch-all")
    {
        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = fallback,
            CaseInsensitive = true,
        });
        return new DynamicRouter(
            _output, options, NullLogger<DynamicRouter>.Instance);
    }
}
