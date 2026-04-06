// ============================================================================
// Tutorial 30 – Business Rule Engine (Lab)
// ============================================================================
// EIP Pattern: Rule Engine (Message Routing variant).
// E2E: BusinessRuleEngine with InMemoryRuleStore + MockEndpoint.
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
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("rules-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task Evaluate_MatchingRule_ReturnsMatch()
    {
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(CreateRouteRule("OrderRule", "MessageType",
            RuleConditionOperator.Equals, "order.created", "orders-topic"));

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "order.created");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
        Assert.That(result.MatchedRules[0].Name, Is.EqualTo("OrderRule"));
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("orders-topic"));

        await _output.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        _output.AssertReceivedOnTopic("orders-topic", 1);
    }

    [Test]
    public async Task Evaluate_NoMatch_ReturnsEmpty()
    {
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(CreateRouteRule("OrderRule", "MessageType",
            RuleConditionOperator.Equals, "order.created", "orders-topic"));

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "unknown.event");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.False);
        Assert.That(result.MatchedRules, Is.Empty);

        await _output.PublishAsync(envelope, "default-topic");
        _output.AssertReceivedOnTopic("default-topic", 1);
    }

    [Test]
    public async Task Evaluate_ContainsOperator_MatchesSubstring()
    {
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(CreateRouteRule("PartialMatch", "Source",
            RuleConditionOperator.Contains, "Order", "order-events"));

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "MyOrderService", "evt");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        await _output.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        _output.AssertReceivedOnTopic("order-events", 1);
    }

    [Test]
    public async Task Evaluate_MetadataCondition_MatchesMetadataField()
    {
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(CreateRouteRule("RegionRule", "Metadata.region",
            RuleConditionOperator.Equals, "us-east", "us-east-topic"));

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "evt") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "us-east" },
        };
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        await _output.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        _output.AssertReceivedOnTopic("us-east-topic", 1);
    }

    [Test]
    public async Task Evaluate_DisabledRule_IsSkipped()
    {
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

        await _output.PublishAsync(envelope, "fallback");
        _output.AssertReceivedOnTopic("fallback", 1);
    }

    [Test]
    public async Task Evaluate_PriorityOrder_HigherPriorityWins()
    {
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
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "fast-lane" },
        });

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "order.created");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("fast-lane"));

        await _output.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        _output.AssertReceivedOnTopic("fast-lane", 1);
    }

    [Test]
    public async Task Evaluate_OrLogic_MatchesAnyCondition()
    {
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "OrRule", Priority = 1, LogicOperator = RuleLogicOperator.Or,
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "PaymentService" },
            ],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "combined" },
        });

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "PaymentService", "payment.received");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        await _output.PublishAsync(envelope, result.Actions[0].TargetTopic!);
        _output.AssertReceivedOnTopic("combined", 1);
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
