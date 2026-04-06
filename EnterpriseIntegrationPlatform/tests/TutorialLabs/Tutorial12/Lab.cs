// ============================================================================
// Tutorial 12 – Recipient List (Lab)
// ============================================================================
// This lab exercises the RecipientListRouter — a pattern that fans out a single
// message to multiple destinations based on matching rules and metadata-based
// recipient resolution. You will configure rules, verify deduplication, and
// confirm that all resolved recipients receive the message.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial12;

[TestFixture]
public sealed class Lab
{
    // ── Single Rule Matches — Fan-out to Multiple Destinations ──────────────

    [Test]
    public async Task Route_SingleRuleMatches_PublishesToAllDestinations()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "order.created",
                    Destinations = ["audit-topic", "analytics-topic", "fulfilment-topic"],
                    Name = "OrderFanOut",
                },
            ],
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");

        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.Destinations, Contains.Item("audit-topic"));
        Assert.That(result.Destinations, Contains.Item("analytics-topic"));
        Assert.That(result.Destinations, Contains.Item("fulfilment-topic"));
    }

    // ── Multiple Rules Match — Destinations Are Merged ──────────────────────

    [Test]
    public async Task Route_MultipleRulesMatch_MergesAllDestinations()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Contains,
                    Value = "order",
                    Destinations = ["audit-topic"],
                    Name = "AuditAll",
                },
                new RecipientListRule
                {
                    FieldName = "Source",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderService",
                    Destinations = ["order-analytics"],
                    Name = "OrderAnalytics",
                },
            ],
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");

        var result = await router.RouteAsync(envelope);

        // Both rules match → destinations are merged.
        Assert.That(result.ResolvedCount, Is.EqualTo(2));
        Assert.That(result.Destinations, Contains.Item("audit-topic"));
        Assert.That(result.Destinations, Contains.Item("order-analytics"));
    }

    // ── Duplicate Destinations Are Removed ──────────────────────────────────

    [Test]
    public async Task Route_DuplicateDestinations_AreDeduplicated()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Contains,
                    Value = "order",
                    Destinations = ["audit-topic", "analytics-topic"],
                },
                new RecipientListRule
                {
                    FieldName = "Source",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderService",
                    Destinations = ["audit-topic", "fulfilment-topic"],
                },
            ],
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created");

        var result = await router.RouteAsync(envelope);

        // "audit-topic" appears in both rules but should be deduplicated.
        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.DuplicatesRemoved, Is.EqualTo(1));
        Assert.That(result.Destinations, Contains.Item("audit-topic"));
        Assert.That(result.Destinations, Contains.Item("analytics-topic"));
        Assert.That(result.Destinations, Contains.Item("fulfilment-topic"));
    }

    // ── No Rule Matches — Empty Result ──────────────────────────────────────

    [Test]
    public async Task Route_NoRuleMatches_ReturnsEmptyDestinations()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "order.created",
                    Destinations = ["orders-topic"],
                },
            ],
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        // This message type doesn't match any rule.
        var envelope = IntegrationEnvelope<string>.Create(
            "payment-data", "PaymentService", "payment.received");

        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(0));
        Assert.That(result.Destinations, Is.Empty);
    }

    // ── Metadata-Based Recipient Resolution ─────────────────────────────────

    [Test]
    public async Task Route_MetadataRecipients_AddsExtraDestinations()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules = [],
            MetadataRecipientsKey = "recipients",
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        // Destinations specified in the envelope metadata.
        var envelope = IntegrationEnvelope<string>.Create(
            "data", "Service", "event.occurred") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["recipients"] = "topic-a,topic-b,topic-c",
            },
        };

        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.Destinations, Contains.Item("topic-a"));
        Assert.That(result.Destinations, Contains.Item("topic-b"));
        Assert.That(result.Destinations, Contains.Item("topic-c"));
    }

    // ── StartsWith Operator ─────────────────────────────────────────────────

    [Test]
    public async Task Route_StartsWithOperator_MatchesPrefixes()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.StartsWith,
                    Value = "order.",
                    Destinations = ["order-events-topic"],
                    Name = "AllOrderEvents",
                },
            ],
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "OrderService", "order.shipped");

        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(1));
        Assert.That(result.Destinations, Contains.Item("order-events-topic"));
    }

    // ── Verify Producer Receives All Publish Calls ──────────────────────────

    [Test]
    public async Task Route_PublishCalledForEachDestination()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "order.created",
                    Destinations = ["topic-a", "topic-b"],
                },
            ],
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "OrderService", "order.created");

        await router.RouteAsync(envelope);

        // Verify publish was called for each destination.
        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("topic-a"),
            Arg.Any<CancellationToken>());

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("topic-b"),
            Arg.Any<CancellationToken>());
    }
}
