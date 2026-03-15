using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class MessageTranslatorTests
{
    private readonly MessageTranslator _sut = new(NullLogger<MessageTranslator>.Instance);

    private static IntegrationEnvelope<string> BuildStringEnvelope(
        string payload = "hello",
        string messageType = "TestMessage",
        string source = "TestService") =>
        IntegrationEnvelope<string>.Create(payload, source, messageType);

    private static IntegrationEnvelope<JsonElement> BuildJsonEnvelope(
        string payloadJson = """{"key":"value"}""") =>
        IntegrationEnvelope<JsonElement>.Create(
            JsonDocument.Parse(payloadJson).RootElement,
            source: "TestService",
            messageType: "TestMessage");

    // ------------------------------------------------------------------ //
    // Null guard
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var converter = Substitute.For<IPayloadConverter<string, string>>();

        var act = () => _sut.TranslateAsync<string, string>(null!, converter);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("envelope");
    }

    [Fact]
    public async Task TranslateAsync_NullConverter_ThrowsArgumentNullException()
    {
        var envelope = BuildStringEnvelope();

        var act = () => _sut.TranslateAsync<string, string>(envelope, null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("converter");
    }

    // ------------------------------------------------------------------ //
    // Message lineage preservation
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_PreservesCorrelationId()
    {
        var envelope = BuildStringEnvelope();
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("translated");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.CorrelationId.Should().Be(envelope.CorrelationId);
    }

    [Fact]
    public async Task TranslateAsync_SetsCausationIdToSourceMessageId()
    {
        var envelope = BuildStringEnvelope();
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("translated");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.CausationId.Should().Be(envelope.MessageId);
    }

    [Fact]
    public async Task TranslateAsync_GeneratesNewMessageId()
    {
        var envelope = BuildStringEnvelope();
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("translated");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.MessageId.Should().NotBe(envelope.MessageId);
        result.MessageId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task TranslateAsync_PreservesSource()
    {
        var envelope = BuildStringEnvelope(source: "OriginalService");
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("translated");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.Source.Should().Be("OriginalService");
    }

    [Fact]
    public async Task TranslateAsync_PreservesMessageType()
    {
        var envelope = BuildStringEnvelope(messageType: "OrderCreated");
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("translated");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.MessageType.Should().Be("OrderCreated");
    }

    [Fact]
    public async Task TranslateAsync_PreservesMetadata()
    {
        var envelope = BuildStringEnvelope() with
        {
            Metadata = new Dictionary<string, string> { ["region"] = "eu-west", ["tenant"] = "acme" },
        };
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("translated");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.Metadata.Should().ContainKey("region").WhoseValue.Should().Be("eu-west");
        result.Metadata.Should().ContainKey("tenant").WhoseValue.Should().Be("acme");
    }

    [Fact]
    public async Task TranslateAsync_PreservesPriority()
    {
        var envelope = BuildStringEnvelope() with { Priority = MessagePriority.High };
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("translated");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.Priority.Should().Be(MessagePriority.High);
    }

    // ------------------------------------------------------------------ //
    // Payload conversion
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task TranslateAsync_InvokesConverterWithSourcePayload()
    {
        var envelope = BuildStringEnvelope(payload: "raw-input");
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync("raw-input", Arg.Any<CancellationToken>())
            .Returns("converted-output");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.Payload.Should().Be("converted-output");
    }

    [Fact]
    public async Task TranslateAsync_ConverterCalledExactlyOnce()
    {
        var envelope = BuildStringEnvelope();
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("x");

        await _sut.TranslateAsync(envelope, converter);

        await converter.Received(1).ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateAsync_ConverterThrows_PropagatesException()
    {
        var envelope = BuildStringEnvelope();
        var converter = Substitute.For<IPayloadConverter<string, string>>();
        converter.ConvertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new FormatException("bad input"));

        var act = () => _sut.TranslateAsync(envelope, converter);

        await act.Should().ThrowAsync<FormatException>().WithMessage("bad input");
    }

    [Fact]
    public async Task TranslateAsync_TypeConversion_StringToJsonElement()
    {
        var envelope = BuildJsonEnvelope("""{"status":"ok"}""");
        var converter = Substitute.For<IPayloadConverter<JsonElement, string>>();
        converter.ConvertAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns("<root><status>ok</status></root>");

        var result = await _sut.TranslateAsync(envelope, converter);

        result.Payload.Should().Be("<root><status>ok</status></root>");
    }
}
