// ============================================================================
// Tutorial 15 – Message Translator (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Type-converting translation from string to int
//   🟡 Intermediate  — Metadata and correlation preserved across a two-stage chain
//   🔴 Advanced      — Source, MessageType, Priority, and SchemaVersion preserved when no override
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Translator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial15;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Type-converting translation from string to int ────
    //
    // SCENARIO: A string payload "42" arrives from a parser service. The
    //           translator converts it to an integer and publishes to the
    //           target topic with the correct MessageType and CausationId.
    //
    // WHAT YOU PROVE: The translator correctly converts payload types across
    //                 a type boundary (string → int) and preserves envelope lineage.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_TypeConversion_StringToInt()
    {
        await using var output = new MockEndpoint("type-convert");
        // TODO: Create a MockPayloadTransform with appropriate configuration
        dynamic transform = null!;

        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a MessageTranslator with appropriate configuration
        dynamic translator = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var result = await translator.TranslateAsync(...)
        dynamic result = null!;

        Assert.That(result.TranslatedEnvelope.Payload, Is.EqualTo(42));
        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("number.parsed"));
        Assert.That(result.TranslatedEnvelope.CausationId, Is.EqualTo(envelope.MessageId));
        output.AssertReceivedOnTopic("int-topic", 1);
    }

    // ── 🟡 INTERMEDIATE — Metadata and correlation across two-stage chain ─
    //
    // SCENARIO: A message flows through two translators in sequence. The
    //           first uppercases the payload; the second wraps it in brackets.
    //           Metadata ("trace") and CorrelationId must survive both hops,
    //           and each stage must set CausationId to its input's MessageId.
    //
    // WHAT YOU PROVE: The translator preserves metadata and correlation
    //                 through a multi-stage translation pipeline.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_MetadataPreservationChain_TwoTranslations()
    {
        await using var output1 = new MockEndpoint("stage1");
        await using var output2 = new MockEndpoint("stage2");

        // TODO: Create a MockPayloadTransform with appropriate configuration
        dynamic transform1 = null!;

        // TODO: Create a MockPayloadTransform with appropriate configuration
        dynamic transform2 = null!;

        // TODO: Create a MessageTranslator with appropriate configuration
        dynamic translator1 = null!;

        // TODO: Create a MessageTranslator with appropriate configuration
        dynamic translator2 = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic original = null!;

        // TODO: var r1 = await translator1.TranslateAsync(...)
        dynamic r1 = null!;
        // TODO: var r2 = await translator2.TranslateAsync(...)
        dynamic r2 = null!;

        Assert.That(r2.TranslatedEnvelope.Payload, Is.EqualTo("[HELLO]"));
        Assert.That(r2.TranslatedEnvelope.Metadata["trace"], Is.EqualTo("abc123"));
        Assert.That(r2.TranslatedEnvelope.CorrelationId, Is.EqualTo(original.CorrelationId));
        Assert.That(r1.TranslatedEnvelope.CausationId, Is.EqualTo(original.MessageId));
        Assert.That(r2.TranslatedEnvelope.CausationId, Is.EqualTo(r1.TranslatedEnvelope.MessageId));

        output1.AssertReceivedOnTopic("stage1-topic", 1);
        output2.AssertReceivedOnTopic("stage2-topic", 1);
    }

    // ── 🔴 ADVANCED — Source, MessageType, Priority, SchemaVersion preserved ─
    //
    // SCENARIO: An envelope carries high-priority data with a specific
    //           SchemaVersion. No TargetSource or TargetMessageType overrides
    //           are configured. After translation, all original envelope
    //           properties must be faithfully preserved in the output.
    //
    // WHAT YOU PROVE: When no overrides are configured, the translator
    //                 preserves Source, MessageType, Priority, and SchemaVersion.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_PreservesSourceWhenNoOverride()
    {
        await using var output = new MockEndpoint("preserve");
        // TODO: Create a MockPayloadTransform with appropriate configuration
        dynamic transform = null!;

        // TODO: var options = Options.Create(...)
        dynamic options = null!;
        // TODO: Create a MessageTranslator with appropriate configuration
        dynamic translator = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;

        // TODO: var result = await translator.TranslateAsync(...)
        dynamic result = null!;

        Assert.That(result.TranslatedEnvelope.Source, Is.EqualTo("OriginalSource"));
        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("original.type"));
        Assert.That(result.TranslatedEnvelope.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(result.TranslatedEnvelope.SchemaVersion, Is.EqualTo("2.0"));
        output.AssertReceivedOnTopic("dest-topic", 1);
    }
}
#endif
