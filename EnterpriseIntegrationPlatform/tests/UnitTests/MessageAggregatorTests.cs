using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class MessageAggregatorTests
{
    private readonly IMessageBrokerProducer _producer;

    public MessageAggregatorTests()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private MessageAggregator<string, string> BuildAggregator(
        AggregatorOptions options,
        int expectedCount = 3,
        Func<IReadOnlyList<string>, string>? aggregateFunc = null,
        Func<IReadOnlyList<IntegrationEnvelope<string>>, bool>? completionFunc = null,
        IMessageAggregateStore<string>? store = null)
    {
        aggregateFunc ??= items => string.Join(",", items);
        var aggregationStrategy = new FuncAggregationStrategy<string, string>(aggregateFunc);

        ICompletionStrategy<string> completionStrategy = completionFunc is not null
            ? new FuncCompletionStrategy<string>(completionFunc)
            : new CountCompletionStrategy<string>(expectedCount);

        store ??= new InMemoryMessageAggregateStore<string>();

        return new MessageAggregator<string, string>(
            store,
            completionStrategy,
            aggregationStrategy,
            _producer,
            Options.Create(options),
            NullLogger<MessageAggregator<string, string>>.Instance);
    }

    private static IntegrationEnvelope<string> BuildEnvelope(
        string payload = "item",
        string messageType = "ItemCreated",
        string source = "ItemService",
        MessagePriority priority = MessagePriority.Normal,
        Dictionary<string, string>? metadata = null,
        Guid? correlationId = null)
    {
        var envelope = IntegrationEnvelope<string>.Create(
            payload,
            source: source,
            messageType: messageType,
            correlationId: correlationId) with { Priority = priority };

        return metadata is null ? envelope : envelope with { Metadata = metadata };
    }

    // ------------------------------------------------------------------ //
    // Incomplete group
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_IncompletedGroup_ReturnsIsCompleteFalse()
    {
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 3);

        var result = await sut.AggregateAsync(BuildEnvelope(payload: "a"));

        result.IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task AggregateAsync_IncompleteGroup_AggregateEnvelopeIsNull()
    {
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 3);

        var result = await sut.AggregateAsync(BuildEnvelope());

        result.AggregateEnvelope.Should().BeNull();
    }

    [Fact]
    public async Task AggregateAsync_IncompleteGroup_ReceivedCountReflectsCurrentGroupSize()
    {
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var correlationId = Guid.NewGuid();
        var sut = BuildAggregator(options, expectedCount: 5);

        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        result.ReceivedCount.Should().Be(2);
    }

    [Fact]
    public async Task AggregateAsync_IncompleteGroup_DoesNotPublish()
    {
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 3);

        await sut.AggregateAsync(BuildEnvelope());
        await sut.AggregateAsync(BuildEnvelope());

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ //
    // Complete group
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_CompleteGroup_ReturnsIsCompleteTrue()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        result.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task AggregateAsync_CompleteGroup_AggregatesPayloads()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 3,
            aggregateFunc: items => string.Join("-", items));

        await sut.AggregateAsync(BuildEnvelope(payload: "a", correlationId: correlationId));
        await sut.AggregateAsync(BuildEnvelope(payload: "b", correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(payload: "c", correlationId: correlationId));

        result.AggregateEnvelope!.Payload.Should().Be("a-b-c");
    }

    [Fact]
    public async Task AggregateAsync_CompleteGroup_PublishesToTargetTopic()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));
        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            "orders.aggregated",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AggregateAsync_CompleteGroup_ReceivedCountEqualsGroupSize()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 3);

        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));
        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        result.ReceivedCount.Should().Be(3);
    }

    // ------------------------------------------------------------------ //
    // Envelope headers on aggregate
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_CompleteGroup_PreservesCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        result.AggregateEnvelope!.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task AggregateAsync_CompleteGroup_CausationIdIsNull()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        result.AggregateEnvelope!.CausationId.Should().BeNull();
    }

    [Fact]
    public async Task AggregateAsync_CompleteGroup_AssignsNewMessageId()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        var env1 = BuildEnvelope(correlationId: correlationId);
        var env2 = BuildEnvelope(correlationId: correlationId);
        await sut.AggregateAsync(env1);
        var result = await sut.AggregateAsync(env2);

        result.AggregateEnvelope!.MessageId.Should().NotBe(env1.MessageId);
        result.AggregateEnvelope.MessageId.Should().NotBe(env2.MessageId);
    }

    [Fact]
    public async Task AggregateAsync_CompleteGroup_UsesHighestPriority()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(
            priority: MessagePriority.Normal, correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(
            priority: MessagePriority.High, correlationId: correlationId));

        result.AggregateEnvelope!.Priority.Should().Be(MessagePriority.High);
    }

    [Fact]
    public async Task AggregateAsync_CompleteGroup_MergesMetadataFromAllEnvelopes()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(
            metadata: new Dictionary<string, string> { ["key1"] = "value1" },
            correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(
            metadata: new Dictionary<string, string> { ["key2"] = "value2" },
            correlationId: correlationId));

        result.AggregateEnvelope!.Metadata.Should().ContainKey("key1");
        result.AggregateEnvelope.Metadata.Should().ContainKey("key2");
    }

    // ------------------------------------------------------------------ //
    // MessageType overriding
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_OverridesMessageType_WhenTargetMessageTypeConfigured()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions
        {
            TargetTopic = "orders.aggregated",
            TargetMessageType = "OrderAggregated",
        };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(
            messageType: "ItemCreated", correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(
            messageType: "ItemCreated", correlationId: correlationId));

        result.AggregateEnvelope!.MessageType.Should().Be("OrderAggregated");
    }

    [Fact]
    public async Task AggregateAsync_PreservesMessageType_WhenTargetMessageTypeNotConfigured()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(
            messageType: "ItemCreated", correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(
            messageType: "ItemCreated", correlationId: correlationId));

        result.AggregateEnvelope!.MessageType.Should().Be("ItemCreated");
    }

    // ------------------------------------------------------------------ //
    // Source overriding
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_OverridesSource_WhenTargetSourceConfigured()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions
        {
            TargetTopic = "orders.aggregated",
            TargetSource = "Aggregator",
        };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(
            source: "ItemService", correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(
            source: "ItemService", correlationId: correlationId));

        result.AggregateEnvelope!.Source.Should().Be("Aggregator");
    }

    [Fact]
    public async Task AggregateAsync_PreservesSource_WhenTargetSourceNotConfigured()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(
            source: "ItemService", correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(
            source: "ItemService", correlationId: correlationId));

        result.AggregateEnvelope!.Source.Should().Be("ItemService");
    }

    // ------------------------------------------------------------------ //
    // Guard clauses
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 3);

        var act = () => sut.AggregateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AggregateAsync_EmptyTargetTopic_ThrowsInvalidOperationException()
    {
        var options = new AggregatorOptions { TargetTopic = string.Empty };
        var sut = BuildAggregator(options, expectedCount: 3);

        var act = () => sut.AggregateAsync(BuildEnvelope());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ------------------------------------------------------------------ //
    // Group isolation between different correlation IDs
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_TwoCorrelationGroups_AreProcessedIndependently()
    {
        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        await sut.AggregateAsync(BuildEnvelope(payload: "a", correlationId: corr1));
        await sut.AggregateAsync(BuildEnvelope(payload: "b", correlationId: corr2));

        var result1 = await sut.AggregateAsync(BuildEnvelope(payload: "c", correlationId: corr1));
        var result2 = await sut.AggregateAsync(BuildEnvelope(payload: "d", correlationId: corr2));

        result1.IsComplete.Should().BeTrue();
        result2.IsComplete.Should().BeTrue();
        result1.AggregateEnvelope!.Payload.Should().Be("a,c");
        result2.AggregateEnvelope!.Payload.Should().Be("b,d");
    }

    // ------------------------------------------------------------------ //
    // Group cleared after aggregation
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_AfterGroupComplete_GroupIsCleared_NextMessageStartsFreshGroup()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 2);

        // First complete cycle
        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));
        await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        // Second cycle with same correlationId — group should start fresh
        var result = await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        result.IsComplete.Should().BeFalse();
        result.ReceivedCount.Should().Be(1);
    }

    // ------------------------------------------------------------------ //
    // Custom completion predicate
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_WithFuncCompletionStrategy_UsesCustomPredicate()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };

        // Complete when total payload length exceeds 5 characters
        var sut = BuildAggregator(options,
            completionFunc: group => group.Sum(e => e.Payload.Length) > 5);

        await sut.AggregateAsync(BuildEnvelope(payload: "ab", correlationId: correlationId));
        await sut.AggregateAsync(BuildEnvelope(payload: "cd", correlationId: correlationId));
        var result = await sut.AggregateAsync(BuildEnvelope(payload: "ef", correlationId: correlationId));

        result.IsComplete.Should().BeTrue();
    }

    // ------------------------------------------------------------------ //
    // CorrelationId on result
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task AggregateAsync_IncompleteResult_CorrelationIdMatchesEnvelope()
    {
        var correlationId = Guid.NewGuid();
        var options = new AggregatorOptions { TargetTopic = "orders.aggregated" };
        var sut = BuildAggregator(options, expectedCount: 3);

        var result = await sut.AggregateAsync(BuildEnvelope(correlationId: correlationId));

        result.CorrelationId.Should().Be(correlationId);
    }
}
