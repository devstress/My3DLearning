// ============================================================================
// Tutorial 12 – Recipient List (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Recipient List pattern
//          fans out messages to multiple destinations via real NATS.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Basic fan-out — single rule sends to three destinations
//   2. No match — unmatched envelope produces empty result
//   3. Multi-rule combination — two matching rules merge destination lists
//   4. Deduplication — overlapping destinations across rules are deduplicated
//   5. Metadata recipients — destinations resolved from envelope metadata key
//   6. Regex operator — pattern-based matching on message type
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
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
    // ── 1. Basic Fan-Out (Real NATS) ─────────────────────────────────

    [Test]
    public async Task Route_SingleRuleMatch_FansOutToAllDestinations()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t12-fanout");
        var ordersTopic = AspireFixture.UniqueTopic("t12-orders");
        var auditTopic = AspireFixture.UniqueTopic("t12-audit");
        var analyticsTopic = AspireFixture.UniqueTopic("t12-analytics");

        var router = CreateRouter(nats, new RecipientListRule
        {
            Name = "OrderEvents",
            FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "order.created",
            Destinations = [ordersTopic, auditTopic, analyticsTopic],
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.Destinations, Has.Count.EqualTo(3));
        Assert.That(result.DuplicatesRemoved, Is.EqualTo(0));
        nats.AssertReceivedCount(3);
        nats.AssertReceivedOnTopic(ordersTopic, 1);
        nats.AssertReceivedOnTopic(auditTopic, 1);
        nats.AssertReceivedOnTopic(analyticsTopic, 1);
    }

    [Test]
    public async Task Route_NoRuleMatch_ReturnsEmptyResult()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t12-nomatch");

        var router = CreateRouter(nats, new RecipientListRule
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
        nats.AssertNoneReceived();
    }


    // ── 2. Multi-Rule & Deduplication (Real NATS) ────────────────────

    [Test]
    public async Task Route_MultipleRulesMatch_CombinesDestinations()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t12-multi");
        var ordersTopic = AspireFixture.UniqueTopic("t12-ord");
        var alertTopic = AspireFixture.UniqueTopic("t12-alert");

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
                    Destinations = [ordersTopic],
                },
                new RecipientListRule
                {
                    Name = "SourceRule",
                    FieldName = "Source",
                    Operator = RoutingOperator.Contains,
                    Value = "Critical",
                    Destinations = [alertTopic],
                },
            ],
        });
        var router = new RecipientListRouter(
            nats, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "CriticalOrderService", "order.created");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(2));
        nats.AssertReceivedOnTopic(ordersTopic, 1);
        nats.AssertReceivedOnTopic(alertTopic, 1);
    }

    [Test]
    public async Task Route_DuplicateDestinations_AreDeduplicated()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t12-dedup");
        var sharedTopic = AspireFixture.UniqueTopic("t12-shared");
        var ordersTopic = AspireFixture.UniqueTopic("t12-ords");
        var auditTopic = AspireFixture.UniqueTopic("t12-aud");

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
                    Destinations = [sharedTopic, ordersTopic],
                },
                new RecipientListRule
                {
                    Name = "Rule2",
                    FieldName = "Source",
                    Operator = RoutingOperator.Contains,
                    Value = "Service",
                    Destinations = [sharedTopic, auditTopic],
                },
            ],
        });
        var router = new RecipientListRouter(
            nats, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "OrderService", "order.created");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.DuplicatesRemoved, Is.GreaterThan(0));
        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        nats.AssertReceivedCount(3);
    }


    // ── 3. Advanced Recipient Sources (Real NATS) ────────────────────

    [Test]
    public async Task Route_MetadataRecipients_AddsDestinations()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t12-meta");
        var topicA = AspireFixture.UniqueTopic("t12-a");
        var topicB = AspireFixture.UniqueTopic("t12-b");
        var topicC = AspireFixture.UniqueTopic("t12-c");

        var options = Options.Create(new RecipientListOptions
        {
            Rules = [],
            MetadataRecipientsKey = "recipients",
        });
        var router = new RecipientListRouter(
            nats, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Svc", "event.fired") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["recipients"] = $"{topicA},{topicB},{topicC}",
            },
        };
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        nats.AssertReceivedOnTopic(topicA, 1);
        nats.AssertReceivedOnTopic(topicB, 1);
        nats.AssertReceivedOnTopic(topicC, 1);
    }

    [Test]
    public async Task Route_RegexRule_MatchesPattern()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t12-regex");
        var targetTopic = AspireFixture.UniqueTopic("t12-oevt");

        var router = CreateRouter(nats, new RecipientListRule
        {
            Name = "AllOrders",
            FieldName = "MessageType",
            Operator = RoutingOperator.Regex,
            Value = @"^order\..+",
            Destinations = [targetTopic],
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "shipped", "Svc", "order.shipped");
        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(1));
        nats.AssertReceivedOnTopic(targetTopic, 1);
    }

    private static RecipientListRouter CreateRouter(
        NatsBrokerEndpoint nats, RecipientListRule rule)
    {
        var options = Options.Create(new RecipientListOptions
        {
            Rules = [rule],
        });
        return new RecipientListRouter(
            nats, options, NullLogger<RecipientListRouter>.Instance);
    }
}
