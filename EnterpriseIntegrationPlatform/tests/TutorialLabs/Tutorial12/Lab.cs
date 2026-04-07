// ============================================================================
// Tutorial 12 – Recipient List (Lab)
// ============================================================================
// EIP Pattern: Recipient List
// E2E: Wire real RecipientListRouter with MockEndpoint as producer, configure
// fan-out rules, send messages, verify delivery to multiple destinations.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial12;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("recipient-list-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Basic Fan-Out ─────────────────────────────────────────────

    [Test]
    public async Task Route_SingleRuleMatch_FansOutToAllDestinations()
    {
        var router = CreateRouter(new RecipientListRule
        {
            Name = "OrderEvents",
            FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "order.created",
            Destinations = ["orders-topic", "audit-topic", "analytics-topic"],
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.Destinations, Has.Count.EqualTo(3));
        Assert.That(result.DuplicatesRemoved, Is.EqualTo(0));
        _output.AssertReceivedCount(3);
        _output.AssertReceivedOnTopic("orders-topic", 1);
        _output.AssertReceivedOnTopic("audit-topic", 1);
        _output.AssertReceivedOnTopic("analytics-topic", 1);
    }

    [Test]
    public async Task Route_NoRuleMatch_ReturnsEmptyResult()
    {
        var router = CreateRouter(new RecipientListRule
        {
            Name = "OrderEvents",
            FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "order.created",
            Destinations = ["orders-topic"],
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "payment.received");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(0));
        Assert.That(result.Destinations, Is.Empty);
        _output.AssertNoneReceived();
    }


    // ── 2. Multi-Rule & Deduplication ────────────────────────────────

    [Test]
    public async Task Route_MultipleRulesMatch_CombinesDestinations()
    {
        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    Name = "TypeRule",
                    FieldName = "MessageType",
                    Operator = RoutingOperator.StartsWith,
                    Value = "order",
                    Destinations = ["orders-topic"],
                },
                new RecipientListRule
                {
                    Name = "SourceRule",
                    FieldName = "Source",
                    Operator = RoutingOperator.Contains,
                    Value = "Critical",
                    Destinations = ["alert-topic"],
                },
            ],
        });
        var router = new RecipientListRouter(
            _output, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "CriticalOrderService", "order.created");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(2));
        _output.AssertReceivedOnTopic("orders-topic", 1);
        _output.AssertReceivedOnTopic("alert-topic", 1);
    }

    [Test]
    public async Task Route_DuplicateDestinations_AreDeduplicated()
    {
        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    Name = "Rule1",
                    FieldName = "MessageType",
                    Operator = RoutingOperator.StartsWith,
                    Value = "order",
                    Destinations = ["shared-topic", "orders-topic"],
                },
                new RecipientListRule
                {
                    Name = "Rule2",
                    FieldName = "Source",
                    Operator = RoutingOperator.Contains,
                    Value = "Service",
                    Destinations = ["shared-topic", "audit-topic"],
                },
            ],
        });
        var router = new RecipientListRouter(
            _output, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "OrderService", "order.created");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.DuplicatesRemoved, Is.GreaterThan(0));
        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        _output.AssertReceivedCount(3);
    }


    // ── 3. Advanced Recipient Sources ────────────────────────────────

    [Test]
    public async Task Route_MetadataRecipients_AddsDestinations()
    {
        var options = Options.Create(new RecipientListOptions
        {
            Rules = [],
            MetadataRecipientsKey = "recipients",
        });
        var router = new RecipientListRouter(
            _output, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "event.fired") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["recipients"] = "topic-a,topic-b,topic-c",
            },
        };
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        _output.AssertReceivedOnTopic("topic-a", 1);
        _output.AssertReceivedOnTopic("topic-b", 1);
        _output.AssertReceivedOnTopic("topic-c", 1);
    }

    [Test]
    public async Task Route_RegexRule_MatchesPattern()
    {
        var router = CreateRouter(new RecipientListRule
        {
            Name = "AllOrders",
            FieldName = "MessageType",
            Operator = RoutingOperator.Regex,
            Value = @"^order\..+",
            Destinations = ["order-events"],
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "shipped", "Svc", "order.shipped");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(1));
        _output.AssertReceivedOnTopic("order-events", 1);
    }

    private RecipientListRouter CreateRouter(RecipientListRule rule)
    {
        var options = Options.Create(new RecipientListOptions
        {
            Rules = [rule],
        });
        return new RecipientListRouter(
            _output, options, NullLogger<RecipientListRouter>.Instance);
    }
}
