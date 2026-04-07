// ============================================================================
// Broker-Agnostic EIP Tests — Routing Patterns
// ============================================================================
// These tests prove that ContentBasedRouter, RecipientListRouter, MessageFilter,
// and DynamicRouter work identically regardless of the underlying broker.
// The router publishes to topics via IMessageBrokerProducer — any broker works.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using EnterpriseIntegrationPlatform.RuleEngine;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace BrokerAgnosticTests;

[TestFixture]
public sealed class RoutingTests
{
    // ── 1. Content-Based Router ─────────────────────────────────────────

    [Test]
    public async Task ContentBasedRouter_RoutesOnMessageType()
    {
        // Content-Based Router inspects envelope fields and routes to matching topic.
        var broker = new MockEndpoint("cbr");
        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                Rules =
                [
                    new RoutingRule
                    {
                        Name = "orders",
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Equals,
                        Value = "OrderCreated",
                        TargetTopic = "processing.orders",
                        Priority = 1
                    },
                    new RoutingRule
                    {
                        Name = "payments",
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Equals,
                        Value = "PaymentReceived",
                        TargetTopic = "processing.payments",
                        Priority = 2
                    }
                ],
                DefaultTopic = "processing.unmatched"
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var orderEnv = IntegrationEnvelope<string>.Create(
            "order-1", "OrderSvc", "OrderCreated");
        var paymentEnv = IntegrationEnvelope<string>.Create(
            "payment-1", "PaymentSvc", "PaymentReceived");
        var unknownEnv = IntegrationEnvelope<string>.Create(
            "unknown-1", "UnknownSvc", "SomethingElse");

        var r1 = await router.RouteAsync(orderEnv);
        var r2 = await router.RouteAsync(paymentEnv);
        var r3 = await router.RouteAsync(unknownEnv);

        // Verify routing decisions
        Assert.That(r1.TargetTopic, Is.EqualTo("processing.orders"));
        Assert.That(r1.IsDefault, Is.False);
        Assert.That(r2.TargetTopic, Is.EqualTo("processing.payments"));
        Assert.That(r2.IsDefault, Is.False);
        Assert.That(r3.TargetTopic, Is.EqualTo("processing.unmatched"));
        Assert.That(r3.IsDefault, Is.True);

        // Verify all 3 messages were published to broker
        broker.AssertReceivedCount(3);
        broker.AssertReceivedOnTopic("processing.orders", 1);
        broker.AssertReceivedOnTopic("processing.payments", 1);
        broker.AssertReceivedOnTopic("processing.unmatched", 1);
    }

    [Test]
    public async Task ContentBasedRouter_RoutesOnSource()
    {
        // Routes based on the Source field of the envelope.
        var broker = new MockEndpoint("cbr-source");
        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                Rules =
                [
                    new RoutingRule
                    {
                        Name = "external",
                        FieldName = "Source",
                        Operator = RoutingOperator.Contains,
                        Value = "External",
                        TargetTopic = "inbound.external",
                        Priority = 1
                    }
                ],
                DefaultTopic = "inbound.internal"
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var externalEnv = IntegrationEnvelope<string>.Create(
            "data", "ExternalPartner", "Event");
        var internalEnv = IntegrationEnvelope<string>.Create(
            "data", "InternalService", "Event");

        await router.RouteAsync(externalEnv);
        await router.RouteAsync(internalEnv);

        broker.AssertReceivedOnTopic("inbound.external", 1);
        broker.AssertReceivedOnTopic("inbound.internal", 1);
    }

    [Test]
    public async Task ContentBasedRouter_RoutesOnMetadata()
    {
        // Routes based on metadata key-value pairs.
        var broker = new MockEndpoint("cbr-metadata");
        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                Rules =
                [
                    new RoutingRule
                    {
                        Name = "high-priority",
                        FieldName = "Metadata.region",
                        Operator = RoutingOperator.Equals,
                        Value = "APAC",
                        TargetTopic = "regional.apac",
                        Priority = 1
                    }
                ],
                DefaultTopic = "regional.default"
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var apacEnv = IntegrationEnvelope<string>.Create("d", "S", "T");
        apacEnv.Metadata["region"] = "APAC";
        var euEnv = IntegrationEnvelope<string>.Create("d", "S", "T");
        euEnv.Metadata["region"] = "EU";

        await router.RouteAsync(apacEnv);
        await router.RouteAsync(euEnv);

        broker.AssertReceivedOnTopic("regional.apac", 1);
        broker.AssertReceivedOnTopic("regional.default", 1);
    }

