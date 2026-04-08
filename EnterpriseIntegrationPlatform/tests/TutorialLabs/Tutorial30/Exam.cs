// ============================================================================
// Tutorial 30 – Business Rule Engine (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Multi-rule evaluation collects all matches
//   🟡 Intermediate  — Reject action blocks routing
//   🔴 Advanced      — In operator matches comma-separated value list
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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
        // TODO: Create a InMemoryRuleStore with appropriate configuration
        dynamic store = null!;
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
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var result = await engine.EvaluateAsync(...)
        dynamic result = null!;

        Assert.That(result.MatchedRules, Has.Count.EqualTo(2));
        Assert.That(result.Actions, Has.Count.EqualTo(2));

        foreach (var action in result.Actions.Where(a => a.TargetTopic is not null))
            // TODO: await output.PublishAsync(...)

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
        // TODO: Create a InMemoryRuleStore with appropriate configuration
        dynamic store = null!;
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "RejectBadSource", Priority = 1,
            Conditions = [new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "MaliciousService" }],
            Action = new RuleAction { ActionType = RuleActionType.Reject, Reason = "Blocked source" },
        });

        var engine = CreateEngine(store);
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var result = await engine.EvaluateAsync(...)
        dynamic result = null!;

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
        // TODO: Create a InMemoryRuleStore with appropriate configuration
        dynamic store = null!;
        await store.AddOrUpdateAsync(new BusinessRule
        {
            Name = "RegionFilter", Priority = 1,
            Conditions = [new RuleCondition { FieldName = "Metadata.region", Operator = RuleConditionOperator.In, Value = "us-east,eu-west,ap-south" }],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "regional" },
        });

        var engine = CreateEngine(store);

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envMatch = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envNoMatch = null!;

        // TODO: var r1 = await engine.EvaluateAsync(...)
        dynamic r1 = null!;
        // TODO: var r2 = await engine.EvaluateAsync(...)
        dynamic r2 = null!;

        Assert.That(r1.HasMatch, Is.True);
        Assert.That(r2.HasMatch, Is.False);

        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("regional", 1);
        output.AssertReceivedCount(1);
    }

    private static BusinessRuleEngine CreateEngine(InMemoryRuleStore store)
    {
        var opts = Options.Create(new RuleEngineOptions { Enabled = true, CacheEnabled = false });
        return new BusinessRuleEngine(store, opts, NullLogger<BusinessRuleEngine>.Instance);
    }
}
#endif
