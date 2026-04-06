// ============================================================================
// Tutorial 12 – Recipient List (Exam)
// ============================================================================
// E2E challenges: event notification fan-out, combined rule + metadata
// resolution, and cross-rule deduplication verification via MockEndpoint.
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
    [Test]
    public async Task Challenge1_EventNotificationFanOut_RoutesToAllSubscribers()
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

    [Test]
    public async Task Challenge2_RulesAndMetadataCombined_MergesDestinations()
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

    [Test]
    public async Task Challenge3_CrossRuleDedup_RemovesDuplicateDestinations()
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
