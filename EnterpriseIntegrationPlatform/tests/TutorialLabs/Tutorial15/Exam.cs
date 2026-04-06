// ============================================================================
// Tutorial 15 – Message Translator (Exam)
// ============================================================================
// E2E challenges: type-converting translator, metadata preservation chain,
// and multi-field transformation verification via MockEndpoint.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Translator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial15;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_TypeConversion_StringToInt()
    {
        await using var output = new MockEndpoint("type-convert");
        var transform = Substitute.For<IPayloadTransform<string, int>>();
        transform.Transform("42").Returns(42);

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "int-topic",
            TargetMessageType = "number.parsed",
        });
        var translator = new MessageTranslator<string, int>(
            transform, output, options,
            NullLogger<MessageTranslator<string, int>>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "42", "parser-svc", "string.input");
        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.Payload, Is.EqualTo(42));
        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("number.parsed"));
        Assert.That(result.TranslatedEnvelope.CausationId, Is.EqualTo(envelope.MessageId));
        output.AssertReceivedOnTopic("int-topic", 1);
    }

    [Test]
    public async Task Challenge2_MetadataPreservationChain_TwoTranslations()
    {
        await using var output1 = new MockEndpoint("stage1");
        await using var output2 = new MockEndpoint("stage2");

        var transform1 = Substitute.For<IPayloadTransform<string, string>>();
        transform1.Transform(Arg.Any<string>()).Returns(x => ((string)x[0]).ToUpperInvariant());

        var transform2 = Substitute.For<IPayloadTransform<string, string>>();
        transform2.Transform(Arg.Any<string>()).Returns(x => $"[{x[0]}]");

        var translator1 = new MessageTranslator<string, string>(
            transform1, output1, Options.Create(new TranslatorOptions { TargetTopic = "stage1-topic" }),
            NullLogger<MessageTranslator<string, string>>.Instance);

        var translator2 = new MessageTranslator<string, string>(
            transform2, output2, Options.Create(new TranslatorOptions { TargetTopic = "stage2-topic" }),
            NullLogger<MessageTranslator<string, string>>.Instance);

        var original = IntegrationEnvelope<string>.Create(
            "hello", "origin", "raw.text") with
        {
            Metadata = new Dictionary<string, string> { ["trace"] = "abc123" },
        };

        var r1 = await translator1.TranslateAsync(original);
        var r2 = await translator2.TranslateAsync(r1.TranslatedEnvelope);

        Assert.That(r2.TranslatedEnvelope.Payload, Is.EqualTo("[HELLO]"));
        Assert.That(r2.TranslatedEnvelope.Metadata["trace"], Is.EqualTo("abc123"));
        Assert.That(r2.TranslatedEnvelope.CorrelationId, Is.EqualTo(original.CorrelationId));
        Assert.That(r1.TranslatedEnvelope.CausationId, Is.EqualTo(original.MessageId));
        Assert.That(r2.TranslatedEnvelope.CausationId, Is.EqualTo(r1.TranslatedEnvelope.MessageId));

        output1.AssertReceivedOnTopic("stage1-topic", 1);
        output2.AssertReceivedOnTopic("stage2-topic", 1);
    }

    [Test]
    public async Task Challenge3_PreservesSourceWhenNoOverride()
    {
        await using var output = new MockEndpoint("preserve");
        var transform = Substitute.For<IPayloadTransform<string, string>>();
        transform.Transform(Arg.Any<string>()).Returns("out");

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "dest-topic",
        });
        var translator = new MessageTranslator<string, string>(
            transform, output, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "OriginalSource", "original.type") with
        {
            Priority = MessagePriority.High,
            SchemaVersion = "2.0",
        };

        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.Source, Is.EqualTo("OriginalSource"));
        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("original.type"));
        Assert.That(result.TranslatedEnvelope.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(result.TranslatedEnvelope.SchemaVersion, Is.EqualTo("2.0"));
        output.AssertReceivedOnTopic("dest-topic", 1);
    }
}
