// ============================================================================
// Tutorial 10 – Message Filter (Lab)
// ============================================================================
// EIP Pattern: Message Filter
// Real Integrations: Wire real MessageFilter with NatsBrokerEndpoint (real
// NATS JetStream via Aspire), configure accept/reject conditions using
// RuleCondition operators, verify messages arrive at output/discard topics.
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
public sealed class Lab
{
    // ── 1. Accept & Reject (Real NATS) ──────────────────────────────────

    [Test]
    public async Task Filter_Accept_PublishesToOutputTopic()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t10-accept");
        var outputTopic = AspireFixture.UniqueTopic("t10-accepted");
        var discardTopic = AspireFixture.UniqueTopic("t10-rejected");

        var filter = CreateFilter(nats, "order.created", outputTopic, discardTopic);

        var envelope = IntegrationEnvelope<string>.Create(
            "valid-order", "OrderService", "order.created");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
        Assert.That(result.OutputTopic, Is.EqualTo(outputTopic));
        nats.AssertReceivedOnTopic(outputTopic, 1);
    }

    [Test]
    public async Task Filter_Reject_PublishesToDiscardTopic()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t10-reject");
        var outputTopic = AspireFixture.UniqueTopic("t10-out");
        var discardTopic = AspireFixture.UniqueTopic("t10-disc");

        var filter = CreateFilter(nats, "order.created", outputTopic, discardTopic);

        var envelope = IntegrationEnvelope<string>.Create(
            "unknown", "UnknownService", "unknown.event");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.OutputTopic, Is.EqualTo(discardTopic));
        nats.AssertReceivedOnTopic(discardTopic, 1);
    }

    [Test]
    public async Task Filter_NoConditions_PassThrough()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t10-pass");
        var outputTopic = AspireFixture.UniqueTopic("t10-passthru");

        var options = Options.Create(new MessageFilterOptions
        {
            Conditions = [],
            OutputTopic = outputTopic,
        });
        var filter = new MessageFilter(
            nats, options, NullLogger<MessageFilter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("any", "svc", "any.type");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
        nats.AssertReceivedOnTopic(outputTopic, 1);
    }

    // ── 2. Silent Discard & Source Filtering (Real NATS) ────────────────

    [Test]
    public async Task Filter_SilentDiscard_NoPublishWhenNoDiscardTopic()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t10-silent");

        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "MessageType",
                    Operator = RuleConditionOperator.Equals,
                    Value = "expected.type",
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = "output-topic",
        });
        var filter = new MessageFilter(
            nats, options, NullLogger<MessageFilter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "wrong", "svc", "wrong.type");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.OutputTopic, Is.Null);
        Assert.That(result.Reason, Does.Contain("silently discarded"));
        nats.AssertNoneReceived();
    }

    [Test]
    public async Task Filter_BySource_AcceptsAndRejects()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t10-source");
        var trustedTopic = AspireFixture.UniqueTopic("t10-trusted");
        var untrustedTopic = AspireFixture.UniqueTopic("t10-untrusted");

        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "Source",
                    Operator = RuleConditionOperator.Equals,
                    Value = "TrustedService",
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = trustedTopic,
            DiscardTopic = untrustedTopic,
        });
        var filter = new MessageFilter(
            nats, options, NullLogger<MessageFilter>.Instance);

        var trusted = IntegrationEnvelope<string>.Create(
            "data", "TrustedService", "data.event");
        var untrusted = IntegrationEnvelope<string>.Create(
            "data", "UntrustedService", "data.event");

        Assert.That((await filter.FilterAsync(trusted)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(untrusted)).Passed, Is.False);

        nats.AssertReceivedOnTopic(trustedTopic, 1);
        nats.AssertReceivedOnTopic(untrustedTopic, 1);
    }

    // ── 3. Advanced Operators (Real NATS) ───────────────────────────────

    [Test]
    public async Task Filter_InOperator_MatchesAnyOfCommaSeparatedValues()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t10-in");
        var partnerTopic = AspireFixture.UniqueTopic("t10-partners");
        var rejectedTopic = AspireFixture.UniqueTopic("t10-reject");

        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "Source",
                    Operator = RuleConditionOperator.In,
                    Value = "PartnerA,PartnerB",
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = partnerTopic,
            DiscardTopic = rejectedTopic,
        });
        var filter = new MessageFilter(
            nats, options, NullLogger<MessageFilter>.Instance);

        var a = IntegrationEnvelope<string>.Create("d", "PartnerA", "ev");
        var b = IntegrationEnvelope<string>.Create("d", "PartnerB", "ev");
        var c = IntegrationEnvelope<string>.Create("d", "Unknown", "ev");

        Assert.That((await filter.FilterAsync(a)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(b)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(c)).Passed, Is.False);

        nats.AssertReceivedOnTopic(partnerTopic, 2);
        nats.AssertReceivedOnTopic(rejectedTopic, 1);
    }

    [Test]
    public async Task Filter_OrLogic_EitherConditionSuffices()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t10-or");
        var fastLane = AspireFixture.UniqueTopic("t10-fast");
        var standard = AspireFixture.UniqueTopic("t10-std");

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
                    FieldName = "Metadata.vip",
                    Operator = RuleConditionOperator.Equals,
                    Value = "true",
                },
            ],
            Logic = RuleLogicOperator.Or,
            OutputTopic = fastLane,
            DiscardTopic = standard,
        });
        var filter = new MessageFilter(
            nats, options, NullLogger<MessageFilter>.Instance);

        var overrideMsg = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Metadata = new Dictionary<string, string> { ["priority-override"] = "true" } };
        var vipMsg = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Metadata = new Dictionary<string, string> { ["vip"] = "true" } };
        var normalMsg = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Metadata = new Dictionary<string, string> { ["tier"] = "bronze" } };

        Assert.That((await filter.FilterAsync(overrideMsg)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(vipMsg)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(normalMsg)).Passed, Is.False);

        nats.AssertReceivedOnTopic(fastLane, 2);
        nats.AssertReceivedOnTopic(standard, 1);
    }

    private static MessageFilter CreateFilter(
        NatsBrokerEndpoint nats, string acceptType, string outputTopic, string discardTopic)
    {
        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "MessageType",
                    Operator = RuleConditionOperator.Equals,
                    Value = acceptType,
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = outputTopic,
            DiscardTopic = discardTopic,
        });
        return new MessageFilter(nats, options, NullLogger<MessageFilter>.Instance);
    }
}
