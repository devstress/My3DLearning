using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Translator;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class MessageTranslatorTests
{
    private readonly IMessageBrokerProducer _producer;

    public MessageTranslatorTests()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private MessageTranslator<string, string> BuildTranslator(
        TranslatorOptions options,
        Func<string, string>? transformFunc = null)
    {
        var transform = new FuncPayloadTransform<string, string>(
            transformFunc ?? (s => s.ToUpperInvariant()));

        return new MessageTranslator<string, string>(
            transform,
            _producer,
            Options.Create(options),
            NullLogger<MessageTranslator<string, string>>.Instance);
    }

    private static IntegrationEnvelope<string> BuildEnvelope(
        string payload = "hello",
        string messageType = "OrderCreated",
        string source = "OrderService",
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
    // Payload transformation
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_WithFuncTransform_TranslatesPayload()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options, s => s.ToUpperInvariant());
        var envelope = BuildEnvelope(payload: "hello");

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.Payload.Should().Be("HELLO");
    }

    // ------------------------------------------------------------------ //
    // Envelope header propagation
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_PreservesCorrelationId_FromSource()
    {
        var correlationId = Guid.NewGuid();
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope(correlationId: correlationId);

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task TranslateAsync_SetsCausationId_ToSourceMessageId()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope();

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.CausationId.Should().Be(envelope.MessageId);
    }

    [Fact]
    public async Task TranslateAsync_AssignsNewMessageId_ToTranslatedEnvelope()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope();

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.MessageId.Should().NotBe(envelope.MessageId);
    }

    [Fact]
    public async Task TranslateAsync_PreservesMetadata_FromSource()
    {
        var metadata = new Dictionary<string, string> { ["region"] = "eu-west" };
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope(metadata: metadata);

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.Metadata.Should().ContainKey("region")
            .WhoseValue.Should().Be("eu-west");
    }

    [Fact]
    public async Task TranslateAsync_PreservesPriority_FromSource()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope(priority: MessagePriority.High);

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.Priority.Should().Be(MessagePriority.High);
    }

    // ------------------------------------------------------------------ //
    // MessageType overriding
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_OverridesMessageType_WhenTargetMessageTypeConfigured()
    {
        var options = new TranslatorOptions
        {
            TargetTopic = "orders.translated",
            TargetMessageType = "OrderV2",
        };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.MessageType.Should().Be("OrderV2");
    }

    [Fact]
    public async Task TranslateAsync_PreservesMessageType_WhenTargetMessageTypeNotConfigured()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope(messageType: "OrderCreated");

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.MessageType.Should().Be("OrderCreated");
    }

    // ------------------------------------------------------------------ //
    // Source overriding
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_OverridesSource_WhenTargetSourceConfigured()
    {
        var options = new TranslatorOptions
        {
            TargetTopic = "orders.translated",
            TargetSource = "Translator",
        };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope(source: "OrderService");

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.Source.Should().Be("Translator");
    }

    [Fact]
    public async Task TranslateAsync_PreservesSource_WhenTargetSourceNotConfigured()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope(source: "OrderService");

        var result = await sut.TranslateAsync(envelope);

        result.TranslatedEnvelope.Source.Should().Be("OrderService");
    }

    // ------------------------------------------------------------------ //
    // Broker publish
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_PublishesToTargetTopic()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope();

        var result = await sut.TranslateAsync(envelope);

        await _producer.Received(1).PublishAsync(
            result.TranslatedEnvelope,
            "orders.translated",
            Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ //
    // Result record
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_ReturnsResult_WithSourceMessageId()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope();

        var result = await sut.TranslateAsync(envelope);

        result.SourceMessageId.Should().Be(envelope.MessageId);
    }

    [Fact]
    public async Task TranslateAsync_ReturnsResult_WithTargetTopic()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope();

        var result = await sut.TranslateAsync(envelope);

        result.TargetTopic.Should().Be("orders.translated");
    }

    // ------------------------------------------------------------------ //
    // Guard clauses
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };
        var sut = BuildTranslator(options);

        var act = () => sut.TranslateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TranslateAsync_EmptyTargetTopic_ThrowsInvalidOperationException()
    {
        var options = new TranslatorOptions { TargetTopic = string.Empty };
        var sut = BuildTranslator(options);
        var envelope = BuildEnvelope();

        var act = () => sut.TranslateAsync(envelope);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
