// ============================================================================
// Tutorial 09 – Content-Based Router (Exam)
// ============================================================================
// Coding challenges: build a multi-rule e-commerce routing table, test
// priority-based rule evaluation, and implement payload-based routing.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial09;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: E-Commerce Regional Routing ────────────────────────────

    [Test]
    public async Task Challenge1_EcommerceRouting_OrdersByRegion()
    {
        // Build a multi-rule routing table for an e-commerce platform.
        // Orders are routed to regional fulfilment topics based on
        // the "region" metadata key.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "Metadata.region",
                    Operator = RoutingOperator.Equals,
                    Value = "us-east",
                    TargetTopic = "fulfilment.us-east",
                    Name = "US-East",
                },
                new RoutingRule
                {
                    Priority = 2,
                    FieldName = "Metadata.region",
                    Operator = RoutingOperator.Equals,
                    Value = "eu-west",
                    TargetTopic = "fulfilment.eu-west",
                    Name = "EU-West",
                },
                new RoutingRule
                {
                    Priority = 3,
                    FieldName = "Metadata.region",
                    Operator = RoutingOperator.Equals,
                    Value = "ap-southeast",
                    TargetTopic = "fulfilment.ap-southeast",
                    Name = "AP-Southeast",
                },
            ],
            DefaultTopic = "fulfilment.global",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        // US-East order.
        var usOrder = IntegrationEnvelope<string>.Create(
            "order-us", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "us-east" },
        };

        var usDecision = await router.RouteAsync(usOrder);
        Assert.That(usDecision.TargetTopic, Is.EqualTo("fulfilment.us-east"));
        Assert.That(usDecision.MatchedRule!.Name, Is.EqualTo("US-East"));

        // EU-West order.
        var euOrder = IntegrationEnvelope<string>.Create(
            "order-eu", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "eu-west" },
        };

        var euDecision = await router.RouteAsync(euOrder);
        Assert.That(euDecision.TargetTopic, Is.EqualTo("fulfilment.eu-west"));

        // Unknown region → global fallback.
        var unknownOrder = IntegrationEnvelope<string>.Create(
            "order-unknown", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "af-south" },
        };

        var unknownDecision = await router.RouteAsync(unknownOrder);
        Assert.That(unknownDecision.TargetTopic, Is.EqualTo("fulfilment.global"));
        Assert.That(unknownDecision.IsDefault, Is.True);
    }

    // ── Challenge 2: Priority-Based Routing (Lower Number Wins) ─────────────

    [Test]
    public async Task Challenge2_PriorityRouting_LowerPriorityWins()
    {
        // When multiple rules match, the rule with the LOWEST Priority number
        // should win (first-match after sorting by priority).
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                // Priority 10 — broad match.
                new RoutingRule
                {
                    Priority = 10,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Contains,
                    Value = "order",
                    TargetTopic = "general-orders",
                    Name = "BroadOrderRule",
                },
                // Priority 1 — specific match (should win).
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "order.created",
                    TargetTopic = "new-orders",
                    Name = "SpecificOrderRule",
                },
            ],
            DefaultTopic = "unmatched",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "new-order", "OrderService", "order.created");

        // Both rules match, but Priority 1 wins.
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("new-orders"));
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("SpecificOrderRule"));
        Assert.That(decision.MatchedRule.Priority, Is.EqualTo(1));
    }

    // ── Challenge 3: Payload-Based Routing with JsonElement ──────────────────

    [Test]
    public async Task Challenge3_PayloadRouting_ByJsonField()
    {
        // Route messages based on a field inside the JSON payload.
        // The Payload.{path} field extraction requires the payload to be a JsonElement.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "Payload.status",
                    Operator = RoutingOperator.Equals,
                    Value = "urgent",
                    TargetTopic = "urgent-processing",
                    Name = "UrgentStatus",
                },
                new RoutingRule
                {
                    Priority = 2,
                    FieldName = "Payload.status",
                    Operator = RoutingOperator.Equals,
                    Value = "normal",
                    TargetTopic = "normal-processing",
                    Name = "NormalStatus",
                },
            ],
            DefaultTopic = "default-processing",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        // Create a JsonElement payload (required for Payload.{path} extraction).
        var urgentJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"orderId\": \"ORD-1\", \"status\": \"urgent\", \"amount\": 5000}");

        var urgentEnvelope = IntegrationEnvelope<JsonElement>.Create(
            urgentJson, "OrderService", "order.submitted");

        var urgentDecision = await router.RouteAsync(urgentEnvelope);
        Assert.That(urgentDecision.TargetTopic, Is.EqualTo("urgent-processing"));
        Assert.That(urgentDecision.MatchedRule!.Name, Is.EqualTo("UrgentStatus"));

        // Normal status order.
        var normalJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"orderId\": \"ORD-2\", \"status\": \"normal\", \"amount\": 50}");

        var normalEnvelope = IntegrationEnvelope<JsonElement>.Create(
            normalJson, "OrderService", "order.submitted");

        var normalDecision = await router.RouteAsync(normalEnvelope);
        Assert.That(normalDecision.TargetTopic, Is.EqualTo("normal-processing"));
        Assert.That(normalDecision.MatchedRule!.Name, Is.EqualTo("NormalStatus"));

        // Unknown status → default topic.
        var unknownJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"orderId\": \"ORD-3\", \"status\": \"backorder\", \"amount\": 10}");

        var unknownEnvelope = IntegrationEnvelope<JsonElement>.Create(
            unknownJson, "OrderService", "order.submitted");

        var unknownDecision = await router.RouteAsync(unknownEnvelope);
        Assert.That(unknownDecision.TargetTopic, Is.EqualTo("default-processing"));
        Assert.That(unknownDecision.IsDefault, Is.True);
    }
}
