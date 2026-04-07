// ============================================================================
// Tutorial 15 – Message Translator (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Message Translator pattern
//          converts a payload from one format to another while preserving
//          envelope identity (CorrelationId, CausationId chain).
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Translate_TransformsPayload_PublishesToTarget        — core payload transformation and target topic publishing
//   2. Translate_PreservesCorrelationId                     — CorrelationId preserved across translation
//   3. Translate_SetsCausationIdToSourceMessageId           — CausationId set to source MessageId
//   4. Translate_OverridesSourceAndMessageType              — Source and MessageType overrides via TranslatorOptions
//   5. Translate_PreservesMetadata                          — metadata dictionary preserved through translation
//   6. Translate_NoTargetTopic_ThrowsInvalidOperation       — validation throws when TargetTopic is empty
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire), NUnit
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
    // ── 1. Core Translation ──────────────────────────────────────────

    [Test]
    public async Task Translate_TransformsPayload_PublishesToTarget()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t15-core");
        var topic = AspireFixture.UniqueTopic("t15-translated");

        var transform = new MockPayloadTransform<string, string>(input => input.ToUpperInvariant());

        var translator = CreateTranslator(nats, transform, topic);
        var envelope = IntegrationEnvelope<string>.Create(
            "hello", "SourceSvc", "input.type");

        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.Payload, Is.EqualTo("HELLO"));
        Assert.That(result.TargetTopic, Is.EqualTo(topic));
        Assert.That(result.SourceMessageId, Is.EqualTo(envelope.MessageId));
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Envelope Fidelity ─────────────────────────────────────────

    [Test]
    public async Task Translate_PreservesCorrelationId()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t15-corr");
        var topic = AspireFixture.UniqueTopic("t15-corr");

        var transform = new MockPayloadTransform<string, string>(_ => "out");

        var translator = CreateTranslator(nats, transform, topic);
        var envelope = IntegrationEnvelope<string>.Create("in", "Svc", "type");

        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.CorrelationId,
            Is.EqualTo(envelope.CorrelationId));
    }

    [Test]
    public async Task Translate_SetsCausationIdToSourceMessageId()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t15-cause");
        var topic = AspireFixture.UniqueTopic("t15-cause");

        var transform = new MockPayloadTransform<string, string>(_ => "out");

        var translator = CreateTranslator(nats, transform, topic);
        var envelope = IntegrationEnvelope<string>.Create("in", "Svc", "type");

        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.CausationId,
            Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task Translate_OverridesSourceAndMessageType()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t15-override");
        var topic = AspireFixture.UniqueTopic("t15-override");

        var transform = new MockPayloadTransform<string, string>(_ => "out");

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = topic,
            TargetSource = "NewSource",
            TargetMessageType = "new.type",
        });
        var translator = new MessageTranslator<string, string>(
            transform, nats, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("in", "OldSource", "old.type");
        var result = await translator.TranslateAsync(envelope);

        Assert.That(result.TranslatedEnvelope.Source, Is.EqualTo("NewSource"));
        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("new.type"));
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 3. Validation & E2E ──────────────────────────────────────────

    [Test]
    public async Task Translate_PreservesMetadata()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t15-meta");
        var topic = AspireFixture.UniqueTopic("t15-meta");

        var transform = new MockPayloadTransform<string, string>(_ => "out");

        var translator = CreateTranslator(nats, transform, topic);
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
        await using var nats = AspireFixture.CreateNatsEndpoint("t15-notopic");

        var transform = new MockPayloadTransform<string, string>(_ => "out");
        var options = Options.Create(new TranslatorOptions { TargetTopic = "" });
        var translator = new MessageTranslator<string, string>(
            transform, nats, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("in", "Svc", "type");

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await translator.TranslateAsync(envelope));
        nats.AssertNoneReceived();
    }

    private static MessageTranslator<string, string> CreateTranslator(
        NatsBrokerEndpoint nats, IPayloadTransform<string, string> transform,
        string targetTopic)
    {
        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = targetTopic,
        });
        return new MessageTranslator<string, string>(
            transform, nats, options,
            NullLogger<MessageTranslator<string, string>>.Instance);
    }
}
