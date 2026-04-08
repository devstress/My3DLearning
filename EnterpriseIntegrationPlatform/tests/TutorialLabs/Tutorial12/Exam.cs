// ============================================================================
// Tutorial 12 – Recipient List (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Multi-rule fan-out with metadata-driven priority alerts
//   🟡 Intermediate  — Rule-based and metadata-based recipients merged together
//   🔴 Advanced      — Cross-rule and cross-source deduplication verification
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial12;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Event Notification Fan-Out ────────────────────────
    //
    // SCENARIO: An order is placed with high priority. The system must
    //           notify email, SMS, and push channels, plus page on-call
    //           staff for high-priority orders.
    //
    // WHAT YOU PROVE: Multiple rules can fire on the same envelope,
    //                 routing to all matching destinations in one pass.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Starter_EventNotificationFanOut_RoutesToAllSubscribers()
    {
        await using var output = new MockEndpoint("notification");
        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a RecipientListRouter with appropriate configuration
        dynamic router = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var result = await router.RouteAsync(...)
        dynamic result = null!;

        Assert.That(result.ResolvedCount, Is.EqualTo(4));
        output.AssertReceivedOnTopic("email-svc", 1);
        output.AssertReceivedOnTopic("sms-svc", 1);
        output.AssertReceivedOnTopic("push-svc", 1);
        output.AssertReceivedOnTopic("pager-svc", 1);
    }

    // ── 🟡 INTERMEDIATE — Rules + Metadata Merge ──────────────────────
    //
    // SCENARIO: A static audit rule always captures order events, but the
    //           sender also embeds dynamic webhook and reporting endpoints
    //           in envelope metadata. Both sources must be merged.
    //
    // WHAT YOU PROVE: Rule-based and metadata-based recipient resolution
    //                 combine into a single deduplicated destination set.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Intermediate_RulesAndMetadataCombined_MergesDestinations()
    {
        await using var output = new MockEndpoint("combined");
        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a RecipientListRouter with appropriate configuration
        dynamic router = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var result = await router.RouteAsync(...)
        dynamic result = null!;

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        output.AssertReceivedOnTopic("audit-log", 1);
        output.AssertReceivedOnTopic("webhook-svc", 1);
        output.AssertReceivedOnTopic("reporting-svc", 1);
    }

    // ── 🔴 ADVANCED — Cross-Rule Deduplication ─────────────────────────
    //
    // SCENARIO: Two static rules and a metadata recipients list all resolve
    //           "shared-topic". The router must deliver exactly one copy per
    //           unique destination while preserving all distinct endpoints.
    //
    // WHAT YOU PROVE: The recipient list correctly deduplicates destinations
    //                 across rules and metadata, reporting accurate counts.
    // ─────────────────────────────────────────────────────────────────────
    [Test]
    public async Task Advanced_CrossRuleDedup_RemovesDuplicateDestinations()
    {
        await using var output = new MockEndpoint("dedup");
        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a RecipientListRouter with appropriate configuration
        dynamic router = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var result = await router.RouteAsync(...)
        dynamic result = null!;

        Assert.That(result.DuplicatesRemoved, Is.GreaterThanOrEqualTo(2));
        Assert.That(result.Destinations, Does.Contain("shared-topic"));
        Assert.That(result.Destinations, Does.Contain("orders-topic"));
        Assert.That(result.Destinations, Does.Contain("source-audit"));
        Assert.That(result.Destinations, Does.Contain("extra-topic"));
        output.AssertReceivedCount(result.ResolvedCount);
    }
}
#endif
