using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
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

    [Test]
    public async Task SplitAsync_WithFuncStrategy_SplitsPayloadIntoItems()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => s.Split(',').ToList());
        var envelope = BuildEnvelope(payload: "x,y,z");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.ItemCount, Is.EqualTo(3));
        Assert.That(result.SplitEnvelopes, Has.Count.EqualTo(3));
        Assert.That(result.SplitEnvelopes[0].Payload, Is.EqualTo("x"));
        Assert.That(result.SplitEnvelopes[1].Payload, Is.EqualTo("y"));
        Assert.That(result.SplitEnvelopes[2].Payload, Is.EqualTo("z"));
    }

    [Test]
    public async Task SplitAsync_SingleItem_ProducesOneEnvelope()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(payload: "single");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.ItemCount, Is.EqualTo(1));
        Assert.That(result.SplitEnvelopes, Has.Count.EqualTo(1));
        Assert.That(result.SplitEnvelopes[0].Payload, Is.EqualTo("single"));
    }

    [Test]
    public async Task SplitAsync_EmptyResult_ReturnsZeroItems()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, _ => []);
        var envelope = BuildEnvelope();

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.ItemCount, Is.EqualTo(0));
        Assert.That(result.SplitEnvelopes, Is.Empty);
    }

    // ------------------------------------------------------------------ //
    // Envelope header propagation
    // ------------------------------------------------------------------ //

    [Test]
    public async Task SplitAsync_PreservesCorrelationId_FromSource()
    {
        var correlationId = Guid.NewGuid();
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(correlationId: correlationId);

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task SplitAsync_SetsCausationId_ToSourceMessageId()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope();

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].CausationId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task SplitAsync_AssignsNewMessageId_ToEachSplitEnvelope()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => s.Split(',').ToList());
        var envelope = BuildEnvelope(payload: "a,b");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].MessageId, Is.Not.EqualTo(envelope.MessageId));
        Assert.That(result.SplitEnvelopes[1].MessageId, Is.Not.EqualTo(envelope.MessageId));
        Assert.That(result.SplitEnvelopes[0].MessageId, Is.Not.EqualTo(result.SplitEnvelopes[1].MessageId));
    }

    [Test]
    public async Task SplitAsync_PreservesMetadata_FromSource()
    {
        var metadata = new Dictionary<string, string> { ["region"] = "eu-west" };
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(metadata: metadata);

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].Metadata.ContainsKey("region"), Is.True);
        Assert.That(result.SplitEnvelopes[0].Metadata["region"], Is.EqualTo("eu-west"));
    }

    [Test]
    public async Task SplitAsync_PreservesPriority_FromSource()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(priority: MessagePriority.High);

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].Priority, Is.EqualTo(MessagePriority.High));
    }

    [Test]
    public async Task SplitAsync_PreservesSchemaVersion_FromSource()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope() with { SchemaVersion = "2.0" };

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].SchemaVersion, Is.EqualTo("2.0"));
    }

    // ------------------------------------------------------------------ //
    // MessageType overriding
    // ------------------------------------------------------------------ //

    [Test]
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

        Assert.That(result.SplitEnvelopes[0].MessageType, Is.EqualTo("ItemSplit"));
    }

    [Test]
    public async Task SplitAsync_PreservesMessageType_WhenTargetMessageTypeNotConfigured()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(messageType: "BatchCreated");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].MessageType, Is.EqualTo("BatchCreated"));
    }

    // ------------------------------------------------------------------ //
    // Source overriding
    // ------------------------------------------------------------------ //

    [Test]
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

        Assert.That(result.SplitEnvelopes[0].Source, Is.EqualTo("Splitter"));
    }

    [Test]
    public async Task SplitAsync_PreservesSource_WhenTargetSourceNotConfigured()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope(source: "BatchService");

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SplitEnvelopes[0].Source, Is.EqualTo("BatchService"));
    }

    // ------------------------------------------------------------------ //
    // Broker publish
    // ------------------------------------------------------------------ //

    [Test]
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

    [Test]
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

    [Test]
    public async Task SplitAsync_ReturnsResult_WithSourceMessageId()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope();

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.SourceMessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task SplitAsync_ReturnsResult_WithTargetTopic()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope();

        var result = await sut.SplitAsync(envelope);

        Assert.That(result.TargetTopic, Is.EqualTo("items.split"));
    }

    // ------------------------------------------------------------------ //
    // Guard clauses
    // ------------------------------------------------------------------ //

    [Test]
    public async Task SplitAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };
        var sut = BuildSplitter(options);

        var act = () => sut.SplitAsync(null!);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }

    [Test]
    public async Task SplitAsync_EmptyTargetTopic_ThrowsInvalidOperationException()
    {
        var options = new SplitterOptions { TargetTopic = string.Empty };
        var sut = BuildSplitter(options, s => [s]);
        var envelope = BuildEnvelope();

        var act = () => sut.SplitAsync(envelope);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
    }

    // ------------------------------------------------------------------ //
    // Metadata isolation between split envelopes
    // ------------------------------------------------------------------ //

    [Test]
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
        Assert.That(result.SplitEnvelopes[1].Metadata.ContainsKey("extra"), Is.False);
    }
}
