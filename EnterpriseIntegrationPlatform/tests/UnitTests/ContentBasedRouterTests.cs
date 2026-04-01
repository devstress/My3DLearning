using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ContentBasedRouterTests
{
    private readonly IMessageBrokerProducer _producer;

    public ContentBasedRouterTests()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private ContentBasedRouter BuildRouter(RouterOptions options) =>
        new(_producer, Options.Create(options), NullLogger<ContentBasedRouter>.Instance);

    private static IntegrationEnvelope<JsonElement> BuildEnvelope(
        string messageType = "OrderCreated",
        string source = "OrderService",
        MessagePriority priority = MessagePriority.Normal,
        string payloadJson = """{"orderId":1}""",
        Dictionary<string, string>? metadata = null)
    {
        var payload = JsonDocument.Parse(payloadJson).RootElement;
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            payload,
            source: source,
            messageType: messageType);

        if (metadata is null)
            return envelope;

        // Reconstruct with metadata (records require a new instance)
        return envelope with { Metadata = metadata };
    }

    // ------------------------------------------------------------------ //
    // MessageType routing
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_MessageTypeEquals_RoutesToMatchingTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderCreated",
                    TargetTopic = "orders.created",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders.created"));
        Assert.That(decision.IsDefault, Is.False);
        Assert.That(decision.MatchedRule, Is.Not.Null);
    }

    [Test]
    public async Task RouteAsync_MessageTypeEquals_IsCaseInsensitive()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "ordercreated",
                    TargetTopic = "orders.created",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders.created"));
    }

    [Test]
    public async Task RouteAsync_MessageTypeContains_RoutesToMatchingTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Contains,
                    Value = "Order",
                    TargetTopic = "orders.topic",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders.topic"));
    }

    [Test]
    public async Task RouteAsync_MessageTypeStartsWith_RoutesToMatchingTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.StartsWith,
                    Value = "Order",
                    TargetTopic = "orders.topic",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderShipped");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders.topic"));
    }

    [Test]
    public async Task RouteAsync_MessageTypeRegex_RoutesToMatchingTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Regex,
                    Value = @"^Order(Created|Updated|Shipped)$",
                    TargetTopic = "orders.topic",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderUpdated");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders.topic"));
    }

    // ------------------------------------------------------------------ //
    // Priority ordering — first match wins
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_MultipleMatchingRules_SelectsHighestPriorityRule()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 10,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Contains,
                    Value = "Order",
                    TargetTopic = "orders.general",
                    Name = "LowPriority",
                },
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderCreated",
                    TargetTopic = "orders.created",
                    Name = "HighPriority",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders.created"));
        Assert.That(decision.MatchedRule!.Name, Is.EqualTo("HighPriority"));
    }

    // ------------------------------------------------------------------ //
    // Default topic fallback
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_NoRuleMatches_UsesDefaultTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "InvoiceCreated",
                    TargetTopic = "invoices.created",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("integration.default"));
        Assert.That(decision.IsDefault, Is.True);
        Assert.That(decision.MatchedRule, Is.Null);
    }

    [Test]
    public async Task RouteAsync_NoRuleMatchesAndNoDefaultTopic_ThrowsInvalidOperationException()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "InvoiceCreated",
                    TargetTopic = "invoices.created",
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var act = () => sut.RouteAsync(envelope);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
    }

    // ------------------------------------------------------------------ //
    // Source field routing
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_SourceEquals_RoutesToMatchingTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "Source",
                    Operator = RoutingOperator.Equals,
                    Value = "PaymentService",
                    TargetTopic = "payments.topic",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(source: "PaymentService");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("payments.topic"));
    }

    // ------------------------------------------------------------------ //
    // Metadata field routing
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_MetadataFieldEquals_RoutesToMatchingTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "Metadata.region",
                    Operator = RoutingOperator.Equals,
                    Value = "eu-west",
                    TargetTopic = "eu.topic",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(metadata: new Dictionary<string, string> { ["region"] = "eu-west" });

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("eu.topic"));
    }

    [Test]
    public async Task RouteAsync_MetadataFieldMissing_DoesNotMatch()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "Metadata.region",
                    Operator = RoutingOperator.Equals,
                    Value = "eu-west",
                    TargetTopic = "eu.topic",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(); // no Metadata

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.IsDefault, Is.True);
        Assert.That(decision.TargetTopic, Is.EqualTo("integration.default"));
    }

    // ------------------------------------------------------------------ //
    // Payload JSON field routing
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_PayloadFieldEquals_RoutesToMatchingTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "Payload.status",
                    Operator = RoutingOperator.Equals,
                    Value = "urgent",
                    TargetTopic = "orders.urgent",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(payloadJson: """{"orderId":1,"status":"urgent"}""");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders.urgent"));
    }

    [Test]
    public async Task RouteAsync_PayloadNestedFieldEquals_RoutesToMatchingTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "Payload.order.priority",
                    Operator = RoutingOperator.Equals,
                    Value = "high",
                    TargetTopic = "orders.high-priority",
                },
            ],
            DefaultTopic = "integration.default",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(payloadJson: """{"order":{"priority":"high","id":42}}""");

        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.TargetTopic, Is.EqualTo("orders.high-priority"));
    }

    // ------------------------------------------------------------------ //
    // Producer called
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_MatchingRule_PublishesToSelectedTopic()
    {
        var options = new RouterOptions
        {
            Rules =
            [
                new RoutingRule
                {
                    Priority = 1,
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderCreated",
                    TargetTopic = "orders.created",
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope();

        await sut.RouteAsync(envelope);

        await _producer.Received(1).PublishAsync(
            envelope,
            "orders.created",
            Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ //
    // Null guard
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var options = new RouterOptions { DefaultTopic = "integration.default" };
        var sut = BuildRouter(options);

        var act = () => sut.RouteAsync<string>(null!);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
}
