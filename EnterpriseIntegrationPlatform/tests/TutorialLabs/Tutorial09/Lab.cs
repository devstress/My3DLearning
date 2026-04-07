// ============================================================================
// Tutorial 09 – Content-Based Router (Lab)
// ============================================================================
// EIP Pattern: Content-Based Router
// End-to-End: Wire real ContentBasedRouter with MockEndpoint as producer,
// configure routing rules (Equals, Contains, StartsWith, Regex), verify
// delivery to correct topics, priority ordering, default fallback, and
// matched rule metadata accessibility.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial09;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("router-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    // ── 1. Routing Operators ────────────────────────────────────────────

    [Test]
    public async Task Route_Equals_MatchesMessageType()
    {
        // Equals operator: case-insensitive exact match on the field value.
        var router = CreateRouter(new RoutingRule
        {
            Priority = 1, Name = "OrderRule",
            FieldName = "MessageType", Operator = RoutingOperator.Equals,
            Value = "order.created", TargetTopic = "orders-topic",
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders-topic"));
        Assert.That(decision.IsDefault, Is.False);
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("OrderRule"));
        _output.AssertReceivedOnTopic("orders-topic", 1);
    }

    [Test]
    public async Task Route_Contains_MatchesMetadataSubstring()
    {
        // Contains operator: substring match in the field value.
        // FieldName "Metadata.region" reads the "region" key from Metadata.
        var router = CreateRouter(new RoutingRule
        {
            Priority = 1, Name = "EuropeRegion",
            FieldName = "Metadata.region", Operator = RoutingOperator.Contains,
            Value = "europe", TargetTopic = "eu-topic",
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "eu-data", "RegionalService", "data.regional") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "western-europe-1" },
        };
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("eu-topic"));
        _output.AssertReceivedOnTopic("eu-topic", 1);
    }

    [Test]
    public async Task Route_StartsWith_MatchesSourcePrefix()
    {
        // StartsWith operator: prefix match on the field value.
        var router = CreateRouter(new RoutingRule
        {
            Priority = 1, Name = "InternalRule",
            FieldName = "Source", Operator = RoutingOperator.StartsWith,
            Value = "Internal", TargetTopic = "internal-topic",
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "InternalOrderService", "order.event");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("internal-topic"));
        _output.AssertReceivedOnTopic("internal-topic", 1);
    }

    [Test]
    public async Task Route_Regex_MatchesPattern()
    {
        // Regex operator: compiled, case-insensitive, 1-second timeout.
        // Matches any MessageType starting with "order." followed by characters.
        var router = CreateRouter(new RoutingRule
        {
            Priority = 1, Name = "AllOrders",
            FieldName = "MessageType", Operator = RoutingOperator.Regex,
            Value = @"^order\..+", TargetTopic = "order-events",
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "shipped", "OrderService", "order.shipped");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("order-events"));
        _output.AssertReceivedOnTopic("order-events", 1);
    }

    // ── 2. Default Fallback & Priority ──────────────────────────────────

    [Test]
    public async Task Route_NoMatch_FallsToDefaultTopic()
    {
        // When no rule matches, the router uses DefaultTopic.
        // RoutingDecision.IsDefault = true, MatchedRule = null.
        var router = CreateRouter(new RoutingRule
        {
            Priority = 1,
            FieldName = "MessageType", Operator = RoutingOperator.Equals,
            Value = "order.created", TargetTopic = "orders-topic",
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "unknown", "UnknownService", "unknown.event");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("catch-all"));
        Assert.That(decision.IsDefault, Is.True);
        Assert.That(decision.MatchedRule, Is.Null);
        _output.AssertReceivedOnTopic("catch-all", 1);
    }

    [Test]
    public async Task Route_Priority_LowerNumberEvaluatedFirst()
    {
        // Rules are evaluated in Priority order (ascending).
        // The first match wins — lower number = higher priority.
        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 10, Name = "Broad",
                    FieldName = "MessageType", Operator = RoutingOperator.Contains,
                    Value = "order", TargetTopic = "general-orders",
                },
                new RoutingRule
                {
                    Priority = 1, Name = "Specific",
                    FieldName = "MessageType", Operator = RoutingOperator.Equals,
                    Value = "order.created", TargetTopic = "new-orders",
                },
            ],
            DefaultTopic = "unmatched",
        });
        var router = new ContentBasedRouter(
            _output, options, NullLogger<ContentBasedRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "new-order", "OrderService", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("new-orders"));
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("Specific"));
        _output.AssertReceivedOnTopic("new-orders", 1);
    }

    // ── 3. RoutingDecision Metadata ─────────────────────────────────────

    [Test]
    public async Task Route_MatchedRule_ContainsAllRuleDetails()
    {
        // RoutingDecision.MatchedRule exposes the full rule that triggered
        // the routing — useful for logging and audit trails.
        var router = CreateRouter(new RoutingRule
        {
            Priority = 5, Name = "CriticalSource",
            FieldName = "Source", Operator = RoutingOperator.Equals,
            Value = "CriticalService", TargetTopic = "critical-topic",
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "alert", "CriticalService", "alert.fired");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.MatchedRule!.Priority, Is.EqualTo(5));
        Assert.That(decision.MatchedRule.FieldName, Is.EqualTo("Source"));
        Assert.That(decision.MatchedRule.Operator, Is.EqualTo(RoutingOperator.Equals));
        Assert.That(decision.MatchedRule.TargetTopic, Is.EqualTo("critical-topic"));
    }

    private ContentBasedRouter CreateRouter(RoutingRule rule)
    {
        var options = Options.Create(new RouterOptions
        {
            Rules = [rule],
            DefaultTopic = "catch-all",
        });
        return new ContentBasedRouter(
            _output, options, NullLogger<ContentBasedRouter>.Instance);
    }
}
