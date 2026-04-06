// ============================================================================
// Tutorial 10 – Message Filter (Exam)
// ============================================================================
// Coding challenges: build a spam filter, a priority-based filter, and a
// metadata-based filter using the MessageFilter and RuleCondition types.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial10;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Spam Filter — Reject Messages from Specific Sources ────

    [Test]
    public async Task Challenge1_SpamFilter_RejectsUntrustedSources()
    {
        // Build a filter that ONLY accepts messages from "TrustedPartnerA"
        // or "TrustedPartnerB".  All other sources are discarded to a DLQ.
        //
        // Using the "In" operator with comma-separated trusted values.
        var producer = Substitute.For<IMessageBrokerProducer>();

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
            OutputTopic = "legitimate-messages",
            DiscardTopic = "spam-quarantine",
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        // Trusted source passes.
        var trustedEnvelope = IntegrationEnvelope<string>.Create(
            "partner-data", "TrustedPartnerA", "partner.update");

        var passResult = await filter.FilterAsync(trustedEnvelope);
        Assert.That(passResult.Passed, Is.True);
        Assert.That(passResult.OutputTopic, Is.EqualTo("legitimate-messages"));

        // Untrusted (spam) source is rejected.
        var spamEnvelope = IntegrationEnvelope<string>.Create(
            "spam-payload", "MaliciousBot", "spam.broadcast");

        var rejectResult = await filter.FilterAsync(spamEnvelope);
        Assert.That(rejectResult.Passed, Is.False);
        Assert.That(rejectResult.OutputTopic, Is.EqualTo("spam-quarantine"));
    }

    [Test]
    public async Task Challenge1_SpamFilter_SecondTrustedPartnerAlsoAccepted()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

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
            OutputTopic = "legitimate-messages",
            DiscardTopic = "spam-quarantine",
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        var partnerB = IntegrationEnvelope<string>.Create(
            "b-data", "TrustedPartnerB", "partner.sync");

        var result = await filter.FilterAsync(partnerB);
        Assert.That(result.Passed, Is.True);
    }

    // ── Challenge 2: Priority Filter — Only High/Critical Pass ──────────────

    [Test]
    public async Task Challenge2_PriorityFilter_OnlyHighAndCriticalPass()
    {
        // Create a filter that only accepts messages with Priority "High" or "Critical".
        // The Priority field on the envelope is extracted as its enum string representation.
        var producer = Substitute.For<IMessageBrokerProducer>();

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
            DiscardTopic = "low-priority-archive",
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        // High priority passes.
        var highPriority = IntegrationEnvelope<string>.Create(
            "urgent-data", "AlertService", "alert.fired") with
        {
            Priority = MessagePriority.High,
        };

        var highResult = await filter.FilterAsync(highPriority);
        Assert.That(highResult.Passed, Is.True);
        Assert.That(highResult.OutputTopic, Is.EqualTo("priority-processing"));

        // Critical priority passes.
        var criticalPriority = IntegrationEnvelope<string>.Create(
            "critical-data", "AlertService", "alert.critical") with
        {
            Priority = MessagePriority.Critical,
        };

        var criticalResult = await filter.FilterAsync(criticalPriority);
        Assert.That(criticalResult.Passed, Is.True);

        // Normal priority is rejected.
        var normalPriority = IntegrationEnvelope<string>.Create(
            "normal-data", "ReportService", "report.generated") with
        {
            Priority = MessagePriority.Normal,
        };

        var normalResult = await filter.FilterAsync(normalPriority);
        Assert.That(normalResult.Passed, Is.False);
        Assert.That(normalResult.OutputTopic, Is.EqualTo("low-priority-archive"));

        // Low priority is rejected.
        var lowPriority = IntegrationEnvelope<string>.Create(
            "background-data", "BatchService", "batch.completed") with
        {
            Priority = MessagePriority.Low,
        };

        var lowResult = await filter.FilterAsync(lowPriority);
        Assert.That(lowResult.Passed, Is.False);
    }

    // ── Challenge 3: Metadata-Based Filter with Multiple Conditions ─────────

    [Test]
    public async Task Challenge3_MetadataFilter_RequiresTenantAndEnvironment()
    {
        // Build a filter that requires BOTH conditions (AND logic):
        //   1. Metadata.tenant must equal "acme-corp"
        //   2. Metadata.environment must equal "production"
        // Messages missing either metadata key are rejected.
        var producer = Substitute.For<IMessageBrokerProducer>();

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

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        // Both conditions met — passes.
        var validEnvelope = IntegrationEnvelope<string>.Create(
            "prod-data", "AcmeService", "data.sync") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["tenant"] = "acme-corp",
                ["environment"] = "production",
            },
        };

        var passResult = await filter.FilterAsync(validEnvelope);
        Assert.That(passResult.Passed, Is.True);
        Assert.That(passResult.OutputTopic, Is.EqualTo("production-acme"));

        // Wrong tenant — rejected.
        var wrongTenant = IntegrationEnvelope<string>.Create(
            "other-data", "OtherService", "data.sync") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["tenant"] = "other-corp",
                ["environment"] = "production",
            },
        };

        var rejectTenant = await filter.FilterAsync(wrongTenant);
        Assert.That(rejectTenant.Passed, Is.False);

        // Wrong environment — rejected.
        var wrongEnv = IntegrationEnvelope<string>.Create(
            "staging-data", "AcmeService", "data.sync") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["tenant"] = "acme-corp",
                ["environment"] = "staging",
            },
        };

        var rejectEnv = await filter.FilterAsync(wrongEnv);
        Assert.That(rejectEnv.Passed, Is.False);
    }

    [Test]
    public async Task Challenge3_MetadataFilter_OrLogic_EitherConditionSuffices()
    {
        // With OR logic, matching ANY condition is enough to pass.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "Metadata.priority-override",
                    Operator = RuleConditionOperator.Equals,
                    Value = "true",
                },
                new RuleCondition
                {
                    FieldName = "Metadata.vip-customer",
                    Operator = RuleConditionOperator.Equals,
                    Value = "true",
                },
            ],
            Logic = RuleLogicOperator.Or,
            OutputTopic = "fast-lane",
            DiscardTopic = "standard-lane",
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        // Only priority-override set — passes (OR logic).
        var priorityOverride = IntegrationEnvelope<string>.Create(
            "rush-order", "OrderService", "order.rush") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["priority-override"] = "true",
            },
        };

        var result1 = await filter.FilterAsync(priorityOverride);
        Assert.That(result1.Passed, Is.True);

        // Only vip-customer set — also passes.
        var vipOrder = IntegrationEnvelope<string>.Create(
            "vip-order", "OrderService", "order.vip") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["vip-customer"] = "true",
            },
        };

        var result2 = await filter.FilterAsync(vipOrder);
        Assert.That(result2.Passed, Is.True);

        // Neither condition met — rejected.
        var normalOrder = IntegrationEnvelope<string>.Create(
            "normal-order", "OrderService", "order.standard") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["customer-tier"] = "bronze",
            },
        };

        var result3 = await filter.FilterAsync(normalOrder);
        Assert.That(result3.Passed, Is.False);
        Assert.That(result3.OutputTopic, Is.EqualTo("standard-lane"));
    }
}
