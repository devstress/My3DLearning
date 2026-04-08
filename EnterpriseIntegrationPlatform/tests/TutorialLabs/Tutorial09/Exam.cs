// ============================================================================
// Tutorial 09 – Content-Based Router (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Multi-rule regional routing with fallback to global
//   🟡 Intermediate — Route by JSON payload fields using JsonElement
//   🔴 Advanced     — Batch routing with per-topic count verification
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial09;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Regional Order Routing ───────────────────────────────
    //
    // SCENARIO: An e-commerce platform routes orders to regional fulfilment
    // centres based on the "region" metadata field. US-East and EU-West
    // orders go to dedicated topics; unknown regions fall back to global.
    //
    // WHAT YOU PROVE: You can configure multi-rule routing with metadata
    // matching and verify fallback behavior for unmatched regions.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Starter_RegionalRouting_MatchesAndFallsBack()
    {
        await using var output = new MockEndpoint("regional");
        // TODO: Create RouterOptions with two RoutingRules:
        //   Rule 1: Priority=1, Name="US-East", FieldName="Metadata.region", Operator=Equals, Value="us-east", TargetTopic="fulfilment.us-east"
        //   Rule 2: Priority=2, Name="EU-West", FieldName="Metadata.region", Operator=Equals, Value="eu-west", TargetTopic="fulfilment.eu-west"
        //   DefaultTopic = "fulfilment.global"
        var options = Options.Create(new RouterOptions()); // ← replace with full RouterOptions configuration
        // TODO: Create a ContentBasedRouter with output, options, and NullLogger
        ContentBasedRouter router = null!; // ← replace with new ContentBasedRouter(...)

        // TODO: Create usOrder — IntegrationEnvelope<string> "o1"/"svc"/"order.created" with Metadata region = "us-east"
        IntegrationEnvelope<string> usOrder = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Metadata = ... }
        // TODO: Create euOrder — IntegrationEnvelope<string> "o2"/"svc"/"order.created" with Metadata region = "eu-west"
        IntegrationEnvelope<string> euOrder = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Metadata = ... }
        // TODO: Create unknownOrder — IntegrationEnvelope<string> "o3"/"svc"/"order.created" with Metadata region = "af-south"
        IntegrationEnvelope<string> unknownOrder = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Metadata = ... }

        Assert.That((await router.RouteAsync(usOrder)).TargetTopic, Is.EqualTo("fulfilment.us-east"));
        Assert.That((await router.RouteAsync(euOrder)).TargetTopic, Is.EqualTo("fulfilment.eu-west"));
        Assert.That((await router.RouteAsync(unknownOrder)).IsDefault, Is.True);

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("fulfilment.us-east", 1);
        output.AssertReceivedOnTopic("fulfilment.eu-west", 1);
        output.AssertReceivedOnTopic("fulfilment.global", 1);
    }

    // ── 🟡 INTERMEDIATE — JSON Payload-Based Routing ──────────────────────
    //
    // SCENARIO: A payment processing system routes orders by their status
    // field inside the JSON payload — urgent orders to priority processing,
    // normal orders to standard processing, and unknown statuses to default.
    //
    // WHAT YOU PROVE: You can route messages based on JsonElement payload
    // fields and verify correct topic assignment for each status level.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Intermediate_PayloadRouting_JsonElementField()
    {
        await using var output = new MockEndpoint("payload");
        // TODO: Create RouterOptions with two RoutingRules:
        //   Rule 1: Priority=1, Name="Urgent", FieldName="Payload.status", Operator=Equals, Value="urgent", TargetTopic="urgent-processing"
        //   Rule 2: Priority=2, Name="Normal", FieldName="Payload.status", Operator=Equals, Value="normal", TargetTopic="normal-processing"
        //   DefaultTopic = "default-processing"
        var options = Options.Create(new RouterOptions()); // ← replace with full RouterOptions configuration
        // TODO: Create a ContentBasedRouter with output, options, and NullLogger
        ContentBasedRouter router = null!; // ← replace with new ContentBasedRouter(...)

        // TODO: Deserialize each JSON string to JsonElement and route via router.RouteAsync
        //   urgentJson: {"status":"urgent","amount":5000}
        //   normalJson: {"status":"normal","amount":50}
        //   unknownJson: {"status":"backorder","amount":10}
        var urgentJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"status\":\"urgent\",\"amount\":5000}");
        var normalJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"status\":\"normal\",\"amount\":50}");
        var unknownJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"status\":\"backorder\",\"amount\":10}");

        // TODO: Route each JSON envelope and capture the routing decision
        RoutingDecision d1 = null!; // ← replace with await router.RouteAsync(IntegrationEnvelope<JsonElement>.Create(urgentJson, "svc", "order"))
        RoutingDecision d2 = null!; // ← replace with await router.RouteAsync(IntegrationEnvelope<JsonElement>.Create(normalJson, "svc", "order"))
        RoutingDecision d3 = null!; // ← replace with await router.RouteAsync(IntegrationEnvelope<JsonElement>.Create(unknownJson, "svc", "order"))
        _ = router; // suppress unused-variable warning — remove after filling in RouteAsync calls above

        Assert.That(d1.TargetTopic, Is.EqualTo("urgent-processing"));
        Assert.That(d2.TargetTopic, Is.EqualTo("normal-processing"));
        Assert.That(d3.IsDefault, Is.True);

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("urgent-processing", 1);
        output.AssertReceivedOnTopic("normal-processing", 1);
        output.AssertReceivedOnTopic("default-processing", 1);
    }

    // ── 🔴 ADVANCED — Batch Routing with Topic Verification ───────────────
    //
    // SCENARIO: A message hub receives a batch of four messages — two orders,
    // one payment, and one inventory update. Each must be routed to the
    // correct topic using StartsWith rules, with per-topic count verification.
    //
    // WHAT YOU PROVE: You can route a batch of mixed message types and
    // verify exact per-topic delivery counts across multiple topics.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Advanced_BatchRouting_MultipleMessagesVerifyTopics()
    {
        await using var output = new MockEndpoint("batch");
        // TODO: Create RouterOptions with two RoutingRules:
        //   Rule 1: Priority=1, FieldName="MessageType", Operator=StartsWith, Value="order", TargetTopic="orders"
        //   Rule 2: Priority=2, FieldName="MessageType", Operator=StartsWith, Value="payment", TargetTopic="payments"
        //   DefaultTopic = "other"
        var options = Options.Create(new RouterOptions()); // ← replace with full RouterOptions configuration
        // TODO: Create a ContentBasedRouter with output, options, and NullLogger
        ContentBasedRouter router = null!; // ← replace with new ContentBasedRouter(...)

        // TODO: Route four messages through the router:
        //   "d1"/"svc"/"order.created", "d2"/"svc"/"order.shipped",
        //   "d3"/"svc"/"payment.received", "d4"/"svc"/"inventory.updated"
        _ = router; // suppress unused-variable warning — remove after filling in RouteAsync calls above

        output.AssertReceivedCount(4);
        output.AssertReceivedOnTopic("orders", 2);
        output.AssertReceivedOnTopic("payments", 1);
        output.AssertReceivedOnTopic("other", 1);
        Assert.That(output.GetReceivedTopics(), Has.Count.EqualTo(3));
    }
}
#endif
