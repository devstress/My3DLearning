// ============================================================================
// Tutorial 10 – Message Filter (Lab)
// ============================================================================
// EIP Pattern: Message Filter.
// E2E: Wire real MessageFilter with MockEndpoint, configure accept/reject
// conditions, verify messages arrive at correct output/discard topics.
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
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("filter-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task Filter_Accept_PublishesToOutputTopic()
    {
        var filter = CreateFilter("order.created", "orders-accepted", "orders-rejected");

        var envelope = IntegrationEnvelope<string>.Create(
            "valid-order", "OrderService", "order.created");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
        Assert.That(result.OutputTopic, Is.EqualTo("orders-accepted"));
        _output.AssertReceivedOnTopic("orders-accepted", 1);
    }

    [Test]
    public async Task Filter_Reject_PublishesToDiscardTopic()
    {
        var filter = CreateFilter("order.created", "orders-accepted", "orders-rejected");

        var envelope = IntegrationEnvelope<string>.Create(
            "unknown", "UnknownService", "unknown.event");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.OutputTopic, Is.EqualTo("orders-rejected"));
        _output.AssertReceivedOnTopic("orders-rejected", 1);
    }

    [Test]
    public async Task Filter_NoConditions_PassThrough()
    {
        var options = Options.Create(new MessageFilterOptions
        {
            Conditions = [],
            OutputTopic = "pass-through",
        });
        var filter = new MessageFilter(
            _output, options, NullLogger<MessageFilter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("any", "svc", "any.type");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
        _output.AssertReceivedOnTopic("pass-through", 1);
    }

    [Test]
    public async Task Filter_SilentDiscard_NoPublish()
    {
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
            _output, options, NullLogger<MessageFilter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "wrong", "svc", "wrong.type");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.OutputTopic, Is.Null);
        Assert.That(result.Reason, Does.Contain("silently discarded"));
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task Filter_BySource_AcceptsAndRejects()
    {
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
            OutputTopic = "trusted-out",
            DiscardTopic = "untrusted-dlq",
        });
        var filter = new MessageFilter(
            _output, options, NullLogger<MessageFilter>.Instance);

        var trusted = IntegrationEnvelope<string>.Create(
            "data", "TrustedService", "data.event");
        var untrusted = IntegrationEnvelope<string>.Create(
            "data", "UntrustedService", "data.event");

        Assert.That((await filter.FilterAsync(trusted)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(untrusted)).Passed, Is.False);

        _output.AssertReceivedOnTopic("trusted-out", 1);
        _output.AssertReceivedOnTopic("untrusted-dlq", 1);
    }

    [Test]
    public async Task Filter_InOperator_MultipleSources()
    {
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
            OutputTopic = "partners",
            DiscardTopic = "rejected",
        });
        var filter = new MessageFilter(
            _output, options, NullLogger<MessageFilter>.Instance);

        var a = IntegrationEnvelope<string>.Create("d", "PartnerA", "ev");
        var b = IntegrationEnvelope<string>.Create("d", "PartnerB", "ev");
        var c = IntegrationEnvelope<string>.Create("d", "Unknown", "ev");

        Assert.That((await filter.FilterAsync(a)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(b)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(c)).Passed, Is.False);

        _output.AssertReceivedOnTopic("partners", 2);
        _output.AssertReceivedOnTopic("rejected", 1);
    }

    [Test]
    public async Task Filter_OrLogic_EitherConditionSuffices()
    {
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
            OutputTopic = "fast-lane",
            DiscardTopic = "standard",
        });
        var filter = new MessageFilter(
            _output, options, NullLogger<MessageFilter>.Instance);

        var overrideMsg = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Metadata = new Dictionary<string, string> { ["priority-override"] = "true" } };
        var vipMsg = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Metadata = new Dictionary<string, string> { ["vip"] = "true" } };
        var normalMsg = IntegrationEnvelope<string>.Create("d", "svc", "ev") with
            { Metadata = new Dictionary<string, string> { ["tier"] = "bronze" } };

        Assert.That((await filter.FilterAsync(overrideMsg)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(vipMsg)).Passed, Is.True);
        Assert.That((await filter.FilterAsync(normalMsg)).Passed, Is.False);

        _output.AssertReceivedOnTopic("fast-lane", 2);
        _output.AssertReceivedOnTopic("standard", 1);
    }

    private MessageFilter CreateFilter(string acceptType, string outputTopic, string discardTopic)
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
        return new MessageFilter(_output, options, NullLogger<MessageFilter>.Instance);
    }
}
