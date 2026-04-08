// ============================================================================
// Tutorial 15 – Message Translator (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Type-converting translation from string to int
//   🟡 Intermediate — Metadata and correlation preserved across a two-stage chain
//   🔴 Advanced     — Source, MessageType, Priority, and SchemaVersion preserved when no override
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Translator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial15;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — Type-converting translation from string to int ────

    [Test]
    public async Task Starter_TypeConversion_StringToInt()
    {
        await using var output = new MockEndpoint("type-convert");
        var transform = new MockPayloadTransform<string, int>(input => int.Parse(input));

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

    // ── 🟡 INTERMEDIATE — Metadata and correlation across two-stage chain ─

    [Test]
    public async Task Intermediate_MetadataPreservationChain_TwoTranslations()
    {
        await using var output1 = new MockEndpoint("stage1");
        await using var output2 = new MockEndpoint("stage2");

        var transform1 = new MockPayloadTransform<string, string>(input => input.ToUpperInvariant());

        var transform2 = new MockPayloadTransform<string, string>(input => $"[{input}]");

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

    // ── 🔴 ADVANCED — Source, MessageType, Priority, SchemaVersion preserved ─

    [Test]
    public async Task Advanced_PreservesSourceWhenNoOverride()
    {
        await using var output = new MockEndpoint("preserve");
        var transform = new MockPayloadTransform<string, string>(_ => "out");

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
