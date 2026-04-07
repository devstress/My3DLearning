// ============================================================================
// Tutorial 15 – Message Translator (Lab)
// ============================================================================
// EIP Pattern: Message Translator
// E2E: Wire real MessageTranslator with MockPayloadTransform and
// MockEndpoint, verify payload transformation and envelope publishing.
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
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("translator-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Core Translation ──────────────────────────────────────────

    [Test]
    public async Task Translate_TransformsPayload_PublishesToTarget()
    {
        var transform = new MockPayloadTransform<string, string>(input => input.ToUpperInvariant());

        var translator = CreateTranslator(transform, "translated-topic");
        var envelope = IntegrationEnvelope<string>.Create(
            "hello", "SourceSvc", "input.type");

        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.Payload, Is.EqualTo("HELLO"));
        Assert.That(result.TargetTopic, Is.EqualTo("translated-topic"));
        Assert.That(result.SourceMessageId, Is.EqualTo(envelope.MessageId));
        _output.AssertReceivedOnTopic("translated-topic", 1);
    }


    // ── 2. Envelope Fidelity ─────────────────────────────────────────

    [Test]
    public async Task Translate_PreservesCorrelationId()
    {
        var transform = new MockPayloadTransform<string, string>(_ => "out");

        var translator = CreateTranslator(transform, "target");
        var envelope = IntegrationEnvelope<string>.Create("in", "Svc", "type");

        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.CorrelationId,
            Is.EqualTo(envelope.CorrelationId));
    }

    [Test]
    public async Task Translate_SetsCausationIdToSourceMessageId()
    {
        var transform = new MockPayloadTransform<string, string>(_ => "out");

        var translator = CreateTranslator(transform, "target");
        var envelope = IntegrationEnvelope<string>.Create("in", "Svc", "type");

        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.CausationId,
            Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task Translate_OverridesSourceAndMessageType()
    {
        var transform = new MockPayloadTransform<string, string>(_ => "out");

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "target",
            TargetSource = "NewSource",
            TargetMessageType = "new.type",
        });
        var translator = new MessageTranslator<string, string>(
            transform, _output, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("in", "OldSource", "old.type");
        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.Source, Is.EqualTo("NewSource"));
        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("new.type"));
        _output.AssertReceivedOnTopic("target", 1);
    }


    // ── 3. Validation & E2E ──────────────────────────────────────────

    [Test]
    public async Task Translate_PreservesMetadata()
    {
        var transform = new MockPayloadTransform<string, string>(_ => "out");

        var translator = CreateTranslator(transform, "target");
        var envelope = IntegrationEnvelope<string>.Create("in", "Svc", "type") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "us-east",
                ["tenant"] = "acme",
            },
        };

        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.Metadata["region"], Is.EqualTo("us-east"));
        Assert.That(result.TranslatedEnvelope.Metadata["tenant"], Is.EqualTo("acme"));
    }

    [Test]
    public async Task Translate_NoTargetTopic_ThrowsInvalidOperation()
    {
        var transform = new MockPayloadTransform<string, string>(_ => "out");
        var options = Options.Create(new TranslatorOptions { TargetTopic = "" });
        var translator = new MessageTranslator<string, string>(
            transform, _output, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("in", "Svc", "type");

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await translator.TranslateAsync(envelope));
        _output.AssertNoneReceived();
    }

    private MessageTranslator<string, string> CreateTranslator(
        IPayloadTransform<string, string> transform, string targetTopic)
    {
        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = targetTopic,
        });
        return new MessageTranslator<string, string>(
            transform, _output, options,
            NullLogger<MessageTranslator<string, string>>.Instance);
    }
}
