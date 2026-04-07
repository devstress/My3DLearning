// ============================================================================
// Tutorial 12 – Recipient List (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Recipient List pattern in realistic
//          scenarios that combine multiple routing concepts end-to-end.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Multi-rule fan-out with metadata-driven priority alerts
//   🟡 Intermediate — Rule-based and metadata-based recipients merged together
//   🔴 Advanced     — Cross-rule and cross-source deduplication verification
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint (in-memory capture for assertion)
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

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
        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    Name = "OrderNotify",
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "order.created",
                    Destinations = ["email-svc", "sms-svc", "push-svc"],
                },
                new RecipientListRule
                {
                    Name = "HighPriorityAlert",
                    FieldName = "Metadata.priority",
                    Operator = RoutingOperator.Equals,
                    Value = "high",
                    Destinations = ["pager-svc"],
                },
            ],
        });
        var router = new RecipientListRouter(
            output, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-1", "svc", "order.created") with
        {
            Metadata = new Dictionary<string, string> { ["priority"] = "high" },
        };
        var result = await router.RouteAsync(envelope);

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
        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    Name = "AuditAll",
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Contains,
                    Value = "order",
                    Destinations = ["audit-log"],
                },
            ],
            MetadataRecipientsKey = "extra-recipients",
        });
        var router = new RecipientListRouter(
            output, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "svc", "order.created") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["extra-recipients"] = "webhook-svc,reporting-svc",
            },
        };
        var result = await router.RouteAsync(envelope);

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
        var options = Options.Create(new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    Name = "TypeRule",
                    FieldName = "MessageType",
                    Operator = RoutingOperator.StartsWith,
                    Value = "order",
                    Destinations = ["shared-topic", "orders-topic"],
                },
                new RecipientListRule
                {
                    Name = "SourceRule",
                    FieldName = "Source",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderService",
                    Destinations = ["shared-topic", "source-audit"],
                },
            ],
            MetadataRecipientsKey = "recipients",
        });
        var router = new RecipientListRouter(
            output, options, NullLogger<RecipientListRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["recipients"] = "shared-topic,extra-topic",
            },
        };
        var result = await router.RouteAsync(envelope);

        Assert.That(result.DuplicatesRemoved, Is.GreaterThanOrEqualTo(2));
        Assert.That(result.Destinations, Does.Contain("shared-topic"));
        Assert.That(result.Destinations, Does.Contain("orders-topic"));
        Assert.That(result.Destinations, Does.Contain("source-audit"));
        Assert.That(result.Destinations, Does.Contain("extra-topic"));
        output.AssertReceivedCount(result.ResolvedCount);
    }
}
