// ============================================================================
// Tutorial 10 – Message Filter (Exam)
// ============================================================================
// E2E challenges: spam filter with In operator, priority-based filter, and
// multi-condition AND filter — all verified via MockEndpoint topic counts.
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
    [Test]
    public async Task Challenge1_SpamFilter_AcceptsTrustedRejectsOthers()
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

    [Test]
    public async Task Challenge2_PriorityFilter_OnlyHighCriticalPass()
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

    [Test]
    public async Task Challenge3_MetadataFilter_AndLogic_BothConditionsRequired()
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
