// ============================================================================
// Tutorial 10 – Message Filter (Lab)
// ============================================================================
// This lab exercises the MessageFilter with various RuleCondition predicates.
// You will configure accept/reject filters, test default behaviour when no
// condition matches, and verify the MessageFilterResult for each scenario.
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
public sealed class Lab
{
    // ── Accept Filter: Message Passes Through ───────────────────────────────

    [Test]
    public async Task Filter_Accept_MessagePassesWhenPredicateMatches()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        // Only messages of type "order.created" pass through.
        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "MessageType",
                    Operator = RuleConditionOperator.Equals,
                    Value = "order.created",
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = "orders-accepted",
            DiscardTopic = "orders-rejected",
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "valid-order", "OrderService", "order.created");

        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
        Assert.That(result.OutputTopic, Is.EqualTo("orders-accepted"));
        Assert.That(result.Reason, Is.EqualTo("Predicate matched"));

        // Verify the message was published to the output topic.
        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("orders-accepted"),
            Arg.Any<CancellationToken>());
    }

    // ── Reject Filter: Message is Filtered Out ──────────────────────────────

    [Test]
    public async Task Filter_Reject_MessageDiscardedWhenPredicateFails()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var options = Options.Create(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "MessageType",
                    Operator = RuleConditionOperator.Equals,
                    Value = "order.created",
                },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = "orders-accepted",
            DiscardTopic = "orders-rejected",
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        // This message type does NOT match — it will be rejected.
        var envelope = IntegrationEnvelope<string>.Create(
            "unknown-data", "UnknownService", "unknown.event");

        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.OutputTopic, Is.EqualTo("orders-rejected"));
        Assert.That(result.Reason, Does.Contain("discard"));

        // Verify the message was published to the discard topic.
        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("orders-rejected"),
            Arg.Any<CancellationToken>());
    }

    // ── Default Action: No Conditions = Pass Through ────────────────────────

    [Test]
    public async Task Filter_NoConditions_DefaultPassThrough()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        // When no conditions are configured, the filter passes everything.
        var options = Options.Create(new MessageFilterOptions
        {
            Conditions = [],
            OutputTopic = "pass-through-topic",
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "any-data", "AnyService", "any.event");

        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
        Assert.That(result.OutputTopic, Is.EqualTo("pass-through-topic"));
    }

    // ── Silent Discard: No DiscardTopic Configured ──────────────────────────

    [Test]
    public async Task Filter_NoDiscardTopic_SilentlyDiscards()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

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
            // No DiscardTopic — silent discard.
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "wrong-data", "Service", "wrong.type");

        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.OutputTopic, Is.Null);
        Assert.That(result.Reason, Does.Contain("silently discarded"));

        // No publish calls at all — the message was silently dropped.
        await producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Verify FilterResult Contains Correct Details ────────────────────────

    [Test]
    public async Task Filter_Result_ContainsCorrectReasonAndTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

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
            OutputTopic = "trusted-output",
            DiscardTopic = "untrusted-dlq",
        });

        var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

        // Matching message.
        var trusted = IntegrationEnvelope<string>.Create(
            "trusted-data", "TrustedService", "data.event");

        var passResult = await filter.FilterAsync(trusted);
        Assert.That(passResult.Passed, Is.True);
        Assert.That(passResult.Reason, Is.EqualTo("Predicate matched"));

        // Non-matching message.
        var untrusted = IntegrationEnvelope<string>.Create(
            "untrusted-data", "UntrustedService", "data.event");

        var failResult = await filter.FilterAsync(untrusted);
        Assert.That(failResult.Passed, Is.False);
        Assert.That(failResult.OutputTopic, Is.EqualTo("untrusted-dlq"));
        Assert.That(failResult.Reason, Does.Contain("discard"));
    }
}
