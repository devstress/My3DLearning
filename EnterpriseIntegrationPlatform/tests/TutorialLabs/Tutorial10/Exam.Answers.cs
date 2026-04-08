// ============================================================================
// Tutorial 10 – Message Filter (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial10;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — Spam Filter with Trusted Partner Whitelist ──────────
    //
    // SCENARIO: An integration gateway receives messages from multiple sources.
    // Only trusted partners (TrustedPartnerA, TrustedPartnerB) should pass
    // through to the legitimate queue; all others are quarantined.
    //
    // WHAT YOU PROVE: You can use the In operator to whitelist trusted sources
    // and route untrusted messages to a quarantine discard topic.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Starter_SpamFilter_AcceptsTrustedRejectsOthers()
    {
        await using var output = new MockEndpoint("spam");
        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "Source",
                    Operator = RuleConditionOperator.In,
                    Value = "TrustedPartnerA,TrustedPartnerB",
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = "legitimate",
            DiscardTopic = "quarantine",
        });
        var filter = new MessageFilter(
            output, options, NullLogger<MessageFilter>.Instance);

        var trusted = IntegrationEnvelope<string>.Create(
            "data", "TrustedPartnerA", "partner.update");
        var spam = IntegrationEnvelope<string>.Create(
            "spam", "MaliciousBot", "spam.broadcast");

        Assert.That((await filter.FilterAsync(trusted)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(spam)).Passed, Is.False);

        output.AssertReceivedOnTopic("legitimate", 1);
        output.AssertReceivedOnTopic("quarantine", 1);
    }

    // ── 🟡 INTERMEDIATE — Priority-Based Message Triage ──────────────────
    //
    // SCENARIO: A monitoring system receives alerts at all priority levels.
    // Only High and Critical priority messages should reach priority processing;
    // Normal and Low messages are archived for later review.
    //
    // WHAT YOU PROVE: You can filter messages by priority using the In operator
    // and verify correct routing to processing vs. archive topics.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Intermediate_PriorityFilter_OnlyHighCriticalPass()
    {
        await using var output = new MockEndpoint("priority");
        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "Priority",
                    Operator = RuleConditionOperator.In,
                    Value = "High,Critical",
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = "priority-processing",
            DiscardTopic = "low-archive",
        });
        var filter = new MessageFilter(
            output, options, NullLogger<MessageFilter>.Instance);

        var high = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Priority = MessagePriority.High };
        var critical = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Priority = MessagePriority.Critical };
        var normal = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Priority = MessagePriority.Normal };
        var low = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Priority = MessagePriority.Low };

        Assert.That((await filter.FilterAsync(high)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(critical)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(normal)).Passed, Is.False);
        Assert.That((await filter.FilterAsync(low)).Passed, Is.False);

        output.AssertReceivedOnTopic("priority-processing", 2);
        output.AssertReceivedOnTopic("low-archive", 2);
    }

    // ── 🔴 ADVANCED — Multi-Condition AND Filter on Metadata ──────────────
    //
    // SCENARIO: A multi-tenant SaaS platform must route messages only when
    // BOTH the tenant is "acme-corp" AND the environment is "production".
    // Messages missing either condition are discarded to non-prod-discard.
    //
    // WHAT YOU PROVE: You can combine multiple RuleConditions with AND logic
    // and verify that all conditions must be satisfied for acceptance.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Advanced_MetadataFilter_AndLogic_BothConditionsRequired()
    {
        await using var output = new MockEndpoint("metadata");
        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "Metadata.tenant",
                    Operator = RuleConditionOperator.Equals,
                    Value = "acme-corp",
                },
                new RuleCondition
                {
                    FieldName = "Metadata.environment",
                    Operator = RuleConditionOperator.Equals,
                    Value = "production",
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = "production-acme",
            DiscardTopic = "non-prod-discard",
        });
        var filter = new MessageFilter(
            output, options, NullLogger<MessageFilter>.Instance);

        var valid = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
        {
            Metadata = new Dictionary<string, string>
                { ["tenant"] = "acme-corp", ["environment"] = "production" },
        };
        var wrongTenant = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
        {
            Metadata = new Dictionary<string, string>
                { ["tenant"] = "other-corp", ["environment"] = "production" },
        };
        var wrongEnv = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
        {
            Metadata = new Dictionary<string, string>
                { ["tenant"] = "acme-corp", ["environment"] = "staging" },
        };

        Assert.That((await filter.FilterAsync(valid)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(wrongTenant)).Passed, Is.False);
        Assert.That((await filter.FilterAsync(wrongEnv)).Passed, Is.False);

        output.AssertReceivedOnTopic("production-acme", 1);
        output.AssertReceivedOnTopic("non-prod-discard", 2);
    }
}
