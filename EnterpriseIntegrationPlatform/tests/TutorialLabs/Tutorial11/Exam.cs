// ============================================================================
// Tutorial 11 – Dynamic Router (Exam)
// ============================================================================
// Coding challenges: build a self-registering microservice topology, test
// route replacement semantics, and verify control-channel thread-safety.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial11;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Multi-Service Dynamic Registration ─────────────────────

    [Test]
    public async Task Challenge1_MultiServiceRegistration_EachServiceGetsItsOwnRoute()
    {
        // Simulate three microservices registering their preferred message types.
        // After registration, route messages of each type and verify they reach
        // the correct destination.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "dead-letter",
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        // Three services register at runtime.
        await router.RegisterAsync("order.created", "orders-topic", "OrderService");
        await router.RegisterAsync("payment.received", "payments-topic", "PaymentService");
        await router.RegisterAsync("shipment.dispatched", "shipping-topic", "ShippingService");

        // Verify routing for each service.
        var orderEnvelope = IntegrationEnvelope<string>.Create(
            "order-1", "Gateway", "order.created");

        var orderDecision = await router.RouteAsync(orderEnvelope);
        Assert.That(orderDecision.Destination, Is.EqualTo("orders-topic"));
        Assert.That(orderDecision.MatchedEntry!.ParticipantId, Is.EqualTo("OrderService"));

        var paymentEnvelope = IntegrationEnvelope<string>.Create(
            "payment-1", "Gateway", "payment.received");

        var paymentDecision = await router.RouteAsync(paymentEnvelope);
        Assert.That(paymentDecision.Destination, Is.EqualTo("payments-topic"));
        Assert.That(paymentDecision.MatchedEntry!.ParticipantId, Is.EqualTo("PaymentService"));

        var shipmentEnvelope = IntegrationEnvelope<string>.Create(
            "shipment-1", "Gateway", "shipment.dispatched");

        var shipmentDecision = await router.RouteAsync(shipmentEnvelope);
        Assert.That(shipmentDecision.Destination, Is.EqualTo("shipping-topic"));

        // Unknown type falls to dead-letter.
        var unknownEnvelope = IntegrationEnvelope<string>.Create(
            "unknown-1", "Gateway", "refund.issued");

        var unknownDecision = await router.RouteAsync(unknownEnvelope);
        Assert.That(unknownDecision.IsFallback, Is.True);
        Assert.That(unknownDecision.Destination, Is.EqualTo("dead-letter"));
    }

    // ── Challenge 2: Route Replacement — Re-Register Overwrites ─────────────

    [Test]
    public async Task Challenge2_RouteReplacement_LatestRegistrationWins()
    {
        // When a participant re-registers for the same condition key, the old
        // destination is replaced. Verify that only the latest destination
        // is used and that the routing table has exactly one entry.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "fallback",
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        // Version 1 of the order service registers.
        await router.RegisterAsync("order.created", "orders-v1-topic", "OrderService-v1");

        // Version 2 replaces the registration.
        await router.RegisterAsync("order.created", "orders-v2-topic", "OrderService-v2");

        // Routing table should have exactly one entry.
        var table = router.GetRoutingTable();
        Assert.That(table, Has.Count.EqualTo(1));
        Assert.That(table["order.created"].Destination, Is.EqualTo("orders-v2-topic"));
        Assert.That(table["order.created"].ParticipantId, Is.EqualTo("OrderService-v2"));

        // Messages route to the v2 destination.
        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "Gateway", "order.created");

        var decision = await router.RouteAsync(envelope);
        Assert.That(decision.Destination, Is.EqualTo("orders-v2-topic"));
    }

    // ── Challenge 3: Unregister Non-Existent Key Returns False ──────────────

    [Test]
    public async Task Challenge3_UnregisterNonExistent_ReturnsFalse()
    {
        // Unregistering a condition key that was never registered should return
        // false and leave the routing table unchanged.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new DynamicRouterOptions
        {
            ConditionField = "MessageType",
            FallbackTopic = "fallback",
        });

        var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

        await router.RegisterAsync("order.created", "orders-topic");

        // Try to unregister a key that doesn't exist.
        var removed = await router.UnregisterAsync("payment.received");
        Assert.That(removed, Is.False);

        // Original entry is still intact.
        var table = router.GetRoutingTable();
        Assert.That(table, Has.Count.EqualTo(1));
        Assert.That(table.ContainsKey("order.created"), Is.True);

        // Route still works.
        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "Gateway", "order.created");

        var decision = await router.RouteAsync(envelope);
        Assert.That(decision.Destination, Is.EqualTo("orders-topic"));
    }
}
