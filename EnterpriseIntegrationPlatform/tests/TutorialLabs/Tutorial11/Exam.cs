// ============================================================================
// Tutorial 11 – Dynamic Router (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Dynamic Router pattern in realistic
//   integration scenarios that combine registration, routing, fallback, and
//   introspection.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — multi-participant topology routed correctly with fallback
//   🟡 Intermediate — route replacement semantics when a new participant overrides
//   🔴 Advanced     — case-insensitive matching across mixed-case condition keys
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
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial11;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Multi-Participant Topology ───────────────────────
    //
    // SCENARIO: Three microservices (orders, payments, shipments) each
    //   register their own route with the Dynamic Router. A fourth message
    //   arrives with an unknown event type.
    //
    // WHAT YOU PROVE: The router dispatches each message to the correct
    //   participant topic and falls back for the unrecognised event.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Starter_MultiParticipantTopology_RoutesCorrectly()
    {
        await using var output = new MockEndpoint("multi-participant");
        var router = CreateRouter(output);

        await router.RegisterAsync("order.created", "order-svc-topic", "order-service");
        await router.RegisterAsync("payment.received", "payment-svc-topic", "payment-service");
        await router.RegisterAsync("shipment.dispatched", "shipment-svc-topic", "shipment-service");

        var e1 = IntegrationEnvelope<string>.Create("o1", "svc", "order.created");
        var e2 = IntegrationEnvelope<string>.Create("p1", "svc", "payment.received");
        var e3 = IntegrationEnvelope<string>.Create("s1", "svc", "shipment.dispatched");
        var e4 = IntegrationEnvelope<string>.Create("u1", "svc", "unknown.event");

        var d1 = await router.RouteAsync(e1);
        var d2 = await router.RouteAsync(e2);
        var d3 = await router.RouteAsync(e3);
        var d4 = await router.RouteAsync(e4);

        Assert.That(d1.Destination, Is.EqualTo("order-svc-topic"));
        Assert.That(d2.Destination, Is.EqualTo("payment-svc-topic"));
        Assert.That(d3.Destination, Is.EqualTo("shipment-svc-topic"));
        Assert.That(d4.IsFallback, Is.True);

        output.AssertReceivedCount(4);
        output.AssertReceivedOnTopic("order-svc-topic", 1);
        output.AssertReceivedOnTopic("payment-svc-topic", 1);
        output.AssertReceivedOnTopic("shipment-svc-topic", 1);
    }

    // ── 🟡 INTERMEDIATE — Route Replacement Semantics ──────────────────
    //
    // SCENARIO: Version 1 of the order handler registers a route. A new
    //   deployment (v2) re-registers the same condition key with a different
    //   destination and participant ID.
    //
    // WHAT YOU PROVE: The latest registration wins — the routing table and
    //   subsequent routing decisions reflect the v2 destination.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Intermediate_RouteReplacement_NewParticipantOverrides()
    {
        await using var output = new MockEndpoint("replacement");
        var router = CreateRouter(output);

        await router.RegisterAsync("order.created", "old-handler", "participant-v1");
        await router.RegisterAsync("order.created", "new-handler", "participant-v2");

        var table = router.GetRoutingTable();
        Assert.That(table["order.created"].Destination, Is.EqualTo("new-handler"));
        Assert.That(table["order.created"].ParticipantId, Is.EqualTo("participant-v2"));

        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("new-handler"));
        Assert.That(decision.MatchedEntry!.ParticipantId, Is.EqualTo("participant-v2"));
        output.AssertReceivedOnTopic("new-handler", 1);
    }

    // ── 🔴 ADVANCED — Case-Insensitive Matching ────────────────────────
    //
    // SCENARIO: A route is registered with mixed-case key "Order.Created",
    //   but the incoming message carries the all-lowercase "order.created".
    //
    // WHAT YOU PROVE: With CaseInsensitive enabled, the router matches
    //   regardless of casing and delivers the message to the registered topic.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Advanced_CaseInsensitive_MatchesRegardlessOfCase()
    {
        await using var output = new MockEndpoint("case-insensitive");
        var router = CreateRouter(output);

        await router.RegisterAsync("Order.Created", "orders-topic");

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "svc", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("orders-topic"));
        Assert.That(decision.IsFallback, Is.False);
        output.AssertReceivedOnTopic("orders-topic", 1);
    }

    private static DynamicRouter CreateRouter(MockEndpoint output)
    {
        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "unmatched",
            CaseInsensitive = true,
        });
        return new DynamicRouter(
            output, options, NullLogger<DynamicRouter>.Instance);
    }
}
