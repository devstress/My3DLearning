// ============================================================================
// Tutorial 11 – Dynamic Router (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — multi-participant topology routed correctly with fallback
//   🟡 Intermediate  — route replacement semantics when a new participant overrides
//   🔴 Advanced      — case-insensitive matching across mixed-case condition keys
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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

        // TODO: await router.RegisterAsync(...)
        // TODO: await router.RegisterAsync(...)
        // TODO: await router.RegisterAsync(...)

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e1 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e2 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e3 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e4 = null!;

        // TODO: var d1 = await router.RouteAsync(...)
        dynamic d1 = null!;
        // TODO: var d2 = await router.RouteAsync(...)
        dynamic d2 = null!;
        // TODO: var d3 = await router.RouteAsync(...)
        dynamic d3 = null!;
        // TODO: var d4 = await router.RouteAsync(...)
        dynamic d4 = null!;

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

        // TODO: await router.RegisterAsync(...)
        // TODO: await router.RegisterAsync(...)

        var table = router.GetRoutingTable();
        Assert.That(table["order.created"].Destination, Is.EqualTo("new-handler"));
        Assert.That(table["order.created"].ParticipantId, Is.EqualTo("participant-v2"));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var decision = await router.RouteAsync(...)
        dynamic decision = null!;

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

        // TODO: await router.RegisterAsync(...)

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var decision = await router.RouteAsync(...)
        dynamic decision = null!;

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
#endif
