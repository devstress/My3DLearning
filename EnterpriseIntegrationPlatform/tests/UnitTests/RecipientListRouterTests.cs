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
public class RecipientListRouterTests
{
    private IMessageBrokerProducer _producer = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private RecipientListRouter BuildRouter(RecipientListOptions? options = null) =>
        new(
            _producer,
            Options.Create(options ?? new RecipientListOptions()),
            NullLogger<RecipientListRouter>.Instance);

    private static IntegrationEnvelope<JsonElement> BuildEnvelope(
        string messageType = "OrderCreated",
        string source = "OrderService",
        string payloadJson = """{"orderId":1}""",
        Dictionary<string, string>? metadata = null)
    {
        var payload = JsonDocument.Parse(payloadJson).RootElement;
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            payload,
            source: source,
            messageType: messageType);

        return metadata is null ? envelope : envelope with { Metadata = metadata };
    }

    // ------------------------------------------------------------------ //
    // Resolve multiple recipients from rules
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_SingleRuleWithMultipleDestinations_PublishesToAll()
    {
        var options = new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderCreated",
                    Destinations = ["orders.topic-a", "orders.topic-b", "orders.topic-c"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope();

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.Destinations, Is.EquivalentTo(new[] { "orders.topic-a", "orders.topic-b", "orders.topic-c" }));

        await _producer.Received(1).PublishAsync(envelope, "orders.topic-a", Arg.Any<CancellationToken>());
        await _producer.Received(1).PublishAsync(envelope, "orders.topic-b", Arg.Any<CancellationToken>());
        await _producer.Received(1).PublishAsync(envelope, "orders.topic-c", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RouteAsync_MultipleMatchingRules_AggregatesAllDestinations()
    {
        var options = new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderCreated",
                    Destinations = ["orders.topic-a"],
                },
                new RecipientListRule
                {
                    FieldName = "Source",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderService",
                    Destinations = ["audit.topic"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope();

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(2));
        Assert.That(result.Destinations, Is.EquivalentTo(new[] { "orders.topic-a", "audit.topic" }));
    }

    // ------------------------------------------------------------------ //
    // Empty list handling
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_NoMatchingRules_ReturnsEmptyList()
    {
        var options = new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "InvoiceCreated",
                    Destinations = ["invoices.topic"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(0));
        Assert.That(result.Destinations, Is.Empty);

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<JsonElement>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RouteAsync_NoRulesConfigured_ReturnsEmptyList()
    {
        var sut = BuildRouter(new RecipientListOptions());
        var envelope = BuildEnvelope();

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(0));
        Assert.That(result.Destinations, Is.Empty);
    }

    // ------------------------------------------------------------------ //
    // Deduplication
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_DuplicateDestinationsFromRules_DeduplicatesAndReportsCount()
    {
        var options = new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Contains,
                    Value = "Order",
                    Destinations = ["orders.topic", "audit.topic"],
                },
                new RecipientListRule
                {
                    FieldName = "Source",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderService",
                    Destinations = ["orders.topic", "notifications.topic"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope();

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.DuplicatesRemoved, Is.EqualTo(1));
        Assert.That(result.Destinations, Is.EquivalentTo(
            new[] { "orders.topic", "audit.topic", "notifications.topic" }));

        await _producer.Received(1).PublishAsync(envelope, "orders.topic", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RouteAsync_DuplicatesCaseInsensitive_DeduplicatesProperly()
    {
        var options = new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderCreated",
                    Destinations = ["Orders.Topic", "orders.topic"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope();

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(1));
        Assert.That(result.DuplicatesRemoved, Is.EqualTo(1));
    }

    // ------------------------------------------------------------------ //
    // Metadata-based resolution
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_MetadataRecipientsKey_AddsMetadataDestinations()
    {
        var options = new RecipientListOptions
        {
            MetadataRecipientsKey = "recipients",
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(metadata: new Dictionary<string, string>
        {
            ["recipients"] = "topic-a, topic-b, topic-c",
        });

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(3));
        Assert.That(result.Destinations, Is.EquivalentTo(new[] { "topic-a", "topic-b", "topic-c" }));
    }

    [Test]
    public async Task RouteAsync_MetadataRecipientsKeyMissing_OnlyUsesRules()
    {
        var options = new RecipientListOptions
        {
            MetadataRecipientsKey = "recipients",
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderCreated",
                    Destinations = ["orders.topic"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(); // no metadata

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(1));
        Assert.That(result.Destinations, Is.EquivalentTo(new[] { "orders.topic" }));
    }

    [Test]
    public async Task RouteAsync_MetadataAndRulesCombined_MergesAndDeduplicates()
    {
        var options = new RecipientListOptions
        {
            MetadataRecipientsKey = "recipients",
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Equals,
                    Value = "OrderCreated",
                    Destinations = ["orders.topic"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(metadata: new Dictionary<string, string>
        {
            ["recipients"] = "orders.topic, extra.topic",
        });

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(2));
        Assert.That(result.DuplicatesRemoved, Is.EqualTo(1));
        Assert.That(result.Destinations, Is.EquivalentTo(new[] { "orders.topic", "extra.topic" }));
    }

    // ------------------------------------------------------------------ //
    // Regex rule
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_RegexRule_MatchesAndPublishes()
    {
        var options = new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "MessageType",
                    Operator = RoutingOperator.Regex,
                    Value = @"^Order(Created|Updated)$",
                    Destinations = ["orders.all"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(messageType: "OrderUpdated");

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(1));
        Assert.That(result.Destinations, Contains.Item("orders.all"));
    }

    // ------------------------------------------------------------------ //
    // Null guard
    // ------------------------------------------------------------------ //

    [Test]
    public void RouteAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var sut = BuildRouter();

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.RouteAsync<string>(null!));
    }

    // ------------------------------------------------------------------ //
    // Metadata field-based rule
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_MetadataFieldRule_ResolvesDestinations()
    {
        var options = new RecipientListOptions
        {
            Rules =
            [
                new RecipientListRule
                {
                    FieldName = "Metadata.region",
                    Operator = RoutingOperator.Equals,
                    Value = "eu-west",
                    Destinations = ["eu.orders", "eu.audit"],
                },
            ],
        };
        var sut = BuildRouter(options);
        var envelope = BuildEnvelope(metadata: new Dictionary<string, string>
        {
            ["region"] = "eu-west",
        });

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.ResolvedCount, Is.EqualTo(2));
        Assert.That(result.Destinations, Is.EquivalentTo(new[] { "eu.orders", "eu.audit" }));
    }
}
