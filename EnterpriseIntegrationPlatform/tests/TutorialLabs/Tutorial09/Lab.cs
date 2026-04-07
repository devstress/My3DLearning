// ============================================================================
// Tutorial 09 – Content-Based Router (Lab)
// ============================================================================
// EIP Pattern: Content-Based Router
// Real Integrations: Wire real ContentBasedRouter with NatsBrokerEndpoint
// (real NATS JetStream via Aspire) as producer. Configure routing rules
// (Equals, Contains, StartsWith, Regex), verify delivery to correct topics,
// priority ordering, default fallback, and matched rule metadata.
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
    // ── 1. Routing Operators (Real NATS) ────────────────────────────────

    [Test]
    public async Task Route_Equals_MatchesMessageType()
    {
        // Equals operator: case-insensitive exact match on the field value.
        await using var nats = AspireFixture.CreateNatsEndpoint("t09-eq");
        var targetTopic = AspireFixture.UniqueTopic("t09-orders");

        var router = CreateRouter(nats, new RoutingRule
        {
            Priority = 1, Name = "OrderRule",
            FieldName = "MessageType", Operator = RoutingOperator.Equals,
            Value = "order.created", TargetTopic = targetTopic,
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo(targetTopic));
        Assert.That(decision.IsDefault, Is.False);
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("OrderRule"));
        nats.AssertReceivedOnTopic(targetTopic, 1);
    }

    [Test]
    public async Task Route_Contains_MatchesMetadataSubstring()
    {
        // Contains operator: substring match in the field value.
        await using var nats = AspireFixture.CreateNatsEndpoint("t09-contains");
        var targetTopic = AspireFixture.UniqueTopic("t09-eu");

        var router = CreateRouter(nats, new RoutingRule
        {
            Priority = 1, Name = "EuropeRegion",
            FieldName = "Metadata.region", Operator = RoutingOperator.Contains,
            Value = "europe", TargetTopic = targetTopic,
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "eu-data", "RegionalService", "data.regional") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "western-europe-1" },
        };
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo(targetTopic));
        nats.AssertReceivedOnTopic(targetTopic, 1);
    }

    [Test]
    public async Task Route_StartsWith_MatchesSourcePrefix()
    {
        // StartsWith operator: prefix match on the field value.
        await using var nats = AspireFixture.CreateNatsEndpoint("t09-starts");
        var targetTopic = AspireFixture.UniqueTopic("t09-internal");

        var router = CreateRouter(nats, new RoutingRule
        {
            Priority = 1, Name = "InternalRule",
            FieldName = "Source", Operator = RoutingOperator.StartsWith,
            Value = "Internal", TargetTopic = targetTopic,
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "InternalOrderService", "order.event");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo(targetTopic));
        nats.AssertReceivedOnTopic(targetTopic, 1);
    }

    [Test]
    public async Task Route_Regex_MatchesPattern()
    {
        // Regex operator: compiled, case-insensitive, 1-second timeout.
        await using var nats = AspireFixture.CreateNatsEndpoint("t09-regex");
        var targetTopic = AspireFixture.UniqueTopic("t09-orderevts");

        var router = CreateRouter(nats, new RoutingRule
        {
            Priority = 1, Name = "AllOrders",
            FieldName = "MessageType", Operator = RoutingOperator.Regex,
            Value = @"^order\..+", TargetTopic = targetTopic,
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "shipped", "OrderService", "order.shipped");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo(targetTopic));
        nats.AssertReceivedOnTopic(targetTopic, 1);
    }

    // ── 2. Default Fallback & Priority (Real NATS) ──────────────────────

    [Test]
    public async Task Route_NoMatch_FallsToDefaultTopic()
    {
        // When no rule matches, the router uses DefaultTopic.
        await using var nats = AspireFixture.CreateNatsEndpoint("t09-default");
        var defaultTopic = AspireFixture.UniqueTopic("t09-catchall");

        var router = CreateRouter(nats, new RoutingRule
        {
            Priority = 1,
            FieldName = "MessageType", Operator = RoutingOperator.Equals,
            Value = "order.created", TargetTopic = "orders-topic",
        }, defaultTopic);

        var envelope = IntegrationEnvelope<string>.Create(
            "unknown", "UnknownService", "unknown.event");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo(defaultTopic));
        Assert.That(decision.IsDefault, Is.True);
        Assert.That(decision.MatchedRule, Is.Null);
        nats.AssertReceivedOnTopic(defaultTopic, 1);
    }

    [Test]
    public async Task Route_Priority_LowerNumberEvaluatedFirst()
    {
        // Rules are evaluated in Priority order (ascending).
        await using var nats = AspireFixture.CreateNatsEndpoint("t09-prio");
        var specificTopic = AspireFixture.UniqueTopic("t09-neworders");
        var generalTopic = AspireFixture.UniqueTopic("t09-general");

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 10, Name = "Broad",
                    FieldName = "MessageType", Operator = RoutingOperator.Contains,
                    Value = "order", TargetTopic = generalTopic,
                },
                new RoutingRule
                {
                    Priority = 1, Name = "Specific",
                    FieldName = "MessageType", Operator = RoutingOperator.Equals,
                    Value = "order.created", TargetTopic = specificTopic,
                },
            ],
            DefaultTopic = "unmatched",
        });
        var router = new ContentBasedRouter(
            nats, options, NullLogger<ContentBasedRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "new-order", "OrderService", "order.created");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo(specificTopic));
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("Specific"));
        nats.AssertReceivedOnTopic(specificTopic, 1);
    }

    // ── 3. RoutingDecision Metadata (Real NATS) ─────────────────────────

    [Test]
    public async Task Route_MatchedRule_ContainsAllRuleDetails()
    {
        // RoutingDecision.MatchedRule exposes the full rule that triggered
        // the routing — useful for logging and audit trails.
        await using var nats = AspireFixture.CreateNatsEndpoint("t09-meta");
        var targetTopic = AspireFixture.UniqueTopic("t09-critical");

        var router = CreateRouter(nats, new RoutingRule
        {
            Priority = 5, Name = "CriticalSource",
            FieldName = "Source", Operator = RoutingOperator.Equals,
            Value = "CriticalService", TargetTopic = targetTopic,
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "alert", "CriticalService", "alert.fired");
        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.MatchedRule!.Priority, Is.EqualTo(5));
        Assert.That(decision.MatchedRule.FieldName, Is.EqualTo("Source"));
        Assert.That(decision.MatchedRule.Operator, Is.EqualTo(RoutingOperator.Equals));
        Assert.That(decision.MatchedRule.TargetTopic, Is.EqualTo(targetTopic));
    }

    private static ContentBasedRouter CreateRouter(
        NatsBrokerEndpoint nats, RoutingRule rule, string defaultTopic = "catch-all")
    {
        var options = Options.Create(new RouterOptions
        {
            Rules = [rule],
            DefaultTopic = defaultTopic,
        });
        return new ContentBasedRouter(
            nats, options, NullLogger<ContentBasedRouter>.Instance);
    }
}
