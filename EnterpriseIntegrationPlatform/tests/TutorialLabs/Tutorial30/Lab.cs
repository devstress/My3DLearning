// ============================================================================
// Tutorial 30 – Business Rule Engine (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Rule Engine pattern evaluates
//          business rules with AND/OR condition logic against message fields.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Evaluate_MatchingRule_ReturnsMatch              — matching rule returns match with action
//   2. Evaluate_NoMatch_ReturnsEmpty                   — no matching rule returns empty result
//   3. Evaluate_ContainsOperator_MatchesSubstring      — Contains operator matches substring
//   4. Evaluate_MetadataCondition_MatchesMetadataField — metadata field condition matching
//   5. Evaluate_DisabledRule_IsSkipped                 — disabled rules are skipped
//   6. Evaluate_PriorityOrder_HigherPriorityWins       — higher priority rule wins with StopOnMatch
//   7. Evaluate_OrLogic_MatchesAnyCondition            — OR logic matches any condition
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial30;

[TestFixture]
public sealed class Lab
{
    // ── 1. Rule Matching ─────────────────────────────────────────────

    [Test]
    public async Task Evaluate_MatchingRule_ReturnsMatch()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t30-match");
        var topic = AspireFixture.UniqueTopic("t30-orders");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(CreateRouteRule("OrderRule", "MessageType",
            RuleConditionOperator.Equals, "order.created", topic));

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "order.created");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
        Assert.That(result.MatchedRules[0].Name, Is.EqualTo("OrderRule"));
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo(topic));

        await nats.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Evaluate_NoMatch_ReturnsEmpty()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t30-nomatch");
        var topic = AspireFixture.UniqueTopic("t30-default");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(CreateRouteRule("OrderRule", "MessageType",
            RuleConditionOperator.Equals, "order.created", "orders-topic"));

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "unknown.event");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.False);
        Assert.That(result.MatchedRules, Is.Empty);

        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Conditions & Logic ────────────────────────────────────────

    [Test]
    public async Task Evaluate_ContainsOperator_MatchesSubstring()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t30-contains");
        var topic = AspireFixture.UniqueTopic("t30-order-events");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(CreateRouteRule("PartialMatch", "Source",
            RuleConditionOperator.Contains, "Order", topic));

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "MyOrderService", "evt");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        await nats.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Evaluate_MetadataCondition_MatchesMetadataField()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t30-metadata");
        var topic = AspireFixture.UniqueTopic("t30-us-east");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(CreateRouteRule("RegionRule", "Metadata.region",
            RuleConditionOperator.Equals, "us-east", topic));

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "evt") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "us-east" },
        };
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        await nats.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Evaluate_DisabledRule_IsSkipped()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t30-disabled");
        var topic = AspireFixture.UniqueTopic("t30-fallback");
        var store = new InMemoryRuleStore();
        var rule = new BusinessRule
        {
            Name = "Disabled", Priority = 1, Enabled = false,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "evt" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "topic" },
        };
        await store.AddOrUpdateAsync(rule);

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "evt");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.False);
        Assert.That(result.RulesEvaluated, Is.EqualTo(0));

        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 3. Priority & Complex Rules ──────────────────────────────────

    [Test]
    public async Task Evaluate_PriorityOrder_HigherPriorityWins()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t30-priority");
        var topic = AspireFixture.UniqueTopic("t30-fast-lane");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "LowPriority", Priority = 10,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "general" },
        });
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "HighPriority", Priority = 1, StopOnMatch = true,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = topic },
        });

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "order.created");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo(topic));

        await nats.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Evaluate_OrLogic_MatchesAnyCondition()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t30-or");
        var topic = AspireFixture.UniqueTopic("t30-combined");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "OrRule", Priority = 1, LogicOperator = RuleLogicOperator.Or,
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "PaymentService" },
            ],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = topic },
        });

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "PaymentService", "payment.received");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        await nats.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    private static BusinessRuleEngine CreateEngine(InMemoryRuleStore store)
    {
        var opts = Options.Create(new RuleEngineOptions { Enabled = true, CacheEnabled = false });
        return new BusinessRuleEngine(store, opts, NullLogger<BusinessRuleEngine>.Instance);
    }

    private static BusinessRule CreateRouteRule(
        string name, string field, RuleConditionOperator op, string value, string targetTopic) =>
        new()
        {
            Name = name, Priority = 1,
            Conditions = [new RuleCondition { FieldName = field, Operator = op, Value = value }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = targetTopic },
        };
}
