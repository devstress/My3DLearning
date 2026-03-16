using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class MessageSplitterTests
{
    private readonly IMessageBrokerProducer _producer;

    public MessageSplitterTests()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private MessageSplitter<string> BuildSplitter(
        SplitterOptions options,
        Func<string, IReadOnlyList<string>>? splitFunc = null)
    {
        var strategy = new FuncSplitStrategy<string>(
            splitFunc ?? (s => s.Split(',').ToList()));

        return new MessageSplitter<string>(
            strategy,
            _producer,
            Options.Create(options),
            NullLogger<MessageSplitter<string>>.Instance);
    }

    private static IntegrationEnvelope<string> BuildEnvelope(
        string payload = "a,b,c",
        string messageType = "BatchCreated",
        string source = "BatchService",
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
    // Payload splitting
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task SplitAsync_WithFuncStrategy_SplitsPayloadIntoItems()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => s.Split(',').ToList());
        var envelope = BuildEnvelope(payload: "x,y,z");

        var result = await sut.SplitAsync(envelope);

        result.ItemCount.Should().Be(3);
        result.SplitEnvelopes.Should().HaveCount(3);
        result.SplitEnvelopes[0].Payload.Should().Be("x");
        result.SplitEnvelopes[1].Payload.Should().Be("y");
        result.SplitEnvelopes[2].Payload.Should().Be("z");
    }

    [Fact]
    public async Task SplitAsync_SingleItem_ProducesOneEnvelope()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(payload: "single");

        var result = await sut.SplitAsync(envelope);

        result.ItemCount.Should().Be(1);
        result.SplitEnvelopes.Should().HaveCount(1);
        result.SplitEnvelopes[0].Payload.Should().Be("single");
    }

    [Fact]
    public async Task SplitAsync_EmptyResult_ReturnsZeroItems()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, _ => []);
        var envelope = BuildEnvelope();

        var result = await sut.SplitAsync(envelope);

        result.ItemCount.Should().Be(0);
        result.SplitEnvelopes.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ //
    // Envelope header propagation
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task SplitAsync_PreservesCorrelationId_FromSource()
    {
        var correlationId = Guid.NewGuid();
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(correlationId: correlationId);

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task SplitAsync_SetsCausationId_ToSourceMessageId()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope();

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].CausationId.Should().Be(envelope.MessageId);
    }

    [Fact]
    public async Task SplitAsync_AssignsNewMessageId_ToEachSplitEnvelope()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => s.Split(',').ToList());
        var envelope = BuildEnvelope(payload: "a,b");

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].MessageId.Should().NotBe(envelope.MessageId);
        result.SplitEnvelopes[1].MessageId.Should().NotBe(envelope.MessageId);
        result.SplitEnvelopes[0].MessageId.Should().NotBe(result.SplitEnvelopes[1].MessageId);
    }

    [Fact]
    public async Task SplitAsync_PreservesMetadata_FromSource()
    {
        var metadata = new Dictionary<string, string> { ["region"] = "eu-west" };
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(metadata: metadata);

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].Metadata.Should().ContainKey("region")
            .WhoseValue.Should().Be("eu-west");
    }

    [Fact]
    public async Task SplitAsync_PreservesPriority_FromSource()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(priority: MessagePriority.High);

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].Priority.Should().Be(MessagePriority.High);
    }

    [Fact]
    public async Task SplitAsync_PreservesSchemaVersion_FromSource()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope() with { SchemaVersion = "2.0" };

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].SchemaVersion.Should().Be("2.0");
    }

    // ------------------------------------------------------------------ //
    // MessageType overriding
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task SplitAsync_OverridesMessageType_WhenTargetMessageTypeConfigured()
    {
        var options = new SplitterOptions
        {
            TargetTopic = "items.split",
            TargetMessageType = "ItemSplit",
        };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(messageType: "BatchCreated");

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].MessageType.Should().Be("ItemSplit");
    }

    [Fact]
    public async Task SplitAsync_PreservesMessageType_WhenTargetMessageTypeNotConfigured()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(messageType: "BatchCreated");

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].MessageType.Should().Be("BatchCreated");
    }

    // ------------------------------------------------------------------ //
    // Source overriding
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task SplitAsync_OverridesSource_WhenTargetSourceConfigured()
    {
        var options = new SplitterOptions
        {
            TargetTopic = "items.split",
            TargetSource = "Splitter",
        };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(source: "BatchService");

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].Source.Should().Be("Splitter");
    }

    [Fact]
    public async Task SplitAsync_PreservesSource_WhenTargetSourceNotConfigured()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(source: "BatchService");

        var result = await sut.SplitAsync(envelope);

        result.SplitEnvelopes[0].Source.Should().Be("BatchService");
    }

    // ------------------------------------------------------------------ //
    // Broker publish
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task SplitAsync_PublishesEachItemToTargetTopic()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => s.Split(',').ToList());
        var envelope = BuildEnvelope(payload: "a,b,c");

        await sut.SplitAsync(envelope);

        await _producer.Received(3).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            "items.split",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SplitAsync_EmptyResult_DoesNotPublish()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, _ => []);
        var envelope = BuildEnvelope();

        await sut.SplitAsync(envelope);

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ //
    // Result record
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task SplitAsync_ReturnsResult_WithSourceMessageId()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope();

        var result = await sut.SplitAsync(envelope);

        result.SourceMessageId.Should().Be(envelope.MessageId);
    }

    [Fact]
    public async Task SplitAsync_ReturnsResult_WithTargetTopic()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope();

        var result = await sut.SplitAsync(envelope);

        result.TargetTopic.Should().Be("items.split");
    }

    // ------------------------------------------------------------------ //
    // Guard clauses
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task SplitAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options);

        var act = () => sut.SplitAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SplitAsync_EmptyTargetTopic_ThrowsInvalidOperationException()
    {
        var options = new SplitterOptions { TargetTopic = string.Empty };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope();

        var act = () => sut.SplitAsync(envelope);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ------------------------------------------------------------------ //
    // Metadata isolation between split envelopes
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task SplitAsync_MetadataIsCopied_NotSharedBetweenSplitEnvelopes()
    {
        var metadata = new Dictionary<string, string> { ["key"] = "value" };
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => s.Split(',').ToList());
        var envelope = BuildEnvelope(payload: "a,b", metadata: metadata);

        var result = await sut.SplitAsync(envelope);

        // Mutate one split envelope's metadata
        result.SplitEnvelopes[0].Metadata["extra"] = "added";

        // The other split envelope should not be affected
        result.SplitEnvelopes[1].Metadata.Should().NotContainKey("extra");
    }
}
