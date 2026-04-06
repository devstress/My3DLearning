// ============================================================================
// Tutorial 15 – Message Translator (Lab)
// ============================================================================
// This lab exercises the MessageTranslator — the pattern that converts a
// message from one format to another. You will test payload transformation,
// envelope field preservation (CorrelationId, Priority, CausationId chain),
// and verify that the translated envelope is published to the target topic.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Translator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial15;

[TestFixture]
public sealed class Lab
{
    // ── Basic Translation — String to String ────────────────────────────────

    [Test]
    public async Task Translate_StringToString_ProducesTranslatedEnvelope()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var transform = new FuncPayloadTransform<string, string>(s => s.ToUpperInvariant());

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "translated-topic",
        });

        var translator = new MessageTranslator<string, string>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "hello world", "SourceService", "greeting.event");

        var result = await translator.TranslateAsync(source);

        Assert.That(result.TranslatedEnvelope.Payload, Is.EqualTo("HELLO WORLD"));
        Assert.That(result.TargetTopic, Is.EqualTo("translated-topic"));
        Assert.That(result.SourceMessageId, Is.EqualTo(source.MessageId));
    }

    // ── CorrelationId Is Preserved ──────────────────────────────────────────

    [Test]
    public async Task Translate_PreservesCorrelationId()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var transform = new FuncPayloadTransform<string, string>(s => s);

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "output-topic",
        });

        var translator = new MessageTranslator<string, string>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "data", "Service", "event.type");

        var result = await translator.TranslateAsync(source);

        Assert.That(result.TranslatedEnvelope.CorrelationId, Is.EqualTo(source.CorrelationId));
    }

    // ── CausationId Set to Source MessageId ──────────────────────────────────

    [Test]
    public async Task Translate_CausationId_SetToSourceMessageId()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var transform = new FuncPayloadTransform<string, string>(s => s);

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "output-topic",
        });

        var translator = new MessageTranslator<string, string>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "data", "Service", "event.type");

        var result = await translator.TranslateAsync(source);

        Assert.That(result.TranslatedEnvelope.CausationId, Is.EqualTo(source.MessageId));
        Assert.That(result.TranslatedEnvelope.MessageId, Is.Not.EqualTo(source.MessageId));
    }

    // ── TargetMessageType Override ──────────────────────────────────────────

    [Test]
    public async Task Translate_TargetMessageTypeOverride_ChangesMessageType()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var transform = new FuncPayloadTransform<string, string>(s => s);

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "output-topic",
            TargetMessageType = "translated.event",
        });

        var translator = new MessageTranslator<string, string>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "data", "Service", "original.event");

        var result = await translator.TranslateAsync(source);

        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("translated.event"));
    }

    // ── TargetSource Override ───────────────────────────────────────────────

    [Test]
    public async Task Translate_TargetSourceOverride_ChangesSource()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var transform = new FuncPayloadTransform<string, string>(s => s);

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "output-topic",
            TargetSource = "TranslatorService",
        });

        var translator = new MessageTranslator<string, string>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "data", "OriginalService", "event.type");

        var result = await translator.TranslateAsync(source);

        Assert.That(result.TranslatedEnvelope.Source, Is.EqualTo("TranslatorService"));
    }

    // ── No TargetTopic Configured — Throws ──────────────────────────────────

    [Test]
    public void Translate_NoTargetTopic_ThrowsInvalidOperationException()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var transform = new FuncPayloadTransform<string, string>(s => s);

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "", // Empty — not configured.
        });

        var translator = new MessageTranslator<string, string>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "data", "Service", "event.type");

        Assert.ThrowsAsync<InvalidOperationException>(
            () => translator.TranslateAsync(source));
    }

    // ── Verify Producer PublishAsync Called ──────────────────────────────────

    [Test]
    public async Task Translate_PublishesToTargetTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var transform = new FuncPayloadTransform<string, string>(s => s);

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "translated-topic",
        });

        var translator = new MessageTranslator<string, string>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "data", "Service", "event.type");

        await translator.TranslateAsync(source);

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("translated-topic"),
            Arg.Any<CancellationToken>());
    }
}
