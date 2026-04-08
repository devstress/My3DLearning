// ============================================================================
// Tutorial 30 – Business Rule Engine (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Rule Engine pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Multi-rule evaluation collects all matches
//   🟡 Intermediate — Reject action blocks routing
//   🔴 Advanced     — In operator matches comma-separated value list
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial30;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Multi-rule evaluation ─────────────────────────────
    //
    // SCENARIO: Two non-stop rules both match the same message. The engine
    //           collects all matched rules and their actions.
    //
    // WHAT YOU PROVE: Multiple matching rules accumulate when StopOnMatch
    //                 is false, and each action is collected.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_MultiRuleEvaluation_CollectsAllMatches()
    {
        await using var output = new MockEndpoint("rules-multi");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "Rule1", Priority = 1, StopOnMatch = false,
            Conditions = [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "order.created" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "audit" },
        });
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "Rule2", Priority = 2, StopOnMatch = false,
            Conditions = [new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Contains, Value = "Order" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "analytics" },
        });

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "OrderService", "order.created");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.MatchedRules, Has.Count.EqualTo(2));
        Assert.That(result.Actions, Has.Count.EqualTo(2));

        foreach (var action in result.Actions.Where(a => a.TargetTopic is not null))
            await output.PublishAsync(envelope, action.TargetTopic!);

        output.AssertReceivedOnTopic("audit", 1);
        output.AssertReceivedOnTopic("analytics", 1);
        output.AssertReceivedCount(2);
    }

    // ── 🟡 INTERMEDIATE — Reject action blocks routing ─────────────────
    //
    // SCENARIO: A rule matches a message from a blocked source and issues
    //           a Reject action. No messages should be routed.
    //
    // WHAT YOU PROVE: The Reject action type correctly prevents routing
    //                 and provides the rejection reason.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_RejectAction_NoRouting()
    {
        await using var output = new MockEndpoint("rules-reject");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "RejectBadSource", Priority = 1,
            Conditions = [new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "MaliciousService" }],
            Action = new RuleAction { ActionType = RuleActionType.Reject, Reason = "Blocked source" },
        });

        var engine = CreateEngine(store);
        var envelope = IntegrationEnvelope<string>.Create("data", "MaliciousService", "evt");
        var result = await engine.EvaluateAsync(envelope);

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.Actions[0].ActionType, Is.EqualTo(RuleActionType.Reject));
        Assert.That(result.Actions[0].Reason, Is.EqualTo("Blocked source"));

        // No publish for rejected messages — output stays empty.
        output.AssertNoneReceived();
    }

    // ── 🔴 ADVANCED — In operator with comma list ─────────────────────
    //
    // SCENARIO: A rule uses the In operator on a metadata field with a
    //           comma-separated value list. One message matches; another
    //           with a region not in the list does not.
    //
    // WHAT YOU PROVE: The In operator correctly matches against any value
    //                 in a comma-separated list and rejects non-members.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_InOperator_MatchesCommaList()
    {
        await using var output = new MockEndpoint("rules-in");
        var store = new InMemoryRuleStore();
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "RegionFilter", Priority = 1,
            Conditions = [new RuleCondition { FieldName = "Metadata.region", Operator = RuleConditionOperator.In, Value = "us-east,eu-west,ap-south" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "regional" },
        });

        var engine = CreateEngine(store);

        var envMatch = IntegrationEnvelope<string>.Create("d1", "Svc", "evt") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "eu-west" },
        };
        var envNoMatch = IntegrationEnvelope<string>.Create("d2", "Svc", "evt") with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "af-south" },
        };

        var r1 = await engine.EvaluateAsync(envMatch);
        var r2 = await engine.EvaluateAsync(envNoMatch);

        Assert.That(r1.HasMatch, Is.True);
        Assert.That(r2.HasMatch, Is.False);

        await output.PublishAsync(envMatch, r1.Actions[0].TargetTopic!);
        output.AssertReceivedOnTopic("regional", 1);
        output.AssertReceivedCount(1);
    }

    private static BusinessRuleEngine CreateEngine(InMemoryRuleStore store)
    {
        var opts = Options.Create(new RuleEngineOptions { Enabled = true, CacheEnabled = false });
        return new BusinessRuleEngine(store, opts, NullLogger<BusinessRuleEngine>.Instance);
    }
}
