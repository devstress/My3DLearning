// ============================================================================
// Tutorial 30 – Business Rule Engine (Lab)
// ============================================================================
// This lab exercises the BusinessRuleEngine with InMemoryRuleStore, testing
// rule evaluation with different conditions, operators, actions, and logic.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial30;

[TestFixture]
public sealed class Lab
{
    private InMemoryRuleStore _store = null!;
    private BusinessRuleEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryRuleStore();
        var options = Options.Create(new RuleEngineOptions { Enabled = true });
        _engine = new BusinessRuleEngine(_store, options, NullLogger<BusinessRuleEngine>.Instance);
    }

    // ── Single Rule Matches by MessageType ──────────────────────────────────

    [Test]
    public async Task Evaluate_SingleEqualsRule_MatchesByMessageType()
    {
        await _store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "RouteOrders",
            Priority = 1,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "orders-topic" },
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "OrderService", "order.created");
        var result = await _engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("orders-topic"));
    }

    // ── No Match Returns Empty Result ───────────────────────────────────────

    [Test]
    public async Task Evaluate_NoMatchingRule_ReturnsNoMatch()
    {
        await _store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "RouteOrders",
            Priority = 1,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "orders-topic" },
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "PaymentService", "payment.received");
        var result = await _engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.False);
        Assert.That(result.MatchedRules, Is.Empty);
        Assert.That(result.Actions, Is.Empty);
    }

    // ── Contains Operator ───────────────────────────────────────────────────

    [Test]
    public async Task Evaluate_ContainsOperator_MatchesSubstring()
    {
        await _store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "AllOrders",
            Priority = 1,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "order" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "all-orders" },
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "Service", "order.shipped");
        var result = await _engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("all-orders"));
    }

    // ── AND Logic: All Conditions Must Match ────────────────────────────────

    [Test]
    public async Task Evaluate_AndLogic_AllConditionsMustMatch()
    {
        await _store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "HighPriorityOrders",
            Priority = 1,
            LogicOperator = RuleLogicOperator.And,
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "PremiumService" },
            ],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "premium-orders" },
        });

        // Only MessageType matches, Source doesn't → no match.
        var envelope1 = IntegrationEnvelope<string>.Create("data", "BasicService", "order.created");
        var result1 = await _engine.EvaluateAsync(envelope1);
        Assert.That(result1.HasMatch, Is.False);

        // Both match → match.
        var envelope2 = IntegrationEnvelope<string>.Create("data", "PremiumService", "order.created");
        var result2 = await _engine.EvaluateAsync(envelope2);
        Assert.That(result2.HasMatch, Is.True);
    }

    // ── OR Logic: Any Condition Matches ─────────────────────────────────────

    [Test]
    public async Task Evaluate_OrLogic_AnyConditionMatches()
    {
        await _store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "OrderOrPayment",
            Priority = 1,
            LogicOperator = RuleLogicOperator.Or,
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" },
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "payment.received" },
            ],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "finance" },
        });

        var orderEnvelope = IntegrationEnvelope<string>.Create("data", "Service", "order.created");
        var orderResult = await _engine.EvaluateAsync(orderEnvelope);
        Assert.That(orderResult.HasMatch, Is.True);

        var paymentEnvelope = IntegrationEnvelope<string>.Create("data", "Service", "payment.received");
        var paymentResult = await _engine.EvaluateAsync(paymentEnvelope);
        Assert.That(paymentResult.HasMatch, Is.True);
    }

    // ── Disabled Rule Is Skipped ────────────────────────────────────────────

    [Test]
    public async Task Evaluate_DisabledRule_IsSkipped()
    {
        await _store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "DisabledRule",
            Priority = 1,
            Enabled = false,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
            Action = new RuleAction { ActionType = RuleActionType.Reject, Reason = "disabled" },
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "Service", "order.created");
        var result = await _engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.False);
    }

    // ── Reject Action Type ──────────────────────────────────────────────────

    [Test]
    public async Task Evaluate_RejectAction_ReturnsRejectWithReason()
    {
        await _store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "RejectSpam",
            Priority = 1,
            Conditions = [new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "SpamService" }],
            Action = new RuleAction { ActionType = RuleActionType.Reject, Reason = "Spam detected" },
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "SpamService", "spam.event");
        var result = await _engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.Actions[0].ActionType, Is.EqualTo(RuleActionType.Reject));
        Assert.That(result.Actions[0].Reason, Is.EqualTo("Spam detected"));
    }
}
