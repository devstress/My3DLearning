// ============================================================================
// Tutorial 10 – Message Filter (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Filter trusted partners using In operator, quarantine others
//   🟡 Intermediate — Priority-based triage — only High/Critical pass through
//   🔴 Advanced     — Multi-condition AND filter on tenant and environment metadata
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
public sealed class Exam
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
        // TODO: Configure MessageFilterOptions with:
        //   Condition: FieldName="Source", Operator=In, Value="TrustedPartnerA,TrustedPartnerB"
        //   Logic=And, OutputTopic="legitimate", DiscardTopic="quarantine"
        var options = Options.Create(new MessageFilterOptions { OutputTopic = "" }); // ← replace with full configuration
        // TODO: Create a MessageFilter with output, options, and NullLogger
        MessageFilter filter = null!; // ← replace with new MessageFilter(...)

        // TODO: Create trusted envelope — payload "data", source "TrustedPartnerA", type "partner.update"
        IntegrationEnvelope<string> trusted = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        // TODO: Create spam envelope — payload "spam", source "MaliciousBot", type "spam.broadcast"
        IntegrationEnvelope<string> spam = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

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
        // TODO: Configure MessageFilterOptions with:
        //   Condition: FieldName="Priority", Operator=In, Value="High,Critical"
        //   Logic=And, OutputTopic="priority-processing", DiscardTopic="low-archive"
        var options = Options.Create(new MessageFilterOptions { OutputTopic = "" }); // ← replace with full configuration
        // TODO: Create a MessageFilter with output, options, and NullLogger
        MessageFilter filter = null!; // ← replace with new MessageFilter(...)

        // TODO: Create an IntegrationEnvelope<string> with payload "d", source "svc", type "ev"
        //       with Priority = MessagePriority.High
        IntegrationEnvelope<string> high = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Priority = ... }
        // TODO: Create an IntegrationEnvelope<string> with payload "d", source "svc", type "ev"
        //       with Priority = MessagePriority.Critical
        IntegrationEnvelope<string> critical = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Priority = ... }
        // TODO: Create an IntegrationEnvelope<string> with payload "d", source "svc", type "ev"
        //       with Priority = MessagePriority.Normal
        IntegrationEnvelope<string> normal = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Priority = ... }
        // TODO: Create an IntegrationEnvelope<string> with payload "d", source "svc", type "ev"
        //       with Priority = MessagePriority.Low
        IntegrationEnvelope<string> low = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Priority = ... }

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
        // TODO: Configure MessageFilterOptions with two conditions (AND logic):
        //   Condition 1: FieldName="Metadata.tenant", Operator=Equals, Value="acme-corp"
        //   Condition 2: FieldName="Metadata.environment", Operator=Equals, Value="production"
        //   Logic=And, OutputTopic="production-acme", DiscardTopic="non-prod-discard"
        var options = Options.Create(new MessageFilterOptions { OutputTopic = "" }); // ← replace with full configuration
        // TODO: Create a MessageFilter with output, options, and NullLogger
        MessageFilter filter = null!; // ← replace with new MessageFilter(...)

        // TODO: Create valid envelope — "d"/"svc"/"ev" with Metadata tenant="acme-corp", environment="production"
        IntegrationEnvelope<string> valid = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Metadata = ... }
        // TODO: Create wrongTenant envelope — "d"/"svc"/"ev" with Metadata tenant="other-corp", environment="production"
        IntegrationEnvelope<string> wrongTenant = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Metadata = ... }
        // TODO: Create wrongEnv envelope — "d"/"svc"/"ev" with Metadata tenant="acme-corp", environment="staging"
        IntegrationEnvelope<string> wrongEnv = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { Metadata = ... }

        Assert.That((await filter.FilterAsync(valid)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(wrongTenant)).Passed, Is.False);
        Assert.That((await filter.FilterAsync(wrongEnv)).Passed, Is.False);

        output.AssertReceivedOnTopic("production-acme", 1);
        output.AssertReceivedOnTopic("non-prod-discard", 2);
    }
}
