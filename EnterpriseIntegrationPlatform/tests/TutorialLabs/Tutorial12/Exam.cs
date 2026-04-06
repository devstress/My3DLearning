// ============================================================================
// Tutorial 12 – Recipient List (Exam)
// ============================================================================
// Coding challenges: build an event notification system, combine rule-based
// and metadata-based recipient resolution, and handle cross-rule dedup.
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
public sealed class Exam
{
    // ── Challenge 1: Event Notification Fan-Out ─────────────────────────────

    [Test]
    public async Task Challenge1_EventNotification_FansOutToAllSubscribers()
    {
        // Build a recipient list that routes order events to three departments:
        //   - Warehouse (fulfilment-topic)
        //   - Finance (billing-topic)
        //   - Analytics (analytics-topic)
        // All three should receive a copy of every order.created message.
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
                    Destinations = ["fulfilment-topic", "billing-topic", "analytics-topic"],
                    Name = "OrderNotification",
                },
            ],
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "new-order", "OrderService", "order.created");

        var result = await router.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.Destinations, Contains.Item("fulfilment-topic"));
        Assert.That(result.Destinations, Contains.Item("billing-topic"));
        Assert.That(result.Destinations, Contains.Item("analytics-topic"));

        // Non-order message should not match.
        var paymentEnvelope = IntegrationEnvelope<string>.Create(
            "payment-data", "PaymentService", "payment.received");

        var paymentResult = await router.RouteAsync(paymentEnvelope);
        Assert.That(paymentResult.ResolvedCount, Is.EqualTo(0));
    }

    // ── Challenge 2: Rule + Metadata Combined Resolution ────────────────────

    [Test]
    public async Task Challenge2_RuleAndMetadataCombined_AllDestinationsReached()
    {
        // Combine rule-based routing (audit-topic for all messages from OrderService)
        // with metadata-based routing (extra destinations in the "notify" key).
        // Verify that all destinations — from rules AND metadata — are resolved
        // and deduplicated.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "Source",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderService",
                    Destinations = ["audit-topic", "compliance-topic"],
                    Name = "OrderAudit",
                },
            ],
            MetadataRecipientsKey = "notify",
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string>
            {
                // Extra recipients from metadata — "audit-topic" is a duplicate.
                ["notify"] = "analytics-topic,audit-topic",
            },
        };

        var result = await router.RouteAsync(envelope);

        // Rule: audit-topic, compliance-topic. Metadata: analytics-topic, audit-topic.
        // Deduplication removes one "audit-topic".
        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.DuplicatesRemoved, Is.EqualTo(1));
        Assert.That(result.Destinations, Contains.Item("audit-topic"));
        Assert.That(result.Destinations, Contains.Item("compliance-topic"));
        Assert.That(result.Destinations, Contains.Item("analytics-topic"));
    }

    // ── Challenge 3: Regex-Based Recipient Matching ─────────────────────────

    [Test]
    public async Task Challenge3_RegexRouting_MatchesPatternBasedDestinations()
    {
        // Use the Regex operator to route all "order.*" message types to one set
        // of recipients and all "payment.*" types to another.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Regex,
                    Value = @"^order\..+",
                    Destinations = ["order-audit", "order-analytics"],
                    Name = "AllOrderEvents",
                },
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Regex,
                    Value = @"^payment\..+",
                    Destinations = ["payment-audit", "payment-ledger"],
                    Name = "AllPaymentEvents",
                },
            ],
        });

        var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

        // An order message.
        var orderEnvelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.shipped");

        var orderResult = await router.RouteAsync(orderEnvelope);
        Assert.That(orderResult.ResolvedCount, Is.EqualTo(2));
        Assert.That(orderResult.Destinations, Contains.Item("order-audit"));
        Assert.That(orderResult.Destinations, Contains.Item("order-analytics"));

        // A payment message.
        var paymentEnvelope = IntegrationEnvelope<string>.Create(
            "payment-data", "PaymentService", "payment.confirmed");

        var paymentResult = await router.RouteAsync(paymentEnvelope);
        Assert.That(paymentResult.ResolvedCount, Is.EqualTo(2));
        Assert.That(paymentResult.Destinations, Contains.Item("payment-audit"));
        Assert.That(paymentResult.Destinations, Contains.Item("payment-ledger"));

        // A refund message matches neither.
        var refundEnvelope = IntegrationEnvelope<string>.Create(
            "refund-data", "RefundService", "refund.issued");

        var refundResult = await router.RouteAsync(refundEnvelope);
        Assert.That(refundResult.ResolvedCount, Is.EqualTo(0));
    }
}
