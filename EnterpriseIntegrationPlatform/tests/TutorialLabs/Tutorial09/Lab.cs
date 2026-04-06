// ============================================================================
// Tutorial 09 – Content-Based Router (Lab)
// ============================================================================
// This lab exercises the ContentBasedRouter with various RoutingRules and
// operators.  You will configure rules for MessageType, Metadata, and Regex
// matching, then verify the RoutingDecision for each scenario.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial09;

[TestFixture]
public sealed class Lab
{
    // ── Routing by MessageType (Equals Operator) ────────────────────────────

    [Test]
    public async Task Route_ByMessageType_Equals_MatchesCorrectTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "order.created",
                    TargetTopic = "orders-topic",
                    Name = "OrderCreated",
                },
                new RoutingRule
                {
                    Priority = 2,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "payment.received",
                    TargetTopic = "payments-topic",
                    Name = "PaymentReceived",
                },
            ],
            DefaultTopic = "unmatched-topic",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders-topic"));
        Assert.That(decision.IsDefault, Is.False);
        Assert.That(decision.MatchedRule, Is.Not.Null);
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("OrderCreated"));
    }

    [Test]
    public async Task Route_ByMessageType_SecondRuleMatches()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "order.created",
                    TargetTopic = "orders-topic",
                },
                new RoutingRule
                {
                    Priority = 2,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "payment.received",
                    TargetTopic = "payments-topic",
                    Name = "PaymentRule",
                },
            ],
            DefaultTopic = "unmatched-topic",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "payment-data", "PaymentService", "payment.received");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("payments-topic"));
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("PaymentRule"));
    }

    // ── Routing by Metadata Field (Contains Operator) ───────────────────────

    [Test]
    public async Task Route_ByMetadata_Contains_MatchesMetadataValue()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "Metadata.region",
                    Operator = RoutingOperator.Contains,
                    Value = "europe",
                    TargetTopic = "eu-topic",
                    Name = "EuropeRegion",
                },
            ],
            DefaultTopic = "global-topic",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "eu-data", "RegionalService", "data.regional") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "western-europe-1",
            },
        };

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("eu-topic"));
        Assert.That(decision.IsDefault, Is.False);
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("EuropeRegion"));
    }

    // ── Routing with Regex Operator ─────────────────────────────────────────

    [Test]
    public async Task Route_ByMessageType_Regex_MatchesPattern()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Regex,
                    Value = @"^order\..+",
                    TargetTopic = "order-events",
                    Name = "AllOrderEvents",
                },
            ],
            DefaultTopic = "other-events",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        // "order.shipped" matches the pattern ^order\..+
        var envelope = IntegrationEnvelope<string>.Create(
            "shipped-data", "OrderService", "order.shipped");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("order-events"));
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("AllOrderEvents"));
    }

    [Test]
    public async Task Route_ByMessageType_Regex_NoMatch_UsesDefault()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Regex,
                    Value = @"^order\..+",
                    TargetTopic = "order-events",
                },
            ],
            DefaultTopic = "other-events",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        // "payment.received" does NOT match ^order\..+
        var envelope = IntegrationEnvelope<string>.Create(
            "payment-data", "PaymentService", "payment.received");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("other-events"));
        Assert.That(decision.IsDefault, Is.True);
        Assert.That(decision.MatchedRule, Is.Null);
    }

    // ── Default Topic Fallback ──────────────────────────────────────────────

    [Test]
    public async Task Route_NoRuleMatches_FallsBackToDefaultTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "order.created",
                    TargetTopic = "orders-topic",
                },
            ],
            DefaultTopic = "catch-all-topic",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        // This message type doesn't match any rule.
        var envelope = IntegrationEnvelope<string>.Create(
            "unknown-data", "UnknownService", "unknown.event");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("catch-all-topic"));
        Assert.That(decision.IsDefault, Is.True);
        Assert.That(decision.MatchedRule, Is.Null);
    }

    // ── Verify RoutingDecision Contains Correct MatchedRule ──────────────────

    [Test]
    public async Task Route_MatchedRule_ContainsAllRuleDetails()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 10,
                    FieldName = "Source",
                    Operator = RoutingOperator.Equals,
                    Value = "CriticalService",
                    TargetTopic = "critical-topic",
                    Name = "CriticalSource",
                },
            ],
            DefaultTopic = "default-topic",
        });

        var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "critical-payload", "CriticalService", "alert.triggered");

        var decision = await router.RouteAsync(envelope);

        Assert.That(decision.MatchedRule, Is.Not.Null);
        Assert.That(decision.MatchedRule!.Priority, Is.EqualTo(10));
        Assert.That(decision.MatchedRule.FieldName, Is.EqualTo("Source"));
        Assert.That(decision.MatchedRule.Operator, Is.EqualTo(RoutingOperator.Equals));
        Assert.That(decision.MatchedRule.Value, Is.EqualTo("CriticalService"));
        Assert.That(decision.MatchedRule.TargetTopic, Is.EqualTo("critical-topic"));
        Assert.That(decision.MatchedRule.Name, Is.EqualTo("CriticalSource"));
    }
}
