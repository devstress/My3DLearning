// ============================================================================
// Tutorial 30 – Business Rule Engine (Exam)
// ============================================================================
// Coding challenges: priority-based rule evaluation, StopOnMatch behavior,
// and metadata-based routing rules.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial30;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Priority-Based Evaluation ──────────────────────────────

    [Test]
    public async Task Challenge1_PriorityRouting_LowerPriorityWins()
    {
        // Two rules match the same message. The lower-priority-number rule
        // should fire first. With StopOnMatch = true (default), only one fires.
        var store = new InMemoryRuleStore();
        var engine = new BusinessRuleEngine(
            store,
            Options.Create(new RuleEngineOptions { Enabled = true }),
            NullLogger<BusinessRuleEngine>.Instance);

        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "BroadMatch",
            Priority = 10,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "order" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "general-orders" },
        });

        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "SpecificMatch",
            Priority = 1,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "new-orders" },
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "OrderService", "order.created");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
        Assert.That(result.MatchedRules[0].Name, Is.EqualTo("SpecificMatch"));
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("new-orders"));
    }

    // ── Challenge 2: StopOnMatch = false Collects Multiple ──────────────────

    [Test]
    public async Task Challenge2_StopOnMatchFalse_CollectsMultipleRules()
    {
        var store = new InMemoryRuleStore();
        var engine = new BusinessRuleEngine(
            store,
            Options.Create(new RuleEngineOptions { Enabled = true }),
            NullLogger<BusinessRuleEngine>.Instance);

        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "Rule1",
            Priority = 1,
            StopOnMatch = false,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "order" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "audit-topic" },
        });

        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "Rule2",
            Priority = 2,
            StopOnMatch = true,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "orders-topic" },
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "Service", "order.created");
        var result = await engine.EvaluateAsync(envelope);

        // Both rules match. Rule1 doesn't stop, Rule2 does.
        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.MatchedRules.Count, Is.EqualTo(2));
        Assert.That(result.Actions.Count, Is.EqualTo(2));
    }

    // ── Challenge 3: Metadata-Based Rule ────────────────────────────────────

    [Test]
    public async Task Challenge3_MetadataBasedRule_RoutesOnTenantId()
    {
        var store = new InMemoryRuleStore();
        var engine = new BusinessRuleEngine(
            store,
            Options.Create(new RuleEngineOptions { Enabled = true }),
            NullLogger<BusinessRuleEngine>.Instance);

        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "PremiumTenant",
            Priority = 1,
            Conditions = [new RuleCondition { FieldName = "Metadata.tenant", Operator = RuleConditionOperator.Equals, Value = "premium-corp" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "premium-processing" },
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "Service", "event") with
        {
            Metadata = new Dictionary<string, string> { ["tenant"] = "premium-corp" },
        };

        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("premium-processing"));
    }
}
