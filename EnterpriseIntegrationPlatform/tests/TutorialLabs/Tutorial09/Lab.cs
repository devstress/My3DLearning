// ============================================================================
// Tutorial 09 – Content-Based Router (Lab)
// ============================================================================
// EIP Pattern: Content-Based Router.
// E2E: Wire real ContentBasedRouter with MockEndpoint as producer, configure
// routing rules, send messages, verify delivery to correct topics.
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

    [Test]
    public async Task Route_Equals_MatchesMessageType()
    {
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
    public async Task Route_Contains_MatchesMetadata()
    {
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
    public async Task Route_StartsWith_MatchesSource()
    {
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

    [Test]
    public async Task Route_NoMatch_FallsToDefault()
    {
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
    public async Task Route_Priority_LowerNumberWins()
    {
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

    [Test]
    public async Task Route_MatchedRule_ContainsAllDetails()
    {
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
