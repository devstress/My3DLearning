using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class MessageFilterTests
{
    private IMessageBrokerProducer _producer = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private MessageFilter CreateFilter(MessageFilterOptions options) =>
        new(
            _producer,
            Options.Create(options),
            NullLogger<MessageFilter>.Instance);

    private static IntegrationEnvelope<JsonElement> CreateEnvelope(
        string messageType = "OrderCreated",
        string source = "Gateway",
        MessagePriority priority = MessagePriority.Normal,
        Dictionary<string, string>? metadata = null,
        string payloadJson = """{"orderId":42,"region":"US"}""")
    {
        var json = JsonDocument.Parse(payloadJson).RootElement.Clone();
        return new IntegrationEnvelope<JsonElement>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = source,
            MessageType = messageType,
            Priority = priority,
            Payload = json,
            Metadata = metadata ?? new Dictionary<string, string>(),
        };
    }

    [Test]
    public async Task FilterAsync_NoConditions_PassesThroughToOutputTopic()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions = [],
            OutputTopic = "output.topic",
        });

        var envelope = CreateEnvelope();
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
        Assert.That(result.OutputTopic, Is.EqualTo("output.topic"));
        await _producer.Received(1).PublishAsync(envelope, "output.topic", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FilterAsync_MatchingCondition_PassesThroughToOutputTopic()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" },
            ],
            OutputTopic = "orders.output",
        });

        var envelope = CreateEnvelope();
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
        Assert.That(result.OutputTopic, Is.EqualTo("orders.output"));
        await _producer.Received(1).PublishAsync(envelope, "orders.output", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FilterAsync_NonMatchingCondition_SilentlyDiscarded()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "ShipmentCreated" },
            ],
            OutputTopic = "orders.output",
            DiscardTopic = null,
        });

        var envelope = CreateEnvelope(messageType: "OrderCreated");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.OutputTopic, Is.Null);
        Assert.That(result.Reason, Does.Contain("silently discarded"));
        await _producer.DidNotReceiveWithAnyArgs()
            .PublishAsync<JsonElement>(default!, default!, default);
    }

    [Test]
    public async Task FilterAsync_NonMatchingCondition_RoutedToDiscardTopic()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "ShipmentCreated" },
            ],
            OutputTopic = "orders.output",
            DiscardTopic = "orders.dlq",
        });

        var envelope = CreateEnvelope(messageType: "OrderCreated");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
        Assert.That(result.OutputTopic, Is.EqualTo("orders.dlq"));
        Assert.That(result.Reason, Does.Contain("discard topic"));
        await _producer.Received(1).PublishAsync(envelope, "orders.dlq", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FilterAsync_AndLogic_AllConditionsMustMatch()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "Gateway" },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(messageType: "OrderCreated", source: "Gateway");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public async Task FilterAsync_AndLogic_OneConditionFails_Discards()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "ExternalApi" },
            ],
            Logic = RuleLogicOperator.And,
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(messageType: "OrderCreated", source: "Gateway");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.False);
    }

    [Test]
    public async Task FilterAsync_OrLogic_AnyConditionSuffices()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "ShipmentCreated" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "Gateway" },
            ],
            Logic = RuleLogicOperator.Or,
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(messageType: "OrderCreated", source: "Gateway");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public async Task FilterAsync_RegexCondition_MatchesPattern()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Regex, Value = "^Order.*" },
            ],
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(messageType: "OrderCreated");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public async Task FilterAsync_ContainsCondition_MatchesSubstring()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "Order" },
            ],
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(messageType: "OrderCreated");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public async Task FilterAsync_InCondition_MatchesOneOfValues()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.In, Value = "ShipmentCreated,OrderCreated,InvoiceCreated" },
            ],
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(messageType: "OrderCreated");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public async Task FilterAsync_GreaterThanCondition_ComparesNumerically()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "Payload.orderId", Operator = RuleConditionOperator.GreaterThan, Value = "10" },
            ],
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(payloadJson: """{"orderId":42,"region":"US"}""");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public async Task FilterAsync_MetadataField_EvaluatesCorrectly()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "Metadata.tenant", Operator = RuleConditionOperator.Equals, Value = "acme" },
            ],
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(metadata: new Dictionary<string, string> { ["tenant"] = "acme" });
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public async Task FilterAsync_PayloadJsonField_ExtractsAndEvaluates()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            Conditions =
            [
                new RuleCondition { FieldName = "Payload.region", Operator = RuleConditionOperator.Equals, Value = "US" },
            ],
            OutputTopic = "output",
        });

        var envelope = CreateEnvelope(payloadJson: """{"orderId":42,"region":"US"}""");
        var result = await filter.FilterAsync(envelope);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public void FilterAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var filter = CreateFilter(new MessageFilterOptions
        {
            OutputTopic = "output",
        });

        Assert.ThrowsAsync<ArgumentNullException>(
            () => filter.FilterAsync<string>(null!));
    }
}