    // ── 2. Regex Routing ────────────────────────────────────────────────

    [Test]
    public async Task ContentBasedRouter_RegexOperator_MatchesPattern()
    {
        var broker = new MockEndpoint("cbr-regex");
        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                Rules =
                [
                    new RoutingRule
                    {
                        Name = "v2-events",
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Regex,
                        Value = "^Order.*V2$",
                        TargetTopic = "v2.orders",
                        Priority = 1
                    }
                ],
                DefaultTopic = "v1.orders"
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var v2Env = IntegrationEnvelope<string>.Create("d", "S", "OrderCreatedV2");
        var v1Env = IntegrationEnvelope<string>.Create("d", "S", "OrderCreated");

        await router.RouteAsync(v2Env);
        await router.RouteAsync(v1Env);

        broker.AssertReceivedOnTopic("v2.orders", 1);
        broker.AssertReceivedOnTopic("v1.orders", 1);
    }

    // ── 3. Message Filter ───────────────────────────────────────────────

    [Test]
    public async Task MessageFilter_PassesMatchingMessages()
    {
        var broker = new MockEndpoint("filter");
        var filter = new MessageFilter(
            broker,
            Options.Create(new MessageFilterOptions
            {
                Conditions =
                [
                    new RuleCondition
                    {
                        FieldName = "MessageType",
                        Operator = RuleConditionOperator.Equals,
                        Value = "Important"
                    }
                ],
                OutputTopic = "accepted",
                DiscardTopic = "discarded"
            }),
            NullLogger<MessageFilter>.Instance);

        var important = IntegrationEnvelope<string>.Create("d", "S", "Important");
        var trivial = IntegrationEnvelope<string>.Create("d", "S", "Trivial");

        var r1 = await filter.FilterAsync(important);
        var r2 = await filter.FilterAsync(trivial);

        Assert.That(r1.Passed, Is.True);
        Assert.That(r2.Passed, Is.False);

        // Important → accepted, Trivial → discarded
        broker.AssertReceivedOnTopic("accepted", 1);
        broker.AssertReceivedOnTopic("discarded", 1);
    }

    // ── 4. No Default Topic — Throws ────────────────────────────────────

    [Test]
    public void ContentBasedRouter_NoDefault_NoMatch_Throws()
    {
        var broker = new MockEndpoint("cbr-no-default");
        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                Rules =
                [
                    new RoutingRule
                    {
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Equals,
                        Value = "SpecificType",
                        TargetTopic = "specific.topic",
                        Priority = 1
                    }
                ]
                // No DefaultTopic!
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var nomatch = IntegrationEnvelope<string>.Create("d", "S", "OtherType");

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            router.RouteAsync(nomatch));
    }

    // ── 5. Priority-Based Rule Ordering ─────────────────────────────────

    [Test]
    public async Task ContentBasedRouter_HigherPriority_MatchesFirst()
    {
        // When multiple rules could match, the one with lower Priority value wins.
        var broker = new MockEndpoint("cbr-priority");
        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                Rules =
                [
                    new RoutingRule
                    {
                        Name = "broad",
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Contains,
                        Value = "Order",
                        TargetTopic = "broad.orders",
                        Priority = 10
                    },
                    new RoutingRule
                    {
                        Name = "specific",
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Equals,
                        Value = "OrderCreated",
                        TargetTopic = "specific.orders",
                        Priority = 1
                    }
                ]
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var env = IntegrationEnvelope<string>.Create("d", "S", "OrderCreated");
        var decision = await router.RouteAsync(env);

        // Priority 1 (specific) wins over priority 10 (broad)
        Assert.That(decision.TargetTopic, Is.EqualTo("specific.orders"));
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("specific"));
    }
}
